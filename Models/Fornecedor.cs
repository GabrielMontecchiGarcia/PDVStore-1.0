namespace PDVStore.Models
{
    public class Fornecedor : IHasId
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? Cnpj { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public bool Ativo { get; set; } = true;

        public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    }
}