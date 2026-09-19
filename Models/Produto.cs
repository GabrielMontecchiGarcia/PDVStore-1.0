using System.ComponentModel.DataAnnotations.Schema;

namespace PDVStore.Models
{
    public class Produto : IHasId
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? CodigoBarras { get; set; }
        public decimal Preco { get; set; }
        public decimal PrecoCusto { get; set; }

        public int Estoque { get; set; }

        // Nível mínimo de estoque para disparar alerta de reposição
        public int EstoqueMinimo { get; set; } = 0;

        // Convenience property usado pelas telas; não é mapeado como coluna
        [NotMapped]
        public int EstoqueAtual
        {
            get => Estoque;
            set => Estoque = value;
        }

        public string? Categoria { get; set; }
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
