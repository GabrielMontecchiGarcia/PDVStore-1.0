namespace PDVStore.Domain.Entities;

/// <summary>
/// Histórico de entrada/saída de estoque. Registra o usuário responsável
/// (quando houver) e o vínculo com a venda que gerou a saída automática.
/// </summary>
public class MovimentacaoEstoque : IHasId
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Tipo { get; set; } = TiposMovimentacao.Entrada;
    public int Quantidade { get; set; }
    public decimal? PrecoUnitario { get; set; }
    public DateTime DataMovimentacao { get; set; } = DateTime.UtcNow;
    public int? UsuarioId { get; set; }
    public string? Motivo { get; set; }
    public int? ReferenciaVendaId { get; set; }

    public Produto? Produto { get; set; }
    public UsuarioCaixa? Usuario { get; set; }
}

/// <summary>
/// Tipos de movimentação. Atenção ao acento em "Saída": é o valor gravado
/// pelo sistema original e usado nos relatórios do dashboard.
/// </summary>
public static class TiposMovimentacao
{
    public const string Entrada = "Entrada";
    public const string Saida = "Saída";
}
