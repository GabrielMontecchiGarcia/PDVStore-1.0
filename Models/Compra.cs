namespace PDVStore.Models
{
    public class Compra : IHasId
    {
        public int Id { get; set; }
        public int FornecedorId { get; set; }
        public int UsuarioCaixaId { get; set; }
        public DateTime DataCompra { get; set; } = DateTime.UtcNow;
        public string? NumeroNota { get; set; }
        public decimal ValorTotal { get; set; }
        public string Status { get; set; } = "Concluida"; // Concluida | Cancelada

        public Fornecedor? Fornecedor { get; set; }
        public UsuarioCaixa? UsuarioCaixa { get; set; }
        public ICollection<ItemCompra> Itens { get; set; } = new List<ItemCompra>();
    }
}