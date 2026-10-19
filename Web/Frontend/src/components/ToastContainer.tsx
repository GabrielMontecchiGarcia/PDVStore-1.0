import { useToast } from '../contexts/ToastContext';
import { Icon } from './Icon';

// ============================================================================
// Contêiner de notificações (canto superior direito).
// ============================================================================

export function ToastContainer() {
  const { toasts, remover } = useToast();

  if (toasts.length === 0) return null;

  return (
    <div className="toast-container" aria-live="polite">
      {toasts.map((toast) => (
        <div key={toast.id} className={`toast toast-${toast.tipo}`}>
          <span className="toast-icon">
            <Icon name={toast.tipo === 'success' ? 'check' : toast.tipo === 'error' ? 'alert' : 'info'} size={18} />
          </span>
          <span className="toast-message">{toast.mensagem}</span>
          <button type="button" className="toast-close" onClick={() => remover(toast.id)} aria-label="Fechar">
            <Icon name="close" size={16} />
          </button>
        </div>
      ))}
    </div>
  );
}
