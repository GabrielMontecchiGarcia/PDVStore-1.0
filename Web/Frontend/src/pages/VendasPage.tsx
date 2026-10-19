import { useMemo, useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useDocumentTitle } from '../hooks/useDebounce';
import { vendaService } from '../services/vendaService';
import { formatCurrency, formatDateTime, formatNumber, mensagemDeErro, primeiroDiaDoMesInput, hojeInput } from '../utils/format';
import { PageHeader, Loading, EmptyState, StatusBadge } from '../components/PageHeader';
import { ConfirmDialog } from '../components/Modal';
import { ExportButtons } from '../components/ExportButtons';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Consulta de vendas e cancelamento (Administrador).
// Expõe na web a regra que já existia no VendaService do sistema Windows
// (CancelarVendaAsync): devolve estoque, registra estorno e reduz o fiado.
// ============================================================================

export function VendasPage() {
  useDocumentTitle('Vendas');
  const { sucesso, erro } = useToast();

  const [inicio, setInicio] = useState(primeiroDiaDoMesInput());
  const [fim, setFim] = useState(hojeInput());
  const [paraCancelar, setParaCancelar] = useState<{ id: number; total: number } | null>(null);
  const [motivo, setMotivo] = useState('');
  const [cancelando, setCancelando] = useState(false);

  const { dados, carregando, erro: erroApi, recarregar } = useApi(
    () => vendaService.listar(inicio, fim),
    [inicio, fim]
  );

  const vendas = dados ?? [];

  const resumo = useMemo(() => {
    const concluidas = vendas.filter((v) => v.status === 'Concluida');
    const total = concluidas.reduce((soma, venda) => soma + venda.valorTotal, 0);
    const canceladas = vendas.filter((v) => v.status === 'Cancelada').length;

    return {
      total,
      quantidade: concluidas.length,
      canceladas,
      ticketMedio: concluidas.length > 0 ? total / concluidas.length : 0
    };
  }, [vendas]);

  const cancelar = async () => {
    if (!paraCancelar) return;
    setCancelando(true);
    try {
      await vendaService.cancelar(paraCancelar.id, motivo.trim() || undefined);
      sucesso(`Venda #${paraCancelar.id} cancelada. Estoque e fiado foram estornados.`);
      setParaCancelar(null);
      setMotivo('');
      await recarregar(true);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setCancelando(false);
    }
  };

  return (
    <div className="page">
      <PageHeader
        titulo="Vendas"
        descricao="Histórico de vendas do período e cancelamento com estorno de estoque."
        acoes={
          <>
            <ExportButtons tipo="vendas" params={{ inicio, fim }} />
            <div className="filter-bar">
              <label className="field field-inline">
                <span>De</span>
                <input type="date" value={inicio} onChange={(e) => setInicio(e.target.value)} />
              </label>
              <label className="field field-inline">
                <span>Até</span>
                <input type="date" value={fim} onChange={(e) => setFim(e.target.value)} />
              </label>
              <button type="button" className="btn btn-primary" onClick={() => void recarregar()} disabled={carregando}>
                <Icon name="refresh" size={16} />
                Atualizar
              </button>
            </div>
          </>
        }
      />

      {erroApi && <div className="alert alert-danger">{erroApi}</div>}

      <section className="stat-grid">
        <article className="stat-card">
          <span className="stat-icon stat-icon-green"><Icon name="cash" /></span>
          <div><small>Faturamento (concluídas)</small><strong>{formatCurrency(resumo.total)}</strong></div>
        </article>
        <article className="stat-card">
          <span className="stat-icon stat-icon-blue"><Icon name="receipt" /></span>
          <div><small>Vendas concluídas</small><strong>{formatNumber(resumo.quantidade)}</strong></div>
        </article>
        <article className="stat-card">
          <span className="stat-icon stat-icon-amber"><Icon name="cart" /></span>
          <div><small>Ticket médio</small><strong>{formatCurrency(resumo.ticketMedio)}</strong></div>
        </article>
        <article className="stat-card">
          <span className="stat-icon stat-icon-red"><Icon name="alert" /></span>
          <div><small>Vendas canceladas</small><strong>{formatNumber(resumo.canceladas)}</strong></div>
        </article>
      </section>

      <section className="card">
        <header className="card-header">
          <div>
            <h2>Vendas do período</h2>
            <p>Somente vendas concluídas e canceladas são listadas.</p>
          </div>
        </header>

        {carregando ? (
          <Loading />
        ) : vendas.length === 0 ? (
          <EmptyState icone="receipt" titulo="Nenhuma venda no período" descricao="Ajuste as datas ou registre vendas no PDV." />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th><th>Data</th><th>Cliente</th><th>Operador</th>
                  <th>Pagamento</th><th className="text-right">Itens</th>
                  <th className="text-right">Total</th><th>Status</th><th className="text-right">Ações</th>
                </tr>
              </thead>
              <tbody>
                {vendas.map((venda) => (
                  <tr key={venda.id}>
                    <td>{venda.id}</td>
                    <td>{formatDateTime(venda.dataVenda)}</td>
                    <td>{venda.clienteNome ?? <span className="text-muted">Não identificado</span>}</td>
                    <td>{venda.usuarioNome ?? '—'}</td>
                    <td>
                      {venda.formaPagamento}
                      {venda.pixTxId && <small className="cell-sub">TxId {venda.pixTxId.slice(0, 8)}…</small>}
                    </td>
                    <td className="text-right">{formatNumber(venda.itens.length)}</td>
                    <td className="text-right"><strong>{formatCurrency(venda.valorTotal)}</strong></td>
                    <td><StatusBadge status={venda.status} /></td>
                    <td className="text-right">
                      <button
                        type="button"
                        className="btn btn-outline btn-sm"
                        onClick={() => { setParaCancelar({ id: venda.id, total: venda.valorTotal }); setMotivo(''); }}
                        disabled={venda.status === 'Cancelada'}
                        title="Cancelar venda e estornar estoque/fiado"
                      >
                        Cancelar
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
        titulo={`Cancelar venda #${paraCancelar?.id ?? ''}`}
        mensagem={`A venda de ${formatCurrency(paraCancelar?.total ?? 0)} será cancelada: o estoque volta, o fiado é reduzido e uma movimentação de estorno é registrada.`}
        confirmarLabel="Cancelar venda"
        perigo
        carregando={cancelando}
        onConfirmar={() => void cancelar()}
        onCancelar={() => { setParaCancelar(null); setMotivo(''); }}
      >
        <label className="field">
          <span>Motivo (registrado em log)</span>
          <input value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Ex.: devolução do cliente" />
        </label>
      </ConfirmDialog>
    </div>
  );
}
