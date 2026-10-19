import type { ReactNode } from 'react';
import { Icon, type IconName } from './Icon';

// ============================================================================
// Cabeçalhos, estados de carregamento/vazio e badges reaproveitados pelas telas.
// ============================================================================

interface PageHeaderProps {
  titulo: string;
  descricao?: string;
  acoes?: ReactNode;
}

export function PageHeader({ titulo, descricao, acoes }: PageHeaderProps) {
  return (
    <header className="page-header">
      <div>
        <h1>{titulo}</h1>
        {descricao && <p>{descricao}</p>}
      </div>
      {acoes && <div className="page-actions">{acoes}</div>}
    </header>
  );
}

export function Loading({ mensagem = 'Carregando dados...' }: { mensagem?: string }) {
  return (
    <div className="loading">
      <span className="spinner" aria-hidden="true" />
      <span>{mensagem}</span>
    </div>
  );
}

interface EmptyStateProps {
  icone?: IconName;
  titulo: string;
  descricao?: string;
  acao?: ReactNode;
}

export function EmptyState({ icone = 'box', titulo, descricao, acao }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <span className="empty-icon"><Icon name={icone} size={28} /></span>
      <h3>{titulo}</h3>
      {descricao && <p>{descricao}</p>}
      {acao}
    </div>
  );
}

export function StatusBadge({ status }: { status: string }) {
  const normalizado = status.toLowerCase();
  const classe =
    normalizado.includes('cancel') ? 'badge-danger'
      : normalizado.includes('aberto') || normalizado.includes('conclu') || normalizado === 'ok' ? 'badge-success'
        : normalizado.includes('baixo') || normalizado.includes('pendente') ? 'badge-warning'
          : 'badge-neutral';

  return <span className={`badge ${classe}`}>{status}</span>;
}

export function AtivoBadge({ ativo }: { ativo: boolean }) {
  return (
    <span className={`badge ${ativo ? 'badge-success' : 'badge-neutral'}`}>
      {ativo ? 'Ativo' : 'Inativo'}
    </span>
  );
}
