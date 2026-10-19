// ============================================================================
// Máscaras e conversões de entrada — mesmas regras de Helpers/Mascaras.cs.
// ============================================================================

export const onlyDigits = (texto: string | null | undefined): string =>
  (texto ?? '').replace(/\D/g, '');

/** Formata progressivamente CPF (até 11) ou CNPJ (até 14 dígitos). */
export const formatCpfCnpj = (texto: string | null | undefined): string => {
  let d = onlyDigits(texto);
  if (!d) return '';
  if (d.length > 14) d = d.slice(0, 14);

  if (d.length <= 11) {
    if (d.length >= 11) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9, 11)}`;
    if (d.length >= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
    if (d.length >= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
    return d;
  }

  if (d.length >= 14) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12, 14)}`;
  if (d.length === 13) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d[12]}`;
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}`;
};

/** Formata progressivamente telefone fixo/celular brasileiro. */
export const formatPhone = (texto: string | null | undefined): string => {
  let d = onlyDigits(texto);
  if (!d) return '';
  if (d.length > 11) d = d.slice(0, 11);

  if (d.length >= 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length >= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  if (d.length >= 7) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  if (d.length >= 2) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  return d;
};

/**
 * Converte texto digitado em número decimal, aceitando vírgula ou ponto.
 * O último separador é o decimal (mesma regra do PDV Windows).
 */
export const parseDecimal = (texto: string | null | undefined): number => {
  let t = (texto ?? '').trim();
  if (!t) return 0;

  const ultimoPonto = t.lastIndexOf('.');
  const ultimaVirgula = t.lastIndexOf(',');

  if (ultimaVirgula > ultimoPonto) t = t.replace(/\./g, '').replace(',', '.');
  else if (ultimoPonto > ultimaVirgula) t = t.replace(/,/g, '');

  const valor = Number.parseFloat(t);
  return Number.isFinite(valor) ? Math.max(0, valor) : 0;
};

/** Converte o número para texto com vírgula (inputs monetários). */
export const toDecimalInput = (valor: number | null | undefined): string =>
  Number(valor ?? 0).toFixed(2).replace('.', ',');

/** Valida CPF pelo algoritmo oficial (11 dígitos, módulo 11). */
export const validarCpf = (texto: string): boolean => {
  const d = onlyDigits(texto);
  if (d.length !== 11 || /^(\d)\1{10}$/.test(d)) return false;

  let soma = 0;
  for (let i = 0; i < 9; i++) soma += Number(d[i]) * (10 - i);
  let dig1 = soma % 11 < 2 ? 0 : 11 - (soma % 11);

  soma = 0;
  for (let i = 0; i < 10; i++) soma += Number(d[i]) * (11 - i);
  const dig2 = soma % 11 < 2 ? 0 : 11 - (soma % 11);

  return Number(d[9]) === dig1 && Number(d[10]) === dig2;
};

/** Valida CNPJ pelo algoritmo oficial (14 dígitos, módulo 11). */
export const validarCnpj = (texto: string): boolean => {
  const d = onlyDigits(texto);
  if (d.length !== 14 || /^(\d)\1{13}$/.test(d)) return false;

  const peso1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const peso2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

  let soma = 0;
  for (let i = 0; i < 12; i++) soma += Number(d[i]) * peso1[i];
  const dig1 = soma % 11 < 2 ? 0 : 11 - (soma % 11);

  soma = 0;
  for (let i = 0; i < 13; i++) soma += Number(d[i]) * peso2[i];
  const dig2 = soma % 11 < 2 ? 0 : 11 - (soma % 11);

  return Number(d[12]) === dig1 && Number(d[13]) === dig2;
};

/** Valida inscrição estadual de SP (14 dígitos), mesma regra do backend. */
export const validarInscricaoEstadual = (texto: string): boolean => {
  const d = onlyDigits(texto);
  if (d.length !== 14) return false;

  const pesos = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  let soma = 0;
  for (let i = 0; i < 13; i++) soma += Number(d[i]) * pesos[i];

  let dv = 11 - (soma % 11);
  if (dv === 10) dv = 1;
  else if (dv === 11) dv = 0;

  return dv === Number(d[13]);
};

export const validarEmail = (texto: string): boolean =>
  /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(texto.trim());

