import type { KeyboardEvent } from 'react';
import type { Produto } from '../models';
import { formatCurrency } from '../utils/format';
import { Icon } from './Icon';
import { EmptyState, Loading } from './PageHeader';

// ============================================================================
// Catálogo de produtos do PDV: busca por nome/código de barras, seleção,
// quantidade e botão Adicionar (fluxo equivalente ao frmPDV).
// ============================================================================

interface PdvProductListProps {
  produtos: Produto[];
  busca: string;
  onBusca: (valor: string) => void;
  carregando: boolean;
  selecionado: Produto | null;
  onSelecionar: (produto: Produto) => void;
  quantidade: string;
  onQuantidade: (valor: string) => void;
  onAdicionar: () => void;
  onBuscarEnter: () => void;
}

export function PdvProductList({
  produtos,
  busca,
  onBusca,
  carregando,
  selecionado,
  onSelecionar,
  quantidade,
  onQuantidade,
  onAdicionar,
  onBuscarEnter
}: PdvProductListProps) {
  const aoTeclar = (evento: KeyboardEvent<HTMLInputElement>) => {
    if (evento.key === 'Enter') {
      evento.preventDefault();
      onBuscarEnter();
    }
  };

  return (
    <section className="card pdv-catalog">
      <header className="card-header">
        <div>
          <h2>Produtos</h2>
          <p>Busque por nome ou código de barras e selecione o item.</p>
        </div>
      </header>

      <div className="pdv-search">
        <div className="search-box">
          <Icon name="search" size={18} />
          <input
            type="text"
            value={busca}
            onChange={(e) => onBusca(e.target.value)}
            onKeyDown={aoTeclar}
            placeholder="Buscar produto ou código de barras (Enter)"
            aria-label="Buscar produto"
          />
        </div>

        <label className="field field-inline pdv-qtd">
          <span>Qtd</span>
          <input
            type="number"
            min={1}
            value={quantidade}
            onChange={(e) => onQuantidade(e.target.value)}
            aria-label="Quantidade"
          />
        </label>

        <button type="button" className="btn btn-primary" onClick={onAdicionar} disabled={!selecionado}>
          <Icon name="plus" size={16} />
          Adicionar
        </button>
      </div>

      {carregando ? (
        <Loading mensagem="Carregando produtos..." />
      ) : produtos.length === 0 ? (
        <EmptyState icone="box" titulo="Nenhum produto encontrado" descricao="Tente outro nome ou código de barras." />
      ) : (
        <div className="table-wrapper pdv-product-table">
          <table className="table table-hover">
            <thead>
              <tr>
                <th>Produto</th>
                <th>Código</th>
                <th className="text-right">Preço</th>
                <th className="text-right">Estoque</th>
              </tr>
            </thead>
            <tbody>
              {produtos.map((produto) => (
                <tr
                  key={produto.id}
                  className={selecionado?.id === produto.id ? 'row-selected' : ''}
                  onClick={() => onSelecionar(produto)}
                >
                  <td><strong>{produto.nome}</strong></td>
                  <td className="text-muted">{produto.codigoBarras || '—'}</td>
                  <td className="text-right">{formatCurrency(produto.preco)}</td>
                  <td className={`text-right ${produto.estoque <= produto.estoqueMinimo ? 'text-danger' : ''}`}>
                    {produto.estoque}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
