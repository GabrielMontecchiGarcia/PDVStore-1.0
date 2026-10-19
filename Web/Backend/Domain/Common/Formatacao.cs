namespace PDVStore.Domain.Common;

/// <summary>
/// Formatação de valores em Real (pt-BR) usada nas mensagens de negócio.
/// Usa fallback invariante quando a cultura pt-BR não está disponível (ambientes
/// com globalization invariant), garantindo que a mensagem nunca quebre a venda.
/// </summary>
public static class Formatacao
{
    public static string Moeda(decimal valor)
    {
        try
        {
            return valor.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        }
        catch (Exception)
        {
            return "R$ " + valor.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
