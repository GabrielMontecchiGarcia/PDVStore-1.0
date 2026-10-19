import { useMemo, useState, type FormEvent } from 'react';
import { useApi } from '../hooks/useApi';
import { useDocumentTitle } from '../hooks/useDebounce';
import { produtoService } from '../services/produtoService';
import type { Produto } from '../models';
import { formatDateTime, formatNumber, mensagemDeErro, primeiroDiaDoMesInput, hojeInput } from '../utils/format';
import { PageHeader, Loading, EmptyState, StatusBadge } from '../components/PageHeader';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Estoque (Administrador): ajustes manuais de entrada/saída com registro de
// movimentação e histórico completo.
// ============================================================================

export function EstoquePage() {
  useDocumentTitle('Estoque');
  const { sucesso, erro } = useToast();

  const produtosApi = useApi(() => produtoService.listar(false, ''), []);

  const [inicio, setInicio] = useState(primeiroDiaDoMesInput());
  const [fim, setFim] = useState(hojeInput());
  const movimentosApi = useApi(() => produtoService.historico(inicio, fim), [inicio, fim]);

  const [selecionado, setSelecionado] = useState<Produto | null>(null);
  const [tipo, setTipo] = useState<'Entrada' | 'Saída'>('Entrada');
  const [quantidade, setQuantidade] = useState('1');
  const [motivo, setMotivo] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [filtroHistorico, setFiltroHistorico] = useState('');

  const produtos = produtosApi.dados ?? [];
  const movimentos = movimentosApi.dados ?? [];

  const produtosBaixos = useMemo(
    () => produtos.filter((p) => p.ativo && p.estoque <= p.estoqueMinimo).length,
    [produtos]
  );

  const movimentosFiltrados = useMemo(() => {
    const termo = filtroHistorico.trim().toLowerCase();
    if (!termo) return movimentos;
    return movimentos.filter((m) => (m.produtoNome ?? '').toLowerCase().includes(termo));
  }, [movimentos, filtroHistorico]);

  const ajustar = async (evento: FormEvent) => {
    evento.preventDefault();

    if (!selecionado) { erro('Selecione um produto.'); return; }

    const qtd = Number.parseInt(quantidade, 10);
    if (!Number.isFinite(qtd) || qtd <= 0) { erro('Quantidade deve ser maior que zero.'); return; }

    setSalvando(true);
    try {
      if (tipo === 'Entrada') {
        await produtoService.entrada(selecionado.id, qtd, motivo.trim() || undefined);
        sucesso(`Entrada de ${qtd} unidade(s) registrada para ${selecionado.nome}.`);
      } else {
        await produtoService.saida(selecionado.id, qtd, motivo.trim() || undefined);
        sucesso(`Saída de ${qtd} unidade(s) registrada para ${selecionado.nome}.`);
      }

      setMotivo('');
      setQuantidade('1');
      await Promise.all([produtosApi.recarregar(true), movimentosApi.recarregar(true)]);

      const atualizado = await produtoService.obter(selecionado.id);
      setSelecionado(atualizado);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className="page">
      <PageHeader
        titulo="Estoque"
        descricao="Ajustes manuais, histórico de movimentações e alertas de reposição."
      />

      <section className="stat-grid stat-grid-3">
        <article className="stat-card">
          <span className="stat-icon stat-icon-blue"><Icon name="box" /></span>
          <div><small>Produtos cadastrados</small><strong>{formatNumber(produtos.length)}</strong></div>
        </article>
        <article className="stat-card">
          <span className="stat-icon stat-icon-red"><Icon name="alert" /></span>
          <div><small>Produtos abaixo do mínimo</small><strong>{formatNumber(produtosBaixos)}</strong></div>
        </article>
        <article className="stat-card">
          <span className="stat-icon stat-icon-green"><Icon name="layers" /></span>
          <div><small>Movimentações no período</small><strong>{formatNumber(movimentos.length)}</strong></div>
        </article>
      </section>

      <div className="grid-2 grid-estoque">
        <section className="card">
          <header className="card-header">
            <div><h2>Produtos</h2><p>Selecione um produto para ajustar o saldo.</p></div>
          </header>
          <div className="card-body no-padding">
            {produtosApi.carregando ? <Loading /> : produtos.length === 0 ? (
              <EmptyState icone="box" titulo="Nenhum produto cadastrado" />
            ) : (
              <div className="table-wrapper table-scroll">
                <table className="table table-hover">
                  <thead>
                    <tr><th>Produto</th><th className="text-right">Estoque</th><th className="text-right">Mín.</th></tr>
                  </thead>
                  <tbody>
                    {produtos.map((produto) => (
                      <tr
                        key={produto.id}
                        className={selecionado?.id === produto.id ? 'row-selected' : ''}
                        onClick={() => { setSelecionado(produto); setQuantidade('1'); }}
                      >
                        <td>
                          <strong>{produto.nome}</strong>
                          {!produto.ativo && <small className="cell-sub">Inativo</small>}
                        </td>
                        <td className={`text-right ${produto.estoque <= produto.estoqueMinimo ? 'text-danger' : ''}`}>
                          {formatNumber(produto.estoque)}
                        </td>
                        <td className="text-right">{formatNumber(produto.estoqueMinimo)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </section>

        <section className="card">
          <header className="card-header">
            <div><h2>Ajuste manual</h2><p>Registra entrada ou saída com usuário e motivo.</p></div>
          </header>
          <div className="card-body">
            {!selecionado ? (
              <EmptyState icone="layers" titulo="Selecione um produto" descricao="Clique em um produto na lista ao lado." />
            ) : (
              <form onSubmit={ajustar} className="form-stack">
                <div className="produto-selecionado">
                  <strong>{selecionado.nome}</strong>
                  <span>Saldo atual: <b>{formatNumber(selecionado.estoque)}</b> · Mínimo: {formatNumber(selecionado.estoqueMinimo)}</span>
                </div>

                <div className="segmented">
                  <button type="button" className={tipo === 'Entrada' ? 'active' : ''} onClick={() => setTipo('Entrada')}>Entrada</button>
                  <button type="button" className={tipo === 'Saída' ? 'active' : ''} onClick={() => setTipo('Saída')}>Saída</button>
                </div>

                <label className="field">
                  <span>Quantidade *</span>
                  <input type="number" min={1} value={quantidade} onChange={(e) => setQuantidade(e.target.value)} />
                </label>

                <label className="field">
                  <span>Motivo</span>
                  <input value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Ex.: ajuste de inventário, devolução..." />
                </label>

                <button type="submit" className="btn btn-primary" disabled={salvando}>
                  {salvando ? 'Registrando...' : `Registrar ${tipo.toLowerCase()}`}
                </button>

                {tipo === 'Saída' && (
                  <small className="text-muted">A saída é bloqueada quando não há saldo suficiente.</small>
                )}
              </form>
            )}
          </div>
        </section>
      </div>

      <section className="card">
        <header className="card-header">
          <div><h2>Histórico de movimentações</h2><p>Entradas e saídas registradas no período.</p></div>
          <div className="filter-bar">
            <label className="field field-inline"><span>De</span><input type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} /></label>
            <label className="field field-inline"><span>Até</span><input type="date" value={fim} onChange={(e) => setFim(e.target.value)} /></label>
          </div>
        </header>

        <div className="toolbar">
          <div className="search-box">
            <Icon name="search" size={18} />
            <input value={filtroHistorico} onChange={(e) => setFiltroHistorico(e.target.value)} placeholder="Filtrar por produto" />
          </div>
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => void movimentosApi.recarregar()} disabled={movimentosApi.carregando}>
            <Icon name="refresh" size={16} /> Atualizar
          </button>
        </div>

        {movimentosApi.carregando ? <Loading /> : movimentosFiltrados.length === 0 ? (
          <EmptyState icone="layers" titulo="Sem movimentações" descricao="Não há movimentações para o período/filtro." />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr><th>Data</th><th>Produto</th><th>Tipo</th><th className="text-right">Qtd</th><th className="text-right">Custo unit.</th><th>Usuário</th><th>Motivo</th></tr>
              </thead>
              <tbody>
                {movimentosFiltrados.map((mov) => (
                  <tr key={mov.id}>
                    <td>{formatDateTime(mov.dataMovimentacao)}</td>
                    <td>{mov.produtoNome ?? `#${mov.produtoId}`}</td>
                    <td><StatusBadge status={mov.tipo === 'Entrada' ? 'OK' : 'Saída'} /></td>
                    <td className="text-right">{formatNumber(mov.quantidade)}</td>
                    <td className="text-right">{mov.precoUnitario != null ? mov.precoUnitario.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) : '—'}</td>
                    <td>{mov.usuarioNome ?? '—'}</td>
                    <td className="text-muted">{mov.motivo || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
