using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PDVStore.Helpers
{
    public static class Mascaras
    {
        public static string SomenteDigitos(string? texto) =>
            new((texto ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>
        /// Converte texto digitado em decimal aceitando vírgula ou ponto como
        /// separador decimal, independentemente da cultura atual. O último
        /// separador presente no texto é considerado o separador decimal.
        /// </summary>
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

        /// <summary>
        /// Formata progressivamente os dígitos como CPF (11) ou CNPJ (14),
        /// conforme a quantidade de dígitos informados.
        /// </summary>
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

        /// <summary>
        /// Formata progressivamente os dígitos como telefone fixo/celular.
        /// </summary>
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

        public static bool ValidarEmail(string? email) =>
            !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

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