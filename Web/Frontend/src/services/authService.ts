import { api, TOKEN_KEY, USUARIO_KEY } from './api';
import type { LoginResponse, Usuario } from '../models';

// ============================================================================
// Serviços de autenticação e sessão.
// ============================================================================

export const authService = {
  async login(nome: string, senha: string): Promise<LoginResponse> {
    const { data } = await api.post<LoginResponse>('/auth/login', { nome, senha });
    return data;
  },

  async me(): Promise<Usuario> {
    const { data } = await api.get<Usuario>('/auth/me');
    return data;
  },

  async alterarSenha(senhaAtual: string, novaSenha: string): Promise<void> {
    await api.post('/auth/alterar-senha', { senhaAtual, novaSenha });
  }
};

// Persistência da sessão em sessionStorage: o token some ao fechar a aba e não
// é compartilhado entre abas (reduz exposição em caso de XSS no navegador).
export const sessaoStorage = {
  salvar(token: string, usuario: Usuario): void {
    sessionStorage.setItem(TOKEN_KEY, token);
    sessionStorage.setItem(USUARIO_KEY, JSON.stringify(usuario));
  },

  obterToken(): string | null {
    return sessionStorage.getItem(TOKEN_KEY);
  },

  obterUsuario(): Usuario | null {
    const bruto = sessionStorage.getItem(USUARIO_KEY);
    if (!bruto) return null;
    try {
      return JSON.parse(bruto) as Usuario;
    } catch {
      return null;
    }
  },

  limpar(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USUARIO_KEY);
  }
};
