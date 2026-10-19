import { useEffect, useState } from 'react';

// ============================================================================
// Hooks utilitários.
// ============================================================================

/** Atrasa a atualização de um valor (usado nas buscas enquanto digita). */
export function useDebounce<T>(valor: T, atrasoMs = 350): T {
  const [debounced, setDebounced] = useState(valor);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(valor), atrasoMs);
    return () => window.clearTimeout(timer);
  }, [valor, atrasoMs]);

  return debounced;
}

/** Atualiza o título da aba do navegador. */
export function useDocumentTitle(titulo: string): void {
  useEffect(() => {
    document.title = `${titulo} · PDVStore`;
  }, [titulo]);
}
