namespace PDVStore.Models
{
    public class ItemVenda : IHasId
    {
        public int Id { get; set; }
        public int VendaId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        // Propriedade computada: total do item da venda (preço unitário x quantidade).
        // Derivada a cada acesso, serve para o VendaService e a tela de PDV montarem o
        // ValorTotal e o cupom, sem gravar colunas redundantes no banco de dados.
        public decimal Subtotal => Quantidade * PrecoUnitario;

        // Propriedade computada de conveniência: expõe o nome do produto sem exigir que cada
        // tela navegue por Produto manualmente (usada no cupom/PDV). Retorna null quando
        // a entidade Produto não foi carregada com Include(P => P.Produto).
        public string? NomeProduto => Produto?.Nome;

        public Produto? Produto { get; set; }
        public Venda? Venda { get; set; }
    }
}
