import { Link } from 'react-router-dom';
import { useDocumentTitle } from '../hooks/useDebounce';
import { Icon } from '../components/Icon';

// ============================================================================
// Página 404 para rotas inexistentes dentro do layout autenticado.
// ============================================================================

export function NotFoundPage() {
  useDocumentTitle('Página não encontrada');

  return (
    <div className="page">
      <div className="empty-state not-found">
        <span className="empty-icon"><Icon name="alert" size={30} /></span>
        <h2>Página não encontrada</h2>
        <p>O endereço acessado não existe ou você não possui permissão para ele.</p>
        <Link className="btn btn-primary" to="/">Voltar ao início</Link>
      </div>
    </div>
  );
}
