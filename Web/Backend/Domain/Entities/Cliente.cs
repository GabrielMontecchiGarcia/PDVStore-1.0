namespace PDVStore.Domain.Entities;

/// <summary>
/// Cliente do PDV, incluindo controle de fiado (caderneta).
/// Regras preservadas:
/// - LimiteCredito = 0 significa "sem limite configurado" (não bloqueia);
/// - SaldoDevedor aumenta em venda fiada e diminui em recebimento/cancelamento,
///   nunca ficando negativo (proteção Math.Max(0, ...));
/// - Cliente inativo não pode receber venda fiada;
/// - Exclusão é lógica (Ativo = false).
/// </summary>
public class Cliente : IHasId
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public DateTime CadastradoEm { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;

    public decimal LimiteCredito { get; set; }
    public decimal SaldoDevedor { get; set; }
}
