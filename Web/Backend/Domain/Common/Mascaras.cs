using System.Globalization;
using System.Text.RegularExpressions;

namespace PDVStore.Domain.Common;

/// <summary>
/// Utilitários de máscara e validação de documentos brasileiros.
/// Portado de `Helpers/Mascaras.cs` do projeto Windows com as MESMAS regras,
/// para que os cadastros da versão web aceitem/rejeitem exatamente os mesmos dados.
/// </summary>
public static partial class Mascaras
{
    // Extrai apenas dígitos; base de todas as máscaras/validações.
    public static string SomenteDigitos(string? texto) =>
        new((texto ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>
    /// Converte texto digitado em decimal aceitando vírgula OU ponto.
    /// O último separador é tratado como decimal (comportamento do PDV original).
    /// Retorna valor padrão para entrada vazia e nunca retorna negativo.
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

    /// <summary>Formata progressivamente CPF (até 11) ou CNPJ (até 14 dígitos).</summary>
    public static string FormatarCpfCnpj(string? texto)
    {
        var d = SomenteDigitos(texto);
        if (string.IsNullOrEmpty(d)) return string.Empty;
        if (d.Length > 14) d = d[..14];

        if (d.Length <= 11)
        {
            if (d.Length >= 11) return $"{d.Substring(0, 3)}.{d.Substring(3, 3)}.{d.Substring(6, 3)}-{d.Substring(9, 2)}";
            if (d.Length >= 9) return $"{d.Substring(0, 3)}.{d.Substring(3, 3)}.{d.Substring(6)}";
            if (d.Length >= 6) return $"{d.Substring(0, 3)}.{d.Substring(3)}";
            return d;
        }

        if (d.Length >= 14) return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}";
        if (d.Length == 13) return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d[12]}";
        return $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}";
    }

    /// <summary>Formata progressivamente telefone fixo/celular brasileiro.</summary>
    public static string FormatarTelefone(string? texto)
    {
        var d = SomenteDigitos(texto);
        if (string.IsNullOrEmpty(d)) return string.Empty;
        if (d.Length > 11) d = d[..11];

        if (d.Length >= 11) return $"({d.Substring(0, 2)}) {d.Substring(2, 5)}-{d.Substring(7)}";
        if (d.Length >= 10) return $"({d.Substring(0, 2)}) {d.Substring(2, 4)}-{d.Substring(6)}";
        if (d.Length >= 7) return $"({d.Substring(0, 2)}) {d.Substring(2, 4)}-{d.Substring(6)}";
        if (d.Length >= 2) return $"({d.Substring(0, 2)}) {d.Substring(2)}";
        return d;
    }

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    /// <summary>Valida formato de e-mail (não garante existência).</summary>
    public static bool ValidarEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

    /// <summary>Normaliza CPF/CNPJ para armazenamento com máscara (como o original).</summary>
    public static string? NormalizarDocumento(string? documento)
    {
        var digitos = SomenteDigitos(documento);
        return string.IsNullOrEmpty(digitos) ? null : FormatarCpfCnpj(digitos);
    }

    /// <summary>Normaliza telefone para armazenamento com máscara.</summary>
    public static string? NormalizarTelefone(string? telefone)
    {
        var digitos = SomenteDigitos(telefone);
        return string.IsNullOrEmpty(digitos) ? null : FormatarTelefone(digitos);
    }
}
