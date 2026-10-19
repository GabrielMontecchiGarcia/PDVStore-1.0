namespace PDVStore.Domain.Entities;

/// <summary>
/// Fornecedor usado nas compras de mercadoria. Soft delete por Ativo,
/// preservando o histórico de compras (mesma regra do projeto Windows).
/// </summary>
public class Fornecedor : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
