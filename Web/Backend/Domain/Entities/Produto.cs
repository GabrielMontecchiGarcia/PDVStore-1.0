using System.ComponentModel.DataAnnotations.Schema;

namespace PDVStore.Domain.Entities;

/// <summary>
/// Produto do catálogo. Replica a entidade Produto do sistema Windows.
/// Regras preservadas: preços e estoques não negativos, soft delete por Ativo,
/// código de barras obrigatório na tela, estoque mínimo para alertas.
/// </summary>
public class Produto : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal Preco { get; set; }
    public decimal PrecoCusto { get; set; }

    /// <summary>Saldo atual em estoque (coluna Estoque no banco).</summary>
    public int Estoque { get; set; }

    /// <summary>Nível mínimo para disparar alerta de reposição.</summary>
    public int EstoqueMinimo { get; set; }

    /// <summary>
    /// Propriedade computada de conveniência usada pelas telas (não é coluna).
    /// Mantida com [NotMapped] exatamente como no projeto original, para evitar
    /// coluna duplicada no SQL Server.
    /// </summary>
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
