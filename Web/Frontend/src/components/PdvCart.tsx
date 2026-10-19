import type { ItemCarrinho } from '../hooks/useCarrinho';
import { formatCurrency } from '../utils/format';
import { Icon } from './Icon';

// ============================================================================
// Carrinho do PDV com controle de quantidade, remoção e totais.
// ============================================================================

interface PdvCartProps {
  itens: ItemCarrinho[];
  subtotal: number;
  desconto: number;
  total: number;
  onAlterarQuantidade: (produtoId: number, quantidade: number) => void;
  onRemover: (produtoId: number) => void;
  onLimpar: () => void;
}

export function PdvCart({ itens, subtotal, desconto, total, onAlterarQuantidade, onRemover, onLimpar }: PdvCartProps) {
  return (
    <section className="card pdv-cart">
      <header className="card-header">
        <div>
          <h2>Carrinho</h2>
          <p>{itens.length} item(ns) na venda.</p>
        </div>
        {itens.length > 0 && (
          <button type="button" className="btn btn-ghost btn-sm" onClick={onLimpar}>Limpar</button>
        )}
      </header>

      <div className="card-body no-padding">
        {itens.length === 0 ? (
          <div className="pdv-cart-empty">
            <Icon name="cart" size={28} />
            <p>Nenhum item adicionado. Selecione um produto acima.</p>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Produto</th>
                  <th className="text-right">Preço</th>
                  <th className="text-center">Qtd</th>
                  <th className="text-right">Subtotal</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {itens.map((item) => (
                  <tr key={item.produtoId} className={item.quantidade > item.estoqueDisponivel ? 'row-error' : ''}>
                    <td>
                      <strong>{item.nome}</strong>
                      {item.quantidade > item.estoqueDisponivel && (
                        <small className="cell-error">Estoque disponível: {item.estoqueDisponivel}</small>
                      )}
                    </td>
                    <td className="text-right">{formatCurrency(item.precoUnitario)}</td>
                    <td className="text-center">
                      <div className="qty-control">
                        <button
                          type="button"
                          className="qty-button"
                          onClick={() => onAlterarQuantidade(item.produtoId, item.quantidade - 1)}
                          aria-label="Diminuir quantidade"
                        >
                          <Icon name="minus" size={14} />
                        </button>
                        <input
                          type="number"
                          min={1}
                          value={item.quantidade}
                          onChange={(e) => onAlterarQuantidade(item.produtoId, Number.parseInt(e.target.value, 10) || 0)}
                          aria-label={`Quantidade de ${item.nome}`}
                        />
                        <button
                          type="button"
                          className="qty-button"
                          onClick={() => onAlterarQuantidade(item.produtoId, item.quantidade + 1)}
                          aria-label="Aumentar quantidade"
                        >
                          <Icon name="plus" size={14} />
                        </button>
                      </div>
                    </td>
                    <td className="text-right"><strong>{formatCurrency(item.precoUnitario * item.quantidade)}</strong></td>
                    <td className="text-right">
                      <button
                        type="button"
                        className="icon-button danger"
                        onClick={() => onRemover(item.produtoId)}
                        title="Remover item"
                      >
                        <Icon name="trash" size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {itens.length > 0 && (
        <footer className="pdv-cart-footer">
          <span>Subtotal <strong>{formatCurrency(subtotal)}</strong></span>
          <span>Desconto <strong>- {formatCurrency(desconto)}</strong></span>
          <span className="total-inline">Total <strong>{formatCurrency(total)}</strong></span>
        </footer>
      )}
    </section>
  );
}
