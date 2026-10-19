namespace PDVStore.DTOs;

/// <summary>
/// Item do relatório de produtos mais vendidos. StatusMinimo é "Baixo" quando
/// Estoque &lt;= EstoqueMinimo, mesma regra do relatório original.
/// </summary>
public class ItemRelatorioDto
{
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public int TotalVendido { get; set; }
    public decimal ValorTotalVendido { get; set; }
    public int EstoqueAtual { get; set; }
    public int EstoqueMinimo { get; set; }
    public string StatusMinimo { get; set; } = string.Empty;
}

/// <summary>Totais de um meio de pagamento no período.</summary>
public class PagamentoPorFormaDto
{
    public string Forma { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
}

/// <summary>Série diária de movimentações (entradas x saídas).</summary>
public class MovimentacaoPorDiaDto
{
    public DateTime Dia { get; set; }
    public int Entradas { get; set; }
    public int Saidas { get; set; }
}

/// <summary>Série diária de vendas (R$).</summary>
public class VendaPorDiaDto
{
    public DateTime Dia { get; set; }
    public decimal TotalVendas { get; set; }
}

/// <summary>
/// Consolidação do dashboard: mesmos indicadores do DashboardViewModel original.
/// Todas as datas de agrupamento usam o fuso configurado em App:TimeZone.
/// </summary>
public class DashboardDto
{
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public decimal TotalPeriodo { get; set; }
    public int QuantidadeVendas { get; set; }
    public List<PagamentoPorFormaDto> PagamentosPorForma { get; set; } = new();
    public List<ItemRelatorioDto> ItensMaisVendidos { get; set; } = new();
    public List<ProdutoDto> AlertasEstoque { get; set; } = new();
    public List<MovimentacaoPorDiaDto> MovimentacoesPorDia { get; set; } = new();
    public List<VendaPorDiaDto> VendasPorDia { get; set; } = new();
}
