// ============================================================================
// Download de arquivos gerados pela API (exportações PDF/Excel).
// ============================================================================

export const baixarArquivo = (conteudo: Blob, nomeArquivo: string): void => {
  const url = URL.createObjectURL(conteudo);
  const link = document.createElement('a');
  link.href = url;
  link.download = nomeArquivo;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
};

export const sufixoDataHora = (): string => {
  const agora = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${agora.getFullYear()}${pad(agora.getMonth() + 1)}${pad(agora.getDate())}_${pad(agora.getHours())}${pad(agora.getMinutes())}`;
};

/**
 * Monta a URL de um arquivo servido pela API (ex.: foto do usuário).
 * - Se a API estiver em outra origem (VITE_API_URL absoluta), usa a origem dela;
 * - Caso contrário, mantém a mesma origem (proxy do Vite em desenvolvimento
 *   ou API publicada no mesmo domínio em produção).
 */
export const urlDeArquivo = (caminho?: string | null): string | null => {
  if (!caminho) return null;

  const base = import.meta.env.VITE_API_URL?.trim();
  if (base && /^https?:\/\//i.test(base)) {
    return `${new URL(base).origin}${caminho}`;
  }

  return caminho;
};
