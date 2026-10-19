import { useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useDocumentTitle } from '../hooks/useDebounce';
import { relatorioService } from '../services/relatorioService';
import { formatCurrency, formatNumber, primeiroDiaDoMesInput, hojeInput } from '../utils/format';
import { PageHeader, Loading, EmptyState } from '../components/PageHeader';
import { MovimentacoesChart, PaymentBreakdown, VendasPorDiaChart } from '../components/Charts';
import { AlertasEstoqueCard, ItensMaisVendidosCard } from '../components/DashboardTables';
import { Icon } from '../components/Icon';

// ============================================================================
// Dashboard & Relatórios. Mesmos indicadores do DashboardViewModel original:
// faturamento, quantidade de vendas, pagamentos por forma, itens mais vendidos,
// alertas de estoque mínimo e gráficos de vendas/movimentações.
// ============================================================================

export function DashboardPage() {
  useDocumentTitle('Dashboard');

  const [inicio, setInicio] = useState(primeiroDiaDoMesInput());
  const [fim, setFim] = useState(hojeInput());

  const { dados, carregando, erro, recarregar } = useApi(
    () => relatorioService.dashboard(inicio, fim),
    [inicio, fim]
  );

  const ticketMedio = dados && dados.quantidadeVendas > 0
    ? dados.totalPeriodo / dados.quantidadeVendas
    : 0;

  return (
    <div className="page">
      <PageHeader
        titulo="Dashboard & Relatórios"
        descricao="Visão consolidada das vendas, recebimentos e estoque no período."
        acoes={
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
        }
      />

      {erro && <div className="alert alert-danger">{erro}</div>}

      {carregando && !dados ? (
        <Loading mensagem="Carregando indicadores..." />
      ) : !dados ? (
        <EmptyState icone="dashboard" titulo="Nenhum dado disponível" descricao="Ajuste o período e tente novamente." />
      ) : (
        <>
          <section className="stat-grid">
            <article className="stat-card">
              <span className="stat-icon stat-icon-green"><Icon name="cash" /></span>
              <div>
                <small>Faturamento no período</small>
                <strong>{formatCurrency(dados.totalPeriodo)}</strong>
              </div>
            </article>
            <article className="stat-card">
              <span className="stat-icon stat-icon-blue"><Icon name="receipt" /></span>
              <div>
                <small>Vendas concluídas</small>
                <strong>{formatNumber(dados.quantidadeVendas)}</strong>
              </div>
            </article>
            <article className="stat-card">
              <span className="stat-icon stat-icon-amber"><Icon name="cart" /></span>
              <div>
                <small>Ticket médio</small>
                <strong>{formatCurrency(ticketMedio)}</strong>
              </div>
            </article>
            <article className="stat-card">
              <span className="stat-icon stat-icon-red"><Icon name="alert" /></span>
              <div>
                <small>Alertas de estoque</small>
                <strong>{formatNumber(dados.alertasEstoque.length)}</strong>
              </div>
            </article>
          </section>

          <div className="grid-2">
            <section className="card">
              <header className="card-header">
                <div>
                  <h2>Vendas por dia</h2>
                  <p>Faturamento diário no período selecionado.</p>
                </div>
              </header>
              <div className="card-body">
                <VendasPorDiaChart dados={dados.vendasPorDia} />
              </div>
            </section>

            <section className="card">
              <header className="card-header">
                <div>
                  <h2>Pagamentos por forma</h2>
                  <p>Distribuição do faturamento por meio de pagamento.</p>
                </div>
              </header>
              <div className="card-body">
                <PaymentBreakdown dados={dados.pagamentosPorForma} />
              </div>
            </section>
          </div>

          <div className="grid-2">
            <ItensMaisVendidosCard itens={dados.itensMaisVendidos} inicio={inicio} fim={fim} />
            <AlertasEstoqueCard produtos={dados.alertasEstoque} />
          </div>

          <section className="card">
            <header className="card-header">
              <div>
                <h2>Entradas e saídas de produtos</h2>
                <p>Movimentações de estoque no período (compras, vendas e ajustes).</p>
              </div>
            </header>
            <div className="card-body">
              <MovimentacoesChart dados={dados.movimentacoesPorDia} />
            </div>
          </section>
        </>
      )}
    </div>
  );
}
