import type { MovimentacaoPorDia, VendaPorDia } from '../models';
import { formatCurrency, formatDateShort, formatNumber } from '../utils/format';

// ============================================================================
// Gráficos de barras em CSS/SVG leve, sem dependências externas.
// ============================================================================

export function VendasPorDiaChart({ dados }: { dados: VendaPorDia[] }) {
  if (dados.length === 0) {
    return <div className="chart-empty">Sem vendas no período.</div>;
  }

  const maximo = Math.max(...dados.map((d) => d.totalVendas), 1);

  return (
    <div className="bar-chart" role="img" aria-label="Vendas por dia">
      {dados.map((dia) => (
        <div className="bar-group" key={dia.dia}>
          <div className="bar-track">
            <div
              className="bar bar-primary"
              style={{ height: `${Math.max(4, (dia.totalVendas / maximo) * 100)}%` }}
              title={`${formatDateShort(dia.dia)}: ${formatCurrency(dia.totalVendas)}`}
            />
          </div>
          <span className="bar-label">{formatDateShort(dia.dia)}</span>
        </div>
      ))}
    </div>
  );
}

export function MovimentacoesChart({ dados }: { dados: MovimentacaoPorDia[] }) {
  if (dados.length === 0) {
    return <div className="chart-empty">Sem movimentações no período.</div>;
  }

  const maximo = Math.max(...dados.flatMap((d) => [d.entradas, d.saidas]), 1);

  return (
    <>
      <div className="chart-legend">
        <span><i className="dot dot-in" /> Entradas</span>
        <span><i className="dot dot-out" /> Saídas</span>
      </div>
      <div className="bar-chart" role="img" aria-label="Entradas e saídas de estoque">
        {dados.map((dia) => (
          <div className="bar-group" key={dia.dia}>
            <div className="bar-track bar-track-duplo">
              <div
                className="bar bar-in"
                style={{ height: `${Math.max(3, (dia.entradas / maximo) * 100)}%` }}
                title={`Entradas: ${formatNumber(dia.entradas)}`}
              />
              <div
                className="bar bar-out"
                style={{ height: `${Math.max(3, (dia.saidas / maximo) * 100)}%` }}
                title={`Saídas: ${formatNumber(dia.saidas)}`}
              />
            </div>
            <span className="bar-label">{formatDateShort(dia.dia)}</span>
          </div>
        ))}
      </div>
    </>
  );
}

export function PaymentBreakdown({ dados }: { dados: { forma: string; quantidade: number; total: number }[] }) {
  const total = dados.reduce((soma, item) => soma + item.total, 0);

  if (dados.length === 0) {
    return <div className="chart-empty">Sem pagamentos no período.</div>;
  }

  return (
    <div className="payment-list">
      {dados.map((item) => {
        const percentual = total > 0 ? (item.total / total) * 100 : 0;
        return (
          <div className="payment-row" key={item.forma}>
            <div className="payment-info">
              <strong>{item.forma}</strong>
              <span>{item.quantidade} venda(s)</span>
              <span className="payment-value">{formatCurrency(item.total)}</span>
            </div>
            <div className="progress">
              <div className="progress-bar" style={{ width: `${percentual}%` }} />
            </div>
          </div>
        );
      })}
      <div className="payment-total">
        <span>Total</span>
        <strong>{formatCurrency(total)}</strong>
      </div>
    </div>
  );
}
