import type { ItemRelatorio, Produto } from '../models';
import { formatCurrency, formatNumber } from '../utils/format';
import { EmptyState, StatusBadge } from './PageHeader';
import { ExportButtons } from './ExportButtons';

// ============================================================================
// Tabelas do dashboard (itens mais vendidos e alertas de estoque).
// ============================================================================

export function ItensMaisVendidosCard({
  itens,
  inicio,
  fim
}: {
  itens: ItemRelatorio[];
  inicio: string;
  fim: string;
}) {
  return (
    <section className="card">
      <header className="card-header">
        <div>
          <h2>Itens mais vendidos</h2>
          <p>Ranking dos 10 produtos com maior saída.</p>
        </div>
        <ExportButtons tipo="itens-mais-vendidos" params={{ inicio, fim }} />
      </header>
      <div className="card-body no-padding">
        {itens.length === 0 ? (
          <EmptyState icone="box" titulo="Sem vendas no período" />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Produto</th>
                  <th className="text-right">Qtd</th>
                  <th className="text-right">Valor</th>
                  <th>Estoque</th>
                </tr>
              </thead>
              <tbody>
                {itens.map((item) => (
                  <tr key={item.nomeProduto}>
                    <td>
                      <strong>{item.nomeProduto}</strong>
                      {item.codigoBarras && <small className="cell-sub">{item.codigoBarras}</small>}
                    </td>
                    <td className="text-right">{formatNumber(item.totalVendido)}</td>
                    <td className="text-right">{formatCurrency(item.valorTotalVendido)}</td>
                    <td><StatusBadge status={item.statusMinimo} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  );
}

export function AlertasEstoqueCard({ produtos }: { produtos: Produto[] }) {
  return (
    <section className="card">
      <header className="card-header">
        <div>
          <h2>Alertas de estoque mínimo</h2>
          <p>Produtos ativos na hora de repor.</p>
        </div>
        <ExportButtons tipo="estoque-minimo" />
      </header>
      <div className="card-body no-padding">
        {produtos.length === 0 ? (
          <EmptyState icone="check" titulo="Estoque em dia" descricao="Nenhum produto abaixo do mínimo." />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Produto</th>
                  <th className="text-right">Atual</th>
                  <th className="text-right">Mínimo</th>
                </tr>
              </thead>
              <tbody>
                {produtos.map((produto) => (
                  <tr key={produto.id}>
                    <td>{produto.nome}</td>
                    <td className="text-right text-danger"><strong>{produto.estoqueAtual}</strong></td>
                    <td className="text-right">{produto.estoqueMinimo}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  );
}
