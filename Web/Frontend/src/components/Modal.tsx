import { useEffect, type ReactNode } from 'react';
import { Icon } from './Icon';
// ============================================================================
// Modal acessível: fecha com Esc ou clique no overlay, bloqueia scroll do body.
// ============================================================================

interface ModalProps {
  aberto: boolean;
  titulo: string;
  subtitulo?: string;
  tamanho?: 'sm' | 'md' | 'lg' | 'xl';
  onFechar: () => void;
  children: ReactNode;
  footer?: ReactNode;
}

export function Modal({ aberto, titulo, subtitulo, tamanho = 'md', onFechar, children, footer }: ModalProps) {
  useEffect(() => {
    if (!aberto) return;

    const aoTeclar = (evento: KeyboardEvent) => {
      if (evento.key === 'Escape') onFechar();
    };

    document.addEventListener('keydown', aoTeclar);
    const overflowAnterior = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    return () => {
      document.removeEventListener('keydown', aoTeclar);
      document.body.style.overflow = overflowAnterior;
    };
  }, [aberto, onFechar]);

  if (!aberto) return null;

  return (
    <div className="modal-overlay" onMouseDown={(e) => e.target === e.currentTarget && onFechar()}>
      <div className={`modal modal-${tamanho}`} role="dialog" aria-modal="true" aria-label={titulo}>
        <header className="modal-header">
          <div>
            <h2>{titulo}</h2>
            {subtitulo && <p>{subtitulo}</p>}
          </div>
          <button type="button" className="icon-button" onClick={onFechar} aria-label="Fechar">
            <Icon name="close" />
          </button>
        </header>

        <div className="modal-body">{children}</div>

        {footer && <footer className="modal-footer">{footer}</footer>}
      </div>
    </div>
  );
}

interface ConfirmDialogProps {
  aberto: boolean;
  titulo: string;
  mensagem: string;
  confirmarLabel?: string;
  cancelarLabel?: string;
  perigo?: boolean;
  carregando?: boolean;
  /** Conteúdo extra exibido no corpo do diálogo (ex.: campo de motivo). */
  children?: ReactNode;
  onConfirmar: () => void;
  onCancelar: () => void;
}

export function ConfirmDialog({
  aberto,
  titulo,
  mensagem,
  confirmarLabel = 'Confirmar',
  cancelarLabel = 'Cancelar',
  perigo = false,
  carregando = false,
  children,
  onConfirmar,
  onCancelar
}: ConfirmDialogProps) {
  return (
    <Modal
      aberto={aberto}
      titulo={titulo}
      tamanho="sm"
      onFechar={onCancelar}
      footer={
        <>
          <button type="button" className="btn btn-ghost" onClick={onCancelar} disabled={carregando}>
            {cancelarLabel}
          </button>
          <button
            type="button"
            className={`btn ${perigo ? 'btn-danger' : 'btn-primary'}`}
            onClick={onConfirmar}
            disabled={carregando}
          >
            {carregando ? 'Processando...' : confirmarLabel}
          </button>
        </>
      }
    >
      <p className="confirm-text">{mensagem}</p>
      {children}
    </Modal>
  );
}
