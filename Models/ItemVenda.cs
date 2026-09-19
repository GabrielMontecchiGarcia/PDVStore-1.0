namespace PDVStore.Models
{
    public class ItemVenda : IHasId
    {
        public int Id { get; set; }
        public int VendaId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Subtotal => Quantidade * PrecoUnitario;

        public string? NomeProduto => Produto?.Nome;

        public Produto? Produto { get; set; }
        public Venda? Venda { get; set; }
    }
}
