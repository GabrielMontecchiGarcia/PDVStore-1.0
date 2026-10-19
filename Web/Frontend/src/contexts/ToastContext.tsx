import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';

// ============================================================================
// Contexto de notificações (toasts) para feedback de sucesso/erro das operações.
// ============================================================================

export type TipoToast = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  tipo: TipoToast;
  mensagem: string;
}

interface ToastContextValue {
  toasts: Toast[];
  sucesso: (mensagem: string) => void;
  erro: (mensagem: string) => void;
  info: (mensagem: string) => void;
  remover: (id: number) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const proximoId = useRef(1);

  const remover = useCallback((id: number) => {
    setToasts((atuais) => atuais.filter((t) => t.id !== id));
  }, []);

  const adicionar = useCallback(
    (tipo: TipoToast, mensagem: string) => {
      const id = proximoId.current++;
      setToasts((atuais) => [...atuais, { id, tipo, mensagem }]);
      window.setTimeout(() => remover(id), 5500);
    },
    [remover]
  );

  const sucesso = useCallback((mensagem: string) => adicionar('success', mensagem), [adicionar]);
  const erro = useCallback((mensagem: string) => adicionar('error', mensagem), [adicionar]);
  const info = useCallback((mensagem: string) => adicionar('info', mensagem), [adicionar]);

  const valor = useMemo(
    () => ({ toasts, sucesso, erro, info, remover }),
    [toasts, sucesso, erro, info, remover]
  );

  return <ToastContext.Provider value={valor}>{children}</ToastContext.Provider>;
}

export function useToast(): ToastContextValue {
  const contexto = useContext(ToastContext);
  if (!contexto) throw new Error('useToast deve ser usado dentro de ToastProvider.');
  return contexto;
}
