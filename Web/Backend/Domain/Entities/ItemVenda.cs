namespace PDVStore.Domain.Entities;

/// <summary>
/// Item de uma venda. Subtotal é propriedade computada (quantidade × preço),
/// não mapeada como coluna — mesma decisão do sistema original.
/// </summary>
public class ItemVenda : IHasId
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }

    /// <summary>Total do item; calculado a cada acesso, sem coluna redundante.</summary>
    public decimal Subtotal => Quantidade * PrecoUnitario;

    /// <summary>Nome do produto quando a navegação foi carregada (cupom/PDV).</summary>
    public string? NomeProduto => Produto?.Nome;

    public Produto? Produto { get; set; }
    public Venda? Venda { get; set; }
}
