import { api } from './api';
import type { Usuario } from '../models';

// ============================================================================
// Usuários (exclusivo do Administrador).
// ============================================================================

export interface CriarUsuarioPayload {
  nome: string;
  senha: string;
  confirmacaoSenha: string;
  permissao: number;
  fotoPath?: string | null;
}

export interface AtualizarUsuarioPayload {
  senha: string;
  confirmacaoSenha: string;
  permissao: number;
  fotoPath?: string | null;
}

export const usuarioService = {
  async listar(apenasAtivos = true, filtro = ''): Promise<Usuario[]> {
    const { data } = await api.get<Usuario[]>('/usuarios', {
      params: { apenasAtivos, filtro: filtro || undefined }
    });
    return data;
  },

  async obter(id: number): Promise<Usuario> {
    const { data } = await api.get<Usuario>(`/usuarios/${id}`);
    return data;
  },

  async criar(usuario: CriarUsuarioPayload): Promise<Usuario> {
    const { data } = await api.post<Usuario>('/usuarios', usuario);
    return data;
  },

  async atualizar(id: number, usuario: AtualizarUsuarioPayload): Promise<Usuario> {
    const { data } = await api.put<Usuario>(`/usuarios/${id}`, usuario);
    return data;
  },

  async desativar(id: number): Promise<void> {
    await api.delete(`/usuarios/${id}`);
  },

  /**
   * Envia a foto do usuário (equivalente ao botão "Escolher Foto" do sistema
   * Windows) e devolve o caminho público para gravar no cadastro.
   */
  async enviarFoto(arquivo: File): Promise<{ caminho: string }> {
    const dados = new FormData();
    dados.append('arquivo', arquivo);

    const { data } = await api.post<{ caminho: string }>('/usuarios/foto', dados);
    return data;
  }
};
