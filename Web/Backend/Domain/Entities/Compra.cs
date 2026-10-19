namespace PDVStore.Domain.Entities;

/// <summary>
/// Compra de mercadoria de um fornecedor. A entrada no estoque é feita pelo
/// CompraService dentro de uma transação, preservando a regra original.
/// </summary>
public class Compra : IHasId
{
    public int Id { get; set; }
    public int FornecedorId { get; set; }
    public int UsuarioCaixaId { get; set; }
    public DateTime DataCompra { get; set; } = DateTime.UtcNow;
    public string? NumeroNota { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = StatusCompra.Concluida;

    public Fornecedor? Fornecedor { get; set; }
    public UsuarioCaixa? UsuarioCaixa { get; set; }
    public ICollection<ItemCompra> Itens { get; set; } = new List<ItemCompra>();
}

/// <summary>Status possíveis de uma compra (mesmas strings do banco original).</summary>
public static class StatusCompra
{
    public const string Concluida = "Concluida";
    public const string Cancelada = "Cancelada";
}
