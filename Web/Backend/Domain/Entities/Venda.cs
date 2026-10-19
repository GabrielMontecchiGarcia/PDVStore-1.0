namespace PDVStore.Domain.Entities;

/// <summary>
/// Cabeçalho da venda. Replica a entidade do projeto Windows.
/// Regras preservadas:
/// - FormaPagamento é texto (Dinheiro, PIX, Cartão Crédito, Cartão Débito, Fiado);
/// - Status inicia "Concluida" e vira "Cancelada" no estorno;
/// - CaixaId default 1 garante compatibilidade com vendas legadas;
/// - ClienteId é opcional (venda não identificada);
/// - PixTxId é preenchido pelo mock de pagamento apenas para PIX.
/// </summary>
public class Venda : IHasId
{
    public int Id { get; set; }
    public int UsuarioCaixaId { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public decimal ValorTotal { get; set; }
    public decimal Desconto { get; set; }
    public string FormaPagamento { get; set; } = FormasPagamento.Dinheiro;
    public string? PixTxId { get; set; }
    public string? Status { get; set; } = StatusVenda.Concluida;
    public int CaixaId { get; set; } = 1;
    public int? ClienteId { get; set; }

    public UsuarioCaixa? UsuarioCaixa { get; set; }
    public Caixa? Caixa { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
}

/// <summary>Status possíveis de uma venda (mesmas strings do banco original).</summary>
public static class StatusVenda
{
    public const string Concluida = "Concluida";
    public const string Cancelada = "Cancelada";
    public const string Pendente = "Pendente";
}

/// <summary>
/// Formas de pagamento aceitas. As strings são exatamente as usadas pelo
/// sistema Windows (inclusive acentos), pois são gravadas em Vendas.FormaPagamento.
/// </summary>
public static class FormasPagamento
{
    public const string Dinheiro = "Dinheiro";
    public const string Pix = "PIX";
    public const string CartaoCredito = "Cartão Crédito";
    public const string CartaoDebito = "Cartão Débito";
    public const string Fiado = "Fiado";

    public static readonly string[] Todas =
    {
        Dinheiro, Pix, CartaoCredito, CartaoDebito, Fiado
    };

    /// <summary>Comparação case-insensitive usada nas regras de fiado/PIX/dinheiro.</summary>
    public static bool Eh(string? forma, string esperada) =>
        string.Equals(forma, esperada, StringComparison.OrdinalIgnoreCase);
}
