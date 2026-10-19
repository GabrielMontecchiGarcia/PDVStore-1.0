// ============================================================================
// Formatação de valores, datas e textos (pt-BR).
// ============================================================================

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const numero = new Intl.NumberFormat('pt-BR');

export const formatCurrency = (valor: number | null | undefined): string =>
  moeda.format(Number(valor ?? 0));

export const formatNumber = (valor: number | null | undefined): string =>
  numero.format(Number(valor ?? 0));

// Datas vindas da API são UTC (ISO). Exibimos no fuso do navegador.
export const formatDateTime = (iso: string | null | undefined): string => {
  if (!iso) return '—';
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return '—';
  return data.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
};

export const formatDate = (iso: string | null | undefined): string => {
  if (!iso) return '—';
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return '—';
  return data.toLocaleDateString('pt-BR');
};

export const formatDateShort = (iso: string | null | undefined): string => {
  if (!iso) return '';
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return '';
  return data.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
};

// Converte uma data local para o formato aceito por <input type="date">.
export const toDateInput = (data: Date): string => {
  const ano = data.getFullYear();
  const mes = String(data.getMonth() + 1).padStart(2, '0');
  const dia = String(data.getDate()).padStart(2, '0');
  return `${ano}-${mes}-${dia}`;
};

export const hojeInput = (): string => toDateInput(new Date());

export const primeiroDiaDoMesInput = (): string => {
  const hoje = new Date();
  return toDateInput(new Date(hoje.getFullYear(), hoje.getMonth(), 1));
};

export const formatPercent = (parte: number, total: number): string => {
  if (!total) return '0%';
  return `${Math.round((parte / total) * 100)}%`;
};

// Mensagem amigável para erros desconhecidos capturados no frontend.
export const mensagemDeErro = (erro: unknown): string => {
  if (erro && typeof erro === 'object' && 'message' in erro) {
    return String((erro as { message: unknown }).message);
  }
  return 'Não foi possível concluir a operação.';
};
