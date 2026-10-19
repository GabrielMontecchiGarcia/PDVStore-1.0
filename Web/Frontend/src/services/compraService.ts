import { api } from './api';
import type { Compra, CompraRequest } from '../models';

// ============================================================================
// Compras de mercadoria: registro (entrada de estoque), consulta e cancelamento.
// ============================================================================

export const compraService = {
  async registrar(compra: CompraRequest): Promise<Compra> {
    const { data } = await api.post<Compra>('/compras', compra);
    return data;
  },

  async obter(id: number): Promise<Compra> {
    const { data } = await api.get<Compra>(`/compras/${id}`);
    return data;
  },

  async listar(inicio?: string, fim?: string): Promise<Compra[]> {
    const { data } = await api.get<Compra[]>('/compras', {
      params: { inicio: inicio || undefined, fim: fim || undefined }
    });
    return data;
  },

  async cancelar(id: number): Promise<void> {
    await api.post(`/compras/${id}/cancelar`);
  }
};
