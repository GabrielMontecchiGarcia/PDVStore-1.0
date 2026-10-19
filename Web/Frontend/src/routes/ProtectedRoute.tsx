import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { PermissaoNome } from '../models';
import type { ReactNode } from 'react';

// ============================================================================
// Rotas protegidas: exigem autenticação e, quando informado, um dos perfis.
// ============================================================================

export const inicioDoPerfil = (permissao: PermissaoNome | undefined): string => {
  switch (permissao) {
    case 'Administrador':
      return '/dashboard';
    case 'Estoquista':
      return '/produtos';
    default:
      return '/pdv';
  }
};

export function Protegida({ perfis, children }: { perfis?: PermissaoNome[]; children?: ReactNode }) {
  const { usuario, carregando } = useAuth();
  const local = useLocation();

  if (carregando) {
    return (
      <div className="tela-carregando">
        <span className="spinner" />
        <span>Carregando sessão...</span>
      </div>
    );
  }

  if (!usuario) {
    return <Navigate to="/login" replace state={{ de: local.pathname }} />;
  }

  if (perfis && !perfis.includes(usuario.permissao)) {
    return <Navigate to={inicioDoPerfil(usuario.permissao)} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
}

export function RedirecionarInicio() {
  const { usuario } = useAuth();
  return <Navigate to={inicioDoPerfil(usuario?.permissao)} replace />;
}
