namespace PDVStore.DTOs;

/// <summary>
/// Produto exposto pela API. EstoqueAtual espelha Estoque (propriedade
/// computada [NotMapped] do domínio), mantendo a nomenclatura das telas.
/// </summary>
public class ProdutoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal Preco { get; set; }
    public decimal PrecoCusto { get; set; }
    public int Estoque { get; set; }
    public int EstoqueAtual { get; set; }
    public int EstoqueMinimo { get; set; }
    public string? Categoria { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}

/// <summary>Cadastro/edição de produto com as validações da tela original.</summary>
public class ProdutoRequest
{
    public string? CodigoBarras { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public decimal PrecoCusto { get; set; }
    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; }
    public string? Categoria { get; set; }
    public string? Descricao { get; set; }
}

/// <summary>Ajuste manual de estoque (entrada ou saída).</summary>
public class AjusteEstoqueRequest
{
    public int Quantidade { get; set; }
    public string? Motivo { get; set; }
}

/// <summary>
/// Movimentação de estoque para histórico/relatórios.
/// Tipo mantém "Entrada"/"Saída" exatamente como no banco.
/// </summary>
public class MovimentacaoEstoqueDto
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal? PrecoUnitario { get; set; }
    public DateTime DataMovimentacao { get; set; }
    public int? UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public string? Motivo { get; set; }
    public int? ReferenciaVendaId { get; set; }
}
