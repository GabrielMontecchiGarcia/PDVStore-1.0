namespace PDVStore.Domain.Entities;

/// <summary>
/// Caixa (turno de operação). Regras preservadas:
/// - Só pode existir um caixa "Aberto" por vez;
/// - Fechamento = ValorInicial + vendas concluídas - sangrias;
/// - Sangria acumula retiradas e exige valor maior que zero.
/// </summary>
public class Caixa : IHasId
{
    public int Id { get; set; }
    public int UsuarioCaixaId { get; set; }
    public DateTime Abertura { get; set; } = DateTime.UtcNow;
    public DateTime? Fechamento { get; set; }
    public decimal ValorInicial { get; set; }
    public decimal? ValorFinal { get; set; }
    public decimal? Sangria { get; set; }
    public string Status { get; set; } = StatusCaixa.Aberto;

    public UsuarioCaixa? UsuarioCaixa { get; set; }
    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}

/// <summary>Valores de Status do caixa (mesmas strings do banco original).</summary>
public static class StatusCaixa
{
    public const string Aberto = "Aberto";
    public const string Fechado = "Fechado";
}
