namespace PDVStore.Models
{
    public class Caixa : IHasId
    {
        public int Id { get; set; }
        public int UsuarioCaixaId { get; set; }
        public DateTime Abertura { get; set; } = DateTime.UtcNow;
        public DateTime? Fechamento { get; set; }
        public decimal ValorInicial { get; set; } = 0;
        public decimal? ValorFinal { get; set; }
        public decimal? Sangria { get; set; }
        public string Status { get; set; } = "Aberto"; // Aberto | Fechado

        public UsuarioCaixa? UsuarioCaixa { get; set; }
        public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
    }
}