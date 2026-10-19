import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../contexts/ToastContext';
import { useApi } from '../hooks/useApi';
import { useCarrinho } from '../hooks/useCarrinho';
import { useDebounce, useDocumentTitle } from '../hooks/useDebounce';
import { caixaService } from '../services/caixaService';
import { clienteService } from '../services/cadastroService';
import { produtoService } from '../services/produtoService';
import { vendaService } from '../services/vendaService';
import type { FormaPagamento, Produto, Venda } from '../models';
import { formatCurrency, mensagemDeErro } from '../utils/format';
import { parseDecimal } from '../utils/masks';
import { PageHeader } from '../components/PageHeader';
import { PdvProductList } from '../components/PdvProductList';
import { PdvCart } from '../components/PdvCart';
import { PdvCheckout } from '../components/PdvCheckout';
import { ReciboModal } from '../components/ReciboModal';
import { Icon } from '../components/Icon';

// ============================================================================
// PDV. Fluxo: caixa aberto -> buscar produto -> carrinho -> pagamento -> venda.
// Todas as regras críticas (estoque, fiado, troco) também são validadas na API.
// ============================================================================

export function PdvPage() {
  useDocumentTitle('Vender (PDV)');
  const { usuario } = useAuth();
  const { sucesso, erro } = useToast();

  const [busca, setBusca] = useState('');
  const buscaDebounced = useDebounce(busca);
  const produtosApi = useApi(() => produtoService.listar(true, buscaDebounced), [buscaDebounced]);
  const clientesApi = useApi(() => clienteService.listar(true), []);
  const caixaApi = useApi(() => caixaService.obterAberto(), []);

  const carrinho = useCarrinho();
  const [selecionado, setSelecionado] = useState<Produto | null>(null);
  const [quantidade, setQuantidade] = useState('1');
  const [formaPagamento, setFormaPagamento] = useState<FormaPagamento>('Dinheiro');
  const [valorRecebido, setValorRecebido] = useState('');
  const [clienteId, setClienteId] = useState('');
  const [processando, setProcessando] = useState(false);
  const [recibo, setRecibo] = useState<Venda | null>(null);
  const [valorRecebidoRecibo, setValorRecebidoRecibo] = useState<number | undefined>(undefined);

  const clientes = clientesApi.dados ?? [];
  const clienteSelecionado = useMemo(
    () => clientes.find((c) => c.id === Number(clienteId)) ?? null,
    [clientes, clienteId]
  );

  const recebido = parseDecimal(valorRecebido);
  const troco = formaPagamento === 'Dinheiro' ? recebido - carrinho.total : 0;
  const caixa = caixaApi.dados;

  const selecionarProduto = (produto: Produto) => {
    setSelecionado(produto);
    setQuantidade('1');
  };

  const adicionarAoCarrinho = () => {
    if (!selecionado) {
      erro('Selecione um produto.');
      return;
    }

    const qtd = Number.parseInt(quantidade, 10);
    if (!Number.isFinite(qtd) || qtd <= 0) {
      erro('Informe uma quantidade válida.');
      return;
    }

    carrinho.adicionar(selecionado, qtd);
    sucesso(`${selecionado.nome} adicionado ao carrinho.`);
    setSelecionado(null);
    setQuantidade('1');
    setBusca('');
  };

  const buscarPorEnter = () => {
    const termo = busca.trim().toLowerCase();
    if (!termo) return;

    const produtos = produtosApi.dados ?? [];
    const encontrado =
      produtos.find((p) => (p.codigoBarras ?? '').toLowerCase() === termo) ??
      produtos.find((p) => p.nome.toLowerCase() === termo);

    if (encontrado) selecionarProduto(encontrado);
    else if (produtos.length === 1) selecionarProduto(produtos[0]);
  };

  const finalizar = async () => {
    const caixaAtual = caixaApi.dados;
    if (!caixaAtual) {
      erro('Caixa: fechado. Abra o caixa para vender.');
      return;
    }
    if (carrinho.itens.length === 0) {
      erro('Adicione pelo menos um item à venda.');
      return;
    }
    if (carrinho.itensSemEstoque.length > 0) {
      erro(`Estoque insuficiente para o produto '${carrinho.itensSemEstoque[0].nome}'.`);
      return;
    }
    if (formaPagamento === 'Fiado' && !clienteSelecionado) {
      erro('Venda fiada exige um cliente selecionado.');
      return;
    }
    if (
      formaPagamento === 'Fiado' && clienteSelecionado &&
      clienteSelecionado.limiteCredito > 0 &&
      clienteSelecionado.saldoDevedor + carrinho.total > clienteSelecionado.limiteCredito
    ) {
      erro(`Limite de crédito do cliente excedido. Saldo atual: ${formatCurrency(clienteSelecionado.saldoDevedor)}.`);
      return;
    }
    if (formaPagamento === 'Dinheiro' && (valorRecebido.trim() ? troco < 0 : carrinho.total > 0)) {
      erro('Valor recebido é insuficiente para completar a venda.');
      return;
    }

    setProcessando(true);
    try {
      const venda = await vendaService.registrar({
        itens: carrinho.itens.map((item) => ({
          produtoId: item.produtoId,
          quantidade: item.quantidade,
          precoUnitario: item.precoUnitario
        })),
        desconto: carrinho.desconto,
        formaPagamento,
        clienteId: clienteSelecionado?.id ?? null,
        valorRecebido: formaPagamento === 'Dinheiro' ? recebido : null
      });

      setValorRecebidoRecibo(formaPagamento === 'Dinheiro' ? recebido : undefined);
      setRecibo(venda);
      sucesso(`Venda #${venda.id} registrada com sucesso.`);

      carrinho.limpar();
      setValorRecebido('');
      setClienteId('');
      setFormaPagamento('Dinheiro');
      setSelecionado(null);

      await Promise.all([
        produtosApi.recarregar(true),
        clientesApi.recarregar(true),
        caixaApi.recarregar(true)
      ]);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setProcessando(false);
    }
  };

  return (
    <div className="page">
      <PageHeader
        titulo="Vender (PDV)"
        descricao={`Operador: ${usuario?.nome ?? '-'} · ${usuario?.permissaoDescricao ?? ''}`}
        acoes={
          caixaApi.carregando ? (
            <span className="badge badge-neutral">Verificando caixa...</span>
          ) : caixa ? (
            <span className="badge badge-success">Caixa #{caixa.id} aberto · {formatCurrency(caixa.valorInicial)}</span>
          ) : (
            <span className="badge badge-danger">Caixa: fechado. Abra o caixa para vender.</span>
          )
        }
      />

      {!caixaApi.carregando && !caixa && (
        <div className="alert alert-warning">
          <Icon name="alert" size={18} />
          <span>
            Caixa: fechado. Abra o caixa para vender.
            {usuario?.permissao === 'Administrador' && <> <Link to="/caixa">Abrir caixa agora</Link>.</>}
          </span>
        </div>
      )}

      {produtosApi.erro && <div className="alert alert-danger">{produtosApi.erro}</div>}
      {caixaApi.erro && <div className="alert alert-danger">{caixaApi.erro}</div>}

      <PdvProductList
        produtos={produtosApi.dados ?? []}
        busca={busca}
        onBusca={setBusca}
        carregando={produtosApi.carregando}
        selecionado={selecionado}
        onSelecionar={selecionarProduto}
        quantidade={quantidade}
        onQuantidade={setQuantidade}
        onAdicionar={adicionarAoCarrinho}
        onBuscarEnter={buscarPorEnter}
      />

      <div className="pdv-bottom">
        <PdvCart
          itens={carrinho.itens}
          subtotal={carrinho.subtotal}
          desconto={carrinho.desconto}
          total={carrinho.total}
          onAlterarQuantidade={carrinho.alterarQuantidade}
          onRemover={carrinho.remover}
          onLimpar={carrinho.limpar}
        />

        <PdvCheckout
          formaPagamento={formaPagamento}
          onFormaPagamento={setFormaPagamento}
          clientes={clientes}
          clienteId={clienteId}
          onCliente={setClienteId}
          descontoTexto={carrinho.descontoTexto}
          onDesconto={carrinho.setDescontoTexto}
          valorRecebido={valorRecebido}
          onValorRecebido={setValorRecebido}
          troco={troco}
          subtotal={carrinho.subtotal}
          desconto={carrinho.desconto}
          total={carrinho.total}
          caixaAberto={!!caixa}
          caixaCarregando={caixaApi.carregando}
          processando={processando}
          carrinhoVazio={carrinho.itens.length === 0}
          onFinalizar={() => void finalizar()}
        />
      </div>

      <ReciboModal venda={recibo} valorRecebido={valorRecebidoRecibo} onFechar={() => setRecibo(null)} />
    </div>
  );
}
