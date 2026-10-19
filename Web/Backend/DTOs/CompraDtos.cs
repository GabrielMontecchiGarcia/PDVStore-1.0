namespace PDVStore.DTOs;

/// <summary>Item enviado no registro de compra.</summary>
public class ItemCompraRequest
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoCusto { get; set; }
}

/// <summary>
/// Registro de compra de mercadoria. O fornecedor é obrigatório (regra da tela);
/// o usuário responsável vem do JWT. O número da nota é opcional em texto,
/// conforme domínio e manual de operação.
/// </summary>
public class CompraRequest
{
    public int FornecedorId { get; set; }
    public string? NumeroNota { get; set; }
    public List<ItemCompraRequest> Itens { get; set; } = new();
}

/// <summary>Item da compra no retorno da API.</summary>
public class ItemCompraDto
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoCusto { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Compra completa, com fornecedor, operador e itens.</summary>
public class CompraDto
{
    public int Id { get; set; }
    public int FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public int UsuarioCaixaId { get; set; }
    public string? UsuarioNome { get; set; }
    public DateTime DataCompra { get; set; }
    public string? NumeroNota { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ItemCompraDto> Itens { get; set; } = new();
}
