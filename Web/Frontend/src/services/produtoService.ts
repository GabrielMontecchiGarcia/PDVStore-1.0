import { api } from './api';
import type { MovimentacaoEstoque, Produto, ProdutoRequest } from '../models';

// ============================================================================
// Produtos, estoque e movimentações.
// ============================================================================

export const produtoService = {
  async listar(apenasAtivos = true, filtro = ''): Promise<Produto[]> {
    const { data } = await api.get<Produto[]>('/produtos', {
      params: { apenasAtivos, filtro: filtro || undefined }
    });
    return data;
  },

  async obter(id: number): Promise<Produto> {
    const { data } = await api.get<Produto>(`/produtos/${id}`);
    return data;
  },

  async porCodigoBarras(codigo: string): Promise<Produto> {
    const { data } = await api.get<Produto>(`/produtos/codigo-barras/${encodeURIComponent(codigo)}`);
    return data;
  },

  async criar(produto: ProdutoRequest): Promise<Produto> {
    const { data } = await api.post<Produto>('/produtos', produto);
    return data;
  },

  async atualizar(id: number, produto: ProdutoRequest): Promise<Produto> {
    const { data } = await api.put<Produto>(`/produtos/${id}`, produto);
    return data;
  },

  async desativar(id: number): Promise<void> {
    await api.delete(`/produtos/${id}`);
  },

  async historico(inicio?: string, fim?: string): Promise<MovimentacaoEstoque[]> {
    const { data } = await api.get<MovimentacaoEstoque[]>('/produtos/movimentacoes', {
      params: { inicio: inicio || undefined, fim: fim || undefined }
    });
    return data;
  },

  async entrada(id: number, quantidade: number, motivo?: string): Promise<void> {
    await api.post(`/produtos/${id}/estoque/entrada`, { quantidade, motivo });
  },

  async saida(id: number, quantidade: number, motivo?: string): Promise<void> {
    await api.post(`/produtos/${id}/estoque/saida`, { quantidade, motivo });
  }
};
