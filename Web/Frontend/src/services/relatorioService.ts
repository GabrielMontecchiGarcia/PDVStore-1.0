import { api } from './api';
import type { Dashboard, ItemRelatorio, Produto } from '../models';

// ============================================================================
// Dashboard, relatórios e exportações (PDF/Excel).
// ============================================================================

export const relatorioService = {
  async dashboard(inicio?: string, fim?: string): Promise<Dashboard> {
    const { data } = await api.get<Dashboard>('/dashboard', {
      params: { inicio: inicio || undefined, fim: fim || undefined }
    });
    return data;
  },

  async itensMaisVendidos(inicio?: string, fim?: string, topN = 10): Promise<ItemRelatorio[]> {
    const { data } = await api.get<ItemRelatorio[]>('/relatorios/itens-mais-vendidos', {
      params: { inicio: inicio || undefined, fim: fim || undefined, topN }
    });
    return data;
  },

  async estoqueMinimo(): Promise<Produto[]> {
    const { data } = await api.get<Produto[]>('/relatorios/estoque-minimo');
    return data;
  },

  async totalVendas(): Promise<number> {
    const { data } = await api.get<{ total: number }>('/relatorios/total-vendas');
    return data.total;
  },

  /** Baixa o arquivo exportado pela API (Excel ou PDF). */
  async exportar(
    tipo: string,
    formato: 'excel' | 'pdf',
    params: { inicio?: string; fim?: string; apenasAtivos?: boolean } = {}
  ): Promise<Blob> {
    const { data } = await api.get<Blob>(`/relatorios/exportar/${tipo}/${formato}`, {
      params,
      responseType: 'blob'
    });
    return data;
  }
};
