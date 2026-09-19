namespace PDVStore.Models
{
    public class Cliente : IHasId
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? CpfCnpj { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public string? Endereco { get; set; }
        public DateTime CadastradoEm { get; set; } = DateTime.UtcNow;
        public bool Ativo { get; set; } = true;

        public decimal LimiteCredito { get; set; } = 0;
        public decimal SaldoDevedor { get; set; } = 0;
    }
}