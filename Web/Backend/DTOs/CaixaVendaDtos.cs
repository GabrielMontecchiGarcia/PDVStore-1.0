namespace PDVStore.DTOs;

/// <summary>
/// Caixa exposto pela API, com o nome do usuário responsável para exibição.
/// </summary>
public class CaixaDto
{
    public int Id { get; set; }
    public int UsuarioCaixaId { get; set; }
    public string? UsuarioNome { get; set; }
    public DateTime Abertura { get; set; }
    public DateTime? Fechamento { get; set; }
    public decimal ValorInicial { get; set; }
    public decimal? ValorFinal { get; set; }
    public decimal? Sangria { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Abertura de caixa com o fundo de troco inicial.</summary>
public class AbrirCaixaRequest
{
    public decimal ValorInicial { get; set; }
}

/// <summary>Sangria (retirada de dinheiro) do caixa aberto.</summary>
public class SangriaRequest
{
    public decimal Valor { get; set; }
}

/// <summary>Item enviado no fechamento/pedido de venda.</summary>
public class ItemVendaRequest
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

/// <summary>
/// Registro de venda. O cliente de uma venda fiada é obrigatório; para dinheiro,
/// o valor recebido é conferido (troco não pode ser negativo), como na tela do PDV.
/// Nenhum dado do operador vem do corpo: o usuário é o autenticado no JWT.
/// </summary>
public class VendaRequest
{
    public List<ItemVendaRequest> Itens { get; set; } = new();
    public decimal Desconto { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public decimal? ValorRecebido { get; set; }
}

/// <summary>Cancelamento de venda; o motivo é registrado em log (não há coluna).</summary>
public class CancelarVendaRequest
{
    public string? Motivo { get; set; }
}

/// <summary>Item da venda no retorno da API.</summary>
public class ItemVendaDto
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Venda completa, com itens, operador, caixa e cliente.</summary>
public class VendaDto
{
    public int Id { get; set; }
    public int UsuarioCaixaId { get; set; }
    public string? UsuarioNome { get; set; }
    public int CaixaId { get; set; }
    public int? ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public DateTime DataVenda { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal Desconto { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public string? PixTxId { get; set; }
    public string? Status { get; set; }
    public List<ItemVendaDto> Itens { get; set; } = new();
}
