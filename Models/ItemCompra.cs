namespace PDVStore.Models
{
    public class ItemCompra : IHasId
    {
        public int Id { get; set; }
        public int CompraId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoCusto { get; set; }
        public decimal Subtotal => Quantidade * PrecoCusto;

        public Compra? Compra { get; set; }
        public Produto? Produto { get; set; }
    }
}