namespace PDVStore.Domain.Entities;

/// <summary>
/// Contrato comum das entidades que possuem chave primária inteira.
/// Mantido do projeto Windows (Models/IHasId.cs) para preservar a mesma nomenclatura.
/// </summary>
public interface IHasId
{
    int Id { get; set; }
}
