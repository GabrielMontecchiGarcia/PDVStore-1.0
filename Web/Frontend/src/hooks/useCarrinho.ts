import { useCallback, useMemo, useState } from 'react';
import type { Produto } from '../models';
import { parseDecimal } from '../utils/masks';

// ============================================================================
// Estado do carrinho do PDV (equivalente ao PDVViewModel do sistema Windows):
// itens, desconto e total calculado em tempo real.
// ============================================================================

export interface ItemCarrinho {
  produtoId: number;
  nome: string;
  codigoBarras?: string | null;
  precoUnitario: number;
  quantidade: number;
  estoqueDisponivel: number;
}

export function useCarrinho() {
  const [itens, setItens] = useState<ItemCarrinho[]>([]);
  const [descontoTexto, setDescontoTexto] = useState('0,00');

  const desconto = useMemo(() => parseDecimal(descontoTexto), [descontoTexto]);

  const subtotal = useMemo(
    () => itens.reduce((soma, item) => soma + item.precoUnitario * item.quantidade, 0),
    [itens]
  );

  const total = Math.max(0, subtotal - desconto);

  const adicionar = useCallback((produto: Produto, quantidade: number) => {
    if (quantidade <= 0) return;

    setItens((atuais) => {
      const existente = atuais.find((i) => i.produtoId === produto.id);

      if (existente) {
        return atuais.map((i) =>
          i.produtoId === produto.id ? { ...i, quantidade: i.quantidade + quantidade } : i
        );
      }

      return [
        ...atuais,
        {
          produtoId: produto.id,
          nome: produto.nome,
          codigoBarras: produto.codigoBarras,
          precoUnitario: produto.preco,
          quantidade,
          estoqueDisponivel: produto.estoque
        }
      ];
    });
  }, []);

  const alterarQuantidade = useCallback((produtoId: number, quantidade: number) => {
    if (quantidade <= 0) {
      setItens((atuais) => atuais.filter((i) => i.produtoId !== produtoId));
      return;
    }

    setItens((atuais) =>
      atuais.map((i) => (i.produtoId === produtoId ? { ...i, quantidade } : i))
    );
  }, []);

  const remover = useCallback((produtoId: number) => {
    setItens((atuais) => atuais.filter((i) => i.produtoId !== produtoId));
  }, []);

  const limpar = useCallback(() => {
    setItens([]);
    setDescontoTexto('0,00');
  }, []);

  /** Detecta itens com quantidade acima do estoque (validação client-side). */
  const itensSemEstoque = useMemo(
    () => itens.filter((i) => i.quantidade > i.estoqueDisponivel),
    [itens]
  );

  return {
    itens,
    desconto,
    descontoTexto,
    setDescontoTexto,
    subtotal,
    total,
    adicionar,
    alterarQuantidade,
    remover,
    limpar,
    itensSemEstoque
  };
}
