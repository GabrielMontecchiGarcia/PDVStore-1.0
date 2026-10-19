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
