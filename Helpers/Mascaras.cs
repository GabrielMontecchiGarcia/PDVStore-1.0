using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PDVStore.Helpers
{
    public static class Mascaras
    {
        // Extrai SOMENTE os dígitos (0-9) de um texto, descartando qualquer pontuação
        // (pontos, traços, parênteses, espaços...). É a base de todas as funções
        // de máscara/validação deste helper (FormatarCpfCnpj, FormatarTelefone,
        // ValidarCpf, ValidarCnpj), pois elas trabalham sobre o número "cru".
        // Texto nulo é tratado como vazio. Retorna string vazia se não houver
        // dígitos. Método expression-bodied (corpo único).
        public static string SomenteDigitos(string? texto) =>
            new((texto ?? string.Empty).Where(char.IsDigit).ToArray());

        // Converte um texto digitado pelo usuário em valor decimal, aceitando vírgula
        // OU ponto como separador decimal independentemente da cultura atual —
        // comum em campos de valor no PDV. Regra: o ÚLTIMO separador presente no
        // texto é considerado o decimal (é o comportamento intuitivo ao digitar).
        // Texto vazio retorna o valorPadrao; se a conversão falhar ou o resultado
        // for negativo, retorna Math.Max(0, valorPadrao) para nunca aceitar valor
        // negativo. Quem chama: telas de entrada de preços/valores.
        public static decimal ParseDecimal(string? texto, decimal valorPadrao = 0)
        {
            var t = (texto ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(t)) return valorPadrao;

            int ultimoPonto = t.LastIndexOf('.');
            int ultimaVirgula = t.LastIndexOf(',');

            if (ultimaVirgula > ultimoPonto)
                t = t.Replace(".", string.Empty).Replace(',', '.');
            else if (ultimoPonto > ultimaVirgula)
                t = t.Replace(",", string.Empty);

            if (!decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal v))
                return Math.Max(0, valorPadrao);

            return Math.Max(0, v);
        }

        // Formata progressivamente os dígitos como CPF (11 dígitos) ou CNPJ (14
        // dígitos), aplicando a máscara conforme a quantidade digitada — assim o
        // campo "ganha a máscara" enquanto o usuário digita. Regras: usa
        // SomenteDigitos para limpar o texto; limita a 14 dígitos; até 11 dígitos
        // formata como CPF (###.###.###-##) e acima como CNPJ (##.###.###/####-##).
        // Devolve os dígitos sem máscara nos primeiros caracteres (ainda não há
        // pontos suficientes). Usado em campos de CPF/CNPJ de cliente/fornecedor.
        public static string FormatarCpfCnpj(string? texto)
        {
            var d = SomenteDigitos(texto);
            if (string.IsNullOrEmpty(d)) return string.Empty;
            if (d.Length > 14) d = d.Substring(0, 14);

            if (d.Length <= 11)
            {
                if (d.Length >= 11) return $"{d.Substring(0, 3)}.{d.Substring(3, 3)}.{d.Substring(6, 3)}-{d.Substring(9, 2)}";
                if (d.Length >= 9) return $"{d.Substring(0, 3)}.{d.Substring(3, 3)}.{d.Substring(6)}";
                if (d.Length >= 6) return $"{d.Substring(0, 3)}.{d.Substring(3)}";
                return d; // 1 a 5 dígitos ainda sem máscara
            }

            if (d.Length >= 14) return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}";
            if (d.Length == 13) return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d[12]}";
            return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}";
        }

        // Formata progressivamente os dígitos como telefone fixo/celular brasileiro
        // enquanto o usuário digita. Regras: usa SomenteDigitos; limita a 11
        // dígitos; a partir de 2 dígitos aplica o DDD "(XX)"; e vai adicionando a
        // máscara de 7 (fixo), 8 (celular antigo) e 9 (celular com 9) dígitos.
        // Retorna apenas os dígitos antes de 2 caracteres. Usado nos campos de
        // telefone de clientes/fornecedores.
        public static string FormatarTelefone(string? texto)
        {
            var d = SomenteDigitos(texto);
            if (string.IsNullOrEmpty(d)) return string.Empty;
            if (d.Length > 11) d = d.Substring(0, 11);

            if (d.Length >= 11) return $"({d.Substring(0, 2)}) {d.Substring(2, 5)}-{d.Substring(7)}";
            if (d.Length >= 10) return $"({d.Substring(0, 2)}) {d.Substring(2, 4)}-{d.Substring(6)}";
            if (d.Length >= 7) return $"({d.Substring(0, 2)}) {d.Substring(2, 4)}-{d.Substring(6)}";
            if (d.Length >= 6) return $"({d.Substring(0, 2)}) {d.Substring(2)}";
            if (d.Length >= 2) return $"({d.Substring(0, 2)}) {d.Substring(2)}";
            return d;
        }

        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        // Valida se um texto é um e-mail em formato aceitável. Usa a regex
        // pré-compilada EmailRegex (padrão: algo@algo.algo) após remover espaços.
        // Não garante que o e-mail exista, apenas a forma. Texto vazio/nulo é
        // inválido. Quem chama: cadastros de cliente/fornecedor/usuario para
        // avisar sobre e-mail mal formatado.
        public static bool ValidarEmail(string? email) =>
            !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

        // Valida um CPF usando o algoritmo oficial do dígito verificador. Regras:
        // precisa ter 11 dígitos e não pode ser todos iguais (ex.: 111.111.111-11).
        // Calcula o 1º dígito verificador (pesos 10..2) e o 2º (pesos 11..2
        // recalculados) e compara com os dois últimos dígitos informados. Depende de
        // SomenteDigitos e do cálculo de módulo 11. Quem chama: cadastros de
        // clientes para barrar CPF inválido.
        public static bool ValidarCpf(string? cpf)
        {
            var d = SomenteDigitos(cpf);
            if (d.Length != 11 || d.Distinct().Count() == 1) return false;

            int soma = 0;
            for (int i = 0; i < 9; i++) soma += (d[i] - '0') * (10 - i);
            int dig1 = soma % 11 < 2 ? 0 : 11 - soma % 11;

            soma = 0;
            for (int i = 0; i < 10; i++) soma += (d[i] - '0') * (11 - i);
            int dig2 = soma % 11 < 2 ? 0 : 11 - soma % 11;

            return d[9] - '0' == dig1 && d[10] - '0' == dig2;
        }

        // Valida um CNPJ usando o algoritmo oficial do dígito verificador. Regras:
        // precisa ter 14 dígitos e não pode ser todos iguais. Aplica os pesos
        // específicos do CNPJ (5..2 sobre 12 dígitos para o 1º dígito; 6..2 sobre
        // 13 dígitos para o 2º) com módulo 11 e compara com os dois últimos
        // dígitos. Depende de SomenteDigitos. Quem chama: cadastros de
        // fornecedores para barrar CNPJ inválido.
        public static bool ValidarCnpj(string? cnpj)
        {
            var d = SomenteDigitos(cnpj);
            if (d.Length != 14 || d.Distinct().Count() == 1) return false;

            int[] peso1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] peso2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            int soma1 = 0;
            for (int i = 0; i < 12; i++) soma1 += (d[i] - '0') * peso1[i];
            int dig1 = soma1 % 11 < 2 ? 0 : 11 - soma1 % 11;

            int soma2 = 0;
            for (int i = 0; i < 13; i++) soma2 += (d[i] - '0') * peso2[i];
            int dig2 = soma2 % 11 < 2 ? 0 : 11 - soma2 % 11;

            return d[12] - '0' == dig1 && d[13] - '0' == dig2;
        }
    }
}