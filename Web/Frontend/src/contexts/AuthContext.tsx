import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authService, sessaoStorage } from '../services/authService';
import type { PermissaoNome, Usuario } from '../models';

// ============================================================================
// Contexto de autenticação/sessão.
// Mantém o usuário logado, restaura a sessão a partir do token e expõe as
// verificações de perfil usadas pelas rotas protegidas e pelo menu.
// ============================================================================

interface AuthContextValue {
  usuario: Usuario | null;
  carregando: boolean;
  entrar: (nome: string, senha: string) => Promise<Usuario>;
  sair: () => void;
  temPermissao: (...perfis: PermissaoNome[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<Usuario | null>(() => sessaoStorage.obterUsuario());
  const [carregando, setCarregando] = useState(true);

  const sair = useCallback(() => {
    sessaoStorage.limpar();
    setUsuario(null);
  }, []);

  // Restaura a sessão validando o token atual com /auth/me.
  useEffect(() => {
    let ativo = true;

    const restaurar = async () => {
      if (!sessaoStorage.obterToken()) {
        if (ativo) setCarregando(false);
        return;
      }

      try {
        const atual = await authService.me();
        if (ativo) {
          setUsuario(atual);
          sessaoStorage.salvar(sessaoStorage.obterToken()!, atual);
        }
      } catch {
        if (ativo) sair();
      } finally {
        if (ativo) setCarregando(false);
      }
    };

    void restaurar();

    const aoPerderAutorizacao = () => sair();
    window.addEventListener('pdv:unauthorized', aoPerderAutorizacao);

    return () => {
      ativo = false;
      window.removeEventListener('pdv:unauthorized', aoPerderAutorizacao);
    };
  }, [sair]);

  const entrar = useCallback(async (nome: string, senha: string) => {
    const resposta = await authService.login(nome, senha);
    sessaoStorage.salvar(resposta.token, resposta.usuario);
    setUsuario(resposta.usuario);
    return resposta.usuario;
  }, []);

  const temPermissao = useCallback(
    (...perfis: PermissaoNome[]) => !!usuario && perfis.includes(usuario.permissao),
    [usuario]
  );

  const valor = useMemo(
    () => ({ usuario, carregando, entrar, sair, temPermissao }),
    [usuario, carregando, entrar, sair, temPermissao]
  );

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const contexto = useContext(AuthContext);
  if (!contexto) throw new Error('useAuth deve ser usado dentro de AuthProvider.');
  return contexto;
}
