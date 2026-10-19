import { api } from './api';
import type { Cliente, ClienteRequest, Fornecedor, FornecedorRequest } from '../models';

// ============================================================================
// Clientes (inclui fiado) e fornecedores.
// ============================================================================

export const clienteService = {
  async listar(apenasAtivos = true, filtro = ''): Promise<Cliente[]> {
    const { data } = await api.get<Cliente[]>('/clientes', {
      params: { apenasAtivos, filtro: filtro || undefined }
    });
    return data;
  },

  async obter(id: number): Promise<Cliente> {
    const { data } = await api.get<Cliente>(`/clientes/${id}`);
    return data;
  },

  async criar(cliente: ClienteRequest): Promise<Cliente> {
    const { data } = await api.post<Cliente>('/clientes', cliente);
    return data;
  },

  async atualizar(id: number, cliente: ClienteRequest): Promise<Cliente> {
    const { data } = await api.put<Cliente>(`/clientes/${id}`, cliente);
    return data;
  },

  async desativar(id: number): Promise<void> {
    await api.delete(`/clientes/${id}`);
  },

  async receberFiado(id: number, valor: number): Promise<Cliente> {
    const { data } = await api.post<Cliente>(`/clientes/${id}/receber-fiado`, { valor });
    return data;
  }
};

export const fornecedorService = {
  async listar(apenasAtivos = true, filtro = ''): Promise<Fornecedor[]> {
    const { data } = await api.get<Fornecedor[]>('/fornecedores', {
      params: { apenasAtivos, filtro: filtro || undefined }
    });
    return data;
  },

  async obter(id: number): Promise<Fornecedor> {
    const { data } = await api.get<Fornecedor>(`/fornecedores/${id}`);
    return data;
  },

  async criar(fornecedor: FornecedorRequest): Promise<Fornecedor> {
    const { data } = await api.post<Fornecedor>('/fornecedores', fornecedor);
    return data;
  },

  async atualizar(id: number, fornecedor: FornecedorRequest): Promise<Fornecedor> {
    const { data } = await api.put<Fornecedor>(`/fornecedores/${id}`, fornecedor);
    return data;
  },

  async desativar(id: number): Promise<void> {
    await api.delete(`/fornecedores/${id}`);
  }
};
