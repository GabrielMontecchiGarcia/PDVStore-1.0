import { api } from './api';
import type { Venda, VendaRequest } from '../models';

// ============================================================================
// Vendas: registro no PDV, consulta e cancelamento.
// ============================================================================

export const vendaService = {
  async registrar(venda: VendaRequest): Promise<Venda> {
    const { data } = await api.post<Venda>('/vendas', venda);
    return data;
  },

  async obter(id: number): Promise<Venda> {
    const { data } = await api.get<Venda>(`/vendas/${id}`);
    return data;
  },

  async listar(inicio?: string, fim?: string, caixaId?: number): Promise<Venda[]> {
    const { data } = await api.get<Venda[]>('/vendas', {
      params: { inicio: inicio || undefined, fim: fim || undefined, caixaId }
    });
    return data;
  },

  async cancelar(id: number, motivo?: string): Promise<void> {
    await api.post(`/vendas/${id}/cancelar`, { motivo });
  }
};
