namespace PDVStore.Models
{
    public class ItemCompra : IHasId
    {
        public int Id { get; set; }
        public int CompraId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoCusto { get; set; }
        // Propriedade computada: custo total do item da compra (preço de custo x quantidade).
        // Não é coluna no banco; é derivada a cada acesso a partir de Quantidade e
        // PrecoCusto. É usada pelo CompraService para somar o ValorTotal da Compra e
        // pela tela de compras ao conferir o pedido do fornecedor.
        public decimal Subtotal => Quantidade * PrecoCusto;

        public Compra? Compra { get; set; }
        public Produto? Produto { get; set; }
    }
}