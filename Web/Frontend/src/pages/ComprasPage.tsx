import { useMemo, useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useDocumentTitle } from '../hooks/useDebounce';
import { compraService } from '../services/compraService';
import { fornecedorService } from '../services/cadastroService';
import { produtoService } from '../services/produtoService';
import type { Compra, ItemCompraRequest } from '../models';
import { formatCurrency, formatDateTime, formatNumber, mensagemDeErro } from '../utils/format';
import { parseDecimal } from '../utils/masks';
import { PageHeader, Loading, EmptyState, StatusBadge } from '../components/PageHeader';
import { ConfirmDialog } from '../components/Modal';
import { ExportButtons } from '../components/ExportButtons';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Compras / entrada de mercadorias. O salvamento dá entrada automática no
// estoque e atualiza o custo do produto (regra do CompraService original).
// ============================================================================

interface ItemFormulario extends ItemCompraRequest {
  nome: string;
}

export function ComprasPage() {
  useDocumentTitle('Compras');
  const { sucesso, erro } = useToast();

  const fornecedoresApi = useApi(() => fornecedorService.listar(true), []);
  const produtosApi = useApi(() => produtoService.listar(true), []);
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const comprasApi = useApi(() => compraService.listar(inicio || undefined, fim || undefined), [inicio, fim]);

  const [fornecedorId, setFornecedorId] = useState('');
  const [numeroNota, setNumeroNota] = useState('');
  const [produtoId, setProdutoId] = useState('');
  const [quantidade, setQuantidade] = useState('1');
  const [custo, setCusto] = useState('');
  const [itens, setItens] = useState<ItemFormulario[]>([]);
  const [salvando, setSalvando] = useState(false);
  const [paraCancelar, setParaCancelar] = useState<Compra | null>(null);
  const [cancelando, setCancelando] = useState(false);

  const fornecedores = fornecedoresApi.dados ?? [];
  const produtos = produtosApi.dados ?? [];
  const total = useMemo(() => itens.reduce((soma, item) => soma + item.quantidade * item.precoCusto, 0), [itens]);

  const adicionarItem = () => {
    const produto = produtos.find((p) => p.id === Number(produtoId));
    if (!produto) { erro('Selecione um produto.'); return; }

    const qtd = Number.parseInt(quantidade, 10);
    if (!Number.isFinite(qtd) || qtd <= 0) { erro('Informe uma quantidade válida.'); return; }

    const valorCusto = parseDecimal(custo);
    if (valorCusto <= 0) { erro('Informe um preço de custo válido.'); return; }

    setItens((atuais) => {
      const existente = atuais.find((i) => i.produtoId === produto.id);
      if (existente) {
        return atuais.map((i) =>
          i.produtoId === produto.id ? { ...i, quantidade: i.quantidade + qtd } : i
        );
      }
      return [...atuais, { produtoId: produto.id, nome: produto.nome, quantidade: qtd, precoCusto: valorCusto }];
    });

    setProdutoId('');
    setQuantidade('1');
    setCusto('');
  };

  const removerItem = (id: number) => setItens((atuais) => atuais.filter((i) => i.produtoId !== id));

  const salvar = async () => {
    if (itens.length === 0) { erro('Adicione pelo menos um item à compra.'); return; }
    if (!fornecedorId) { erro('Selecione um fornecedor.'); return; }

    setSalvando(true);
    try {
      const compra = await compraService.registrar({
        fornecedorId: Number(fornecedorId),
        numeroNota: numeroNota.trim() || null,
        itens: itens.map(({ produtoId: id, quantidade: qtd, precoCusto }) => ({ produtoId: id, quantidade: qtd, precoCusto }))
      });

      sucesso(`Compra #${compra.id} registrada! Estoque atualizado.`);
      setItens([]);
      setNumeroNota('');
      await Promise.all([comprasApi.recarregar(true), produtosApi.recarregar(true)]);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setSalvando(false);
    }
  };

  const cancelarCompra = async () => {
    if (!paraCancelar) return;
    setCancelando(true);
    try {
      await compraService.cancelar(paraCancelar.id);
      sucesso('Compra cancelada e estoque estornado.');
      setParaCancelar(null);
      await Promise.all([comprasApi.recarregar(true), produtosApi.recarregar(true)]);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setCancelando(false);
    }
  };

  const compras = comprasApi.dados ?? [];

  return (
    <div className="page">
      <PageHeader
        titulo="Compras / Entrada de Mercadorias"
        descricao="Registre notas de fornecedores e dê entrada automática no estoque."
        acoes={<ExportButtons tipo="compras" params={{ inicio: inicio || undefined, fim: fim || undefined }} />}
      />

      <section className="card">
        <header className="card-header">
          <div><h2>Nova compra</h2><p>Selecione o fornecedor, adicione os itens e salve.</p></div>
        </header>
        <div className="card-body">
          <div className="form-grid">
            <label className="field">
              <span>Fornecedor *</span>
              <select value={fornecedorId} onChange={(e) => setFornecedorId(e.target.value)}>
                <option value="">Selecione...</option>
                {fornecedores.map((f) => <option key={f.id} value={f.id}>{f.nome}</option>)}
              </select>
            </label>

            <label className="field">
              <span>Nº da nota (opcional)</span>
              <input value={numeroNota} onChange={(e) => setNumeroNota(e.target.value)} placeholder="Ex.: NF-1234" />
            </label>
          </div>

          <div className="compra-item-form">
            <label className="field">
              <span>Produto</span>
              <select value={produtoId} onChange={(e) => setProdutoId(e.target.value)}>
                <option value="">Selecione...</option>
                {produtos.map((p) => <option key={p.id} value={p.id}>{p.nome} · estoque {p.estoque}</option>)}
              </select>
            </label>
            <label className="field field-qtd">
              <span>Qtd</span>
              <input type="number" min={1} value={quantidade} onChange={(e) => setQuantidade(e.target.value)} />
            </label>
            <label className="field">
              <span>Custo unitário (R$)</span>
              <input inputMode="decimal" value={custo} onChange={(e) => setCusto(e.target.value)} placeholder="0,00" />
            </label>
            <button type="button" className="btn btn-outline" onClick={adicionarItem}>
              <Icon name="plus" size={16} /> Adicionar item
            </button>
          </div>

          {itens.length > 0 && (
            <div className="table-wrapper">
              <table className="table">
                <thead>
                  <tr><th>Produto</th><th className="text-right">Qtd</th><th className="text-right">Custo</th><th className="text-right">Subtotal</th><th /></tr>
                </thead>
                <tbody>
                  {itens.map((item) => (
                    <tr key={item.produtoId}>
                      <td>{item.nome}</td>
                      <td className="text-right">{formatNumber(item.quantidade)}</td>
                      <td className="text-right">{formatCurrency(item.precoCusto)}</td>
                      <td className="text-right"><strong>{formatCurrency(item.quantidade * item.precoCusto)}</strong></td>
                      <td className="text-right">
                        <button type="button" className="icon-button danger" onClick={() => removerItem(item.produtoId)} title="Remover">
                          <Icon name="trash" size={16} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <div className="compra-footer">
            <div className="total-box total-box-inline">
              <span>Total da compra</span>
              <strong className="total-value">{formatCurrency(total)}</strong>
            </div>
            <button type="button" className="btn btn-primary btn-lg" onClick={() => void salvar()} disabled={salvando || itens.length === 0}>
              {salvando ? 'Salvando...' : 'Salvar compra'}
            </button>
          </div>
        </div>
      </section>

      <section className="card">
        <header className="card-header">
          <div><h2>Compras registradas</h2><p>Consulte o histórico e cancele quando necessário.</p></div>
          <div className="filter-bar">
            <label className="field field-inline"><span>De</span><input type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} /></label>
            <label className="field field-inline"><span>Até</span><input type="date" value={fim} onChange={(e) => setFim(e.target.value)} /></label>
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => { setInicio(''); setFim(''); }}>Limpar</button>
          </div>
        </header>

        {comprasApi.carregando ? <Loading /> : compras.length === 0 ? (
          <EmptyState icone="shopping" titulo="Nenhuma compra registrada" descricao="Registre a primeira compra acima." />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr><th>#</th><th>Data</th><th>Fornecedor</th><th>Nota</th><th className="text-right">Total</th><th>Status</th><th className="text-right">Ações</th></tr>
              </thead>
              <tbody>
                {compras.map((compra) => (
                  <tr key={compra.id}>
                    <td>{compra.id}</td>
                    <td>{formatDateTime(compra.dataCompra)}</td>
                    <td>{compra.fornecedorNome ?? `#${compra.fornecedorId}`}</td>
                    <td>{compra.numeroNota || '—'}</td>
                    <td className="text-right">{formatCurrency(compra.valorTotal)}</td>
                    <td><StatusBadge status={compra.status} /></td>
                    <td className="text-right">
                      <button
                        type="button"
                        className="btn btn-outline btn-sm"
                        onClick={() => setParaCancelar(compra)}
                        disabled={compra.status === 'Cancelada'}
                      >
                        Cancelar compra
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <ConfirmDialog
        aberto={!!paraCancelar}
        titulo="Cancelar compra"
        mensagem={`Deseja cancelar a compra #${paraCancelar?.id}? O estoque dos itens será estornado.`}
        confirmarLabel="Cancelar compra"
        perigo
        carregando={cancelando}
        onConfirmar={() => void cancelarCompra()}
        onCancelar={() => setParaCancelar(null)}
      />
    </div>
  );
}
