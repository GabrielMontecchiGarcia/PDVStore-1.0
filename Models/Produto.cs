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
        // Propriedade computada de conveniência: apenas espelha o campo Estoque, mas expõe a
        // nomenclatura "EstoqueAtual" que várias telas esperam. Marcada com [NotMapped]
        // para o EF Core não criar uma coluna duplicada no banco de dados.
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
