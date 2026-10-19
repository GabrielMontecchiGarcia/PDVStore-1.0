namespace PDVStore.Domain.Common;

/// <summary>
/// Validações de documentos com dígito verificador. Separadas em arquivo parcial
/// para manter a mesma classe Mascaras do projeto original.
/// </summary>
public static partial class Mascaras
{
    /// <summary>Valida CPF pelo algoritmo oficial (11 dígitos, módulo 11).</summary>
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

    /// <summary>Valida CNPJ pelo algoritmo oficial (14 dígitos, módulo 11).</summary>
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

    /// <summary>
    /// Valida inscrição estadual de SP (14 dígitos), mesma regra do original:
    /// pesos 6..2 sobre 13 dígitos, DV com regra 10→1 e 11→0.
    /// </summary>
    public static bool ValidarInscEstadual(string? ie)
    {
        if (string.IsNullOrWhiteSpace(ie)) return false;

        var numeros = SomenteDigitos(ie);
        if (numeros.Length != 14) return false;

        int[] pesos = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        int soma = 0;
        for (int i = 0; i < 13; i++)
            soma += (numeros[i] - '0') * pesos[i];

        int dv = 11 - (soma % 11);
        if (dv == 10) dv = 1;
        else if (dv == 11) dv = 0;

        return dv == (numeros[13] - '0');
    }
}
