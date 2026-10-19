import { useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useDebounce, useDocumentTitle } from '../hooks/useDebounce';
import { produtoService } from '../services/produtoService';
import type { Produto } from '../models';
import { formatCurrency, formatNumber, mensagemDeErro } from '../utils/format';
import { PageHeader, Loading, EmptyState, AtivoBadge } from '../components/PageHeader';
import { ProdutoFormModal } from '../components/ProdutoFormModal';
import { ConfirmDialog } from '../components/Modal';
import { ExportButtons } from '../components/ExportButtons';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Cadastro de produtos (Administrador/Estoquista). A exclusão é lógica,
// preservando vendas, compras e movimentações históricas.
// ============================================================================

export function ProdutosPage() {
  useDocumentTitle('Produtos');
  const { sucesso, erro } = useToast();

  const [busca, setBusca] = useState('');
  const buscaDebounced = useDebounce(busca);
  const [apenasAtivos, setApenasAtivos] = useState(true);
  const [modalAberto, setModalAberto] = useState(false);
  const [emEdicao, setEmEdicao] = useState<Produto | null>(null);
  const [paraExcluir, setParaExcluir] = useState<Produto | null>(null);
  const [excluindo, setExcluindo] = useState(false);

  const { dados, carregando, erro: erroApi, recarregar } = useApi(
    () => produtoService.listar(apenasAtivos, buscaDebounced),
    [apenasAtivos, buscaDebounced]
  );

  const abrirNovo = () => {
    setEmEdicao(null);
    setModalAberto(true);
  };

  const abrirEdicao = (produto: Produto) => {
    setEmEdicao(produto);
    setModalAberto(true);
  };

  const excluir = async () => {
    if (!paraExcluir) return;
    setExcluindo(true);
    try {
      await produtoService.desativar(paraExcluir.id);
      sucesso('Produto desativado.');
      setParaExcluir(null);
      await recarregar(true);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setExcluindo(false);
    }
  };

  const produtos = dados ?? [];

  return (
    <div className="page">
      <PageHeader
        titulo="Produtos"
        descricao="Cadastro de produtos, preços e níveis de estoque."
        acoes={
          <>
            <ExportButtons tipo="produtos" params={{ apenasAtivos }} />
            <button type="button" className="btn btn-primary" onClick={abrirNovo}>
              <Icon name="plus" size={16} />
              Novo produto
            </button>
          </>
        }
      />

      {erroApi && <div className="alert alert-danger">{erroApi}</div>}

      <section className="card">
        <div className="toolbar">
          <div className="search-box">
            <Icon name="search" size={18} />
            <input
              type="text"
              value={busca}
              onChange={(e) => setBusca(e.target.value)}
              placeholder="Buscar por nome ou código de barras"
            />
          </div>
          <label className="checkbox">
            <input type="checkbox" checked={!apenasAtivos} onChange={(e) => setApenasAtivos(!e.target.checked)} />
            Mostrar inativos
          </label>
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => void recarregar()} disabled={carregando}>
            <Icon name="refresh" size={16} />
            Atualizar
          </button>
        </div>

        {carregando ? (
          <Loading />
        ) : produtos.length === 0 ? (
          <EmptyState
            icone="box"
            titulo="Nenhum produto encontrado"
            descricao="Cadastre o primeiro produto ou ajuste a busca."
            acao={<button className="btn btn-primary" onClick={abrirNovo}>Novo produto</button>}
          />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Produto</th>
                  <th>Código</th>
                  <th className="text-right">Preço</th>
                  <th className="text-right">Custo</th>
                  <th className="text-right">Estoque</th>
                  <th className="text-right">Mínimo</th>
                  <th>Status</th>
                  <th className="text-right">Ações</th>
                </tr>
              </thead>
              <tbody>
                {produtos.map((produto) => (
                  <tr key={produto.id}>
                    <td>
                      <strong>{produto.nome}</strong>
                      {produto.categoria && <small className="cell-sub">{produto.categoria}</small>}
                    </td>
                    <td className="text-muted">{produto.codigoBarras || '—'}</td>
                    <td className="text-right">{formatCurrency(produto.preco)}</td>
                    <td className="text-right">{formatCurrency(produto.precoCusto)}</td>
                    <td className={`text-right ${produto.estoque <= produto.estoqueMinimo ? 'text-danger' : ''}`}>
                      <strong>{formatNumber(produto.estoque)}</strong>
                    </td>
                    <td className="text-right">{formatNumber(produto.estoqueMinimo)}</td>
                    <td><AtivoBadge ativo={produto.ativo} /></td>
                    <td className="text-right actions">
                      <button type="button" className="icon-button" title="Editar" onClick={() => abrirEdicao(produto)}>
                        <Icon name="edit" size={16} />
                      </button>
                      <button type="button" className="icon-button danger" title="Desativar" onClick={() => setParaExcluir(produto)}>
                        <Icon name="trash" size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <ProdutoFormModal
        aberto={modalAberto}
        produto={emEdicao}
        onFechar={() => setModalAberto(false)}
        onSalvo={() => void recarregar(true)}
      />

      <ConfirmDialog
        aberto={!!paraExcluir}
        titulo="Desativar produto"
        mensagem={`Deseja desativar o produto "${paraExcluir?.nome}"? Ele deixará de aparecer no PDV, mas o histórico será preservado.`}
        confirmarLabel="Desativar"
        perigo
        carregando={excluindo}
        onConfirmar={() => void excluir()}
        onCancelar={() => setParaExcluir(null)}
      />
    </div>
  );
}
