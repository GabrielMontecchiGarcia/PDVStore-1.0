namespace PDVStore.Domain.Entities;

/// <summary>
/// Item de uma compra. Subtotal é computado (quantidade × custo) e não é coluna,
/// exatamente como no projeto Windows.
/// </summary>
public class ItemCompra : IHasId
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoCusto { get; set; }

    /// <summary>Custo total do item; derivado a cada acesso.</summary>
    public decimal Subtotal => Quantidade * PrecoCusto;

    public Compra? Compra { get; set; }
    public Produto? Produto { get; set; }
}
