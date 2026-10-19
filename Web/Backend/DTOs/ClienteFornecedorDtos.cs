namespace PDVStore.DTOs;

/// <summary>
/// Cliente com dados de fiado. O saldo devedor só é alterado por venda fiada,
/// recebimento ou cancelamento de venda, nunca por edição direta do cadastro.
/// </summary>
public class ClienteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public DateTime CadastradoEm { get; set; }
    public bool Ativo { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal SaldoDevedor { get; set; }
}

/// <summary>
/// Cadastro/edição de cliente. As validações da tela original são aplicadas no
/// servidor: CPF/CNPJ válido, telefone com DDD (10+), e-mail, endereço e limite ≥ 10.
/// </summary>
public class ClienteRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    public decimal LimiteCredito { get; set; }
}

/// <summary>Recebimento (baixa) de parte do saldo devedor do cliente.</summary>
public class ReceberFiadoRequest
{
    public decimal Valor { get; set; }
}

/// <summary>Fornecedor exposto pela API.</summary>
public class FornecedorDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public bool Ativo { get; set; }
}

/// <summary>Cadastro/edição de fornecedor com as validações da tela original.</summary>
public class FornecedorRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
}
