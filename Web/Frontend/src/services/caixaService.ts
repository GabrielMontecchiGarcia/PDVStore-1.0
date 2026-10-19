import { api } from './api';
import type { Caixa } from '../models';

// ============================================================================
// Caixa: consulta do caixa aberto, histórico, abertura, sangria e fechamento.
// ============================================================================

export const caixaService = {
  /** Retorna null quando não há caixa aberto (API responde 204). */
  async obterAberto(): Promise<Caixa | null> {
    const { data, status } = await api.get<Caixa>('/caixas/aberto');
    if (status === 204 || !data) return null;
    return data;
  },

  async listar(): Promise<Caixa[]> {
    const { data } = await api.get<Caixa[]>('/caixas');
    return data;
  },

  async abrir(valorInicial: number): Promise<Caixa> {
    const { data } = await api.post<Caixa>('/caixas/abrir', { valorInicial });
    return data;
  },

  async sangria(id: number, valor: number): Promise<void> {
    await api.post(`/caixas/${id}/sangria`, { valor });
  },

  async fechar(id: number): Promise<void> {
    await api.post(`/caixas/${id}/fechar`);
  }
};
