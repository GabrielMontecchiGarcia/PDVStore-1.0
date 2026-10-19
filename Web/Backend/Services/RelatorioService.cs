using PDVStore.Application.Interfaces;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de relatórios e dashboard. Replica o RelatorioService e o
/// DashboardViewModel do projeto Windows:
/// - Ranking top 10 de itens vendidos no período;
/// - Alertas de estoque mínimo (Estoque &lt;= EstoqueMinimo, apenas ativos);
/// - Faturamento e quantidade de vendas concluídas;
/// - Distribuição por forma de pagamento;
/// - Movimentações por dia (entradas x saídas) e vendas por dia.
/// As datas são armazenadas em UTC e agrupadas no fuso configurado (App:TimeZone),
/// substituindo o ToLocalTime() dependente da máquina do operador.
/// </summary>
public class RelatorioService : IRelatorioService
{
    private readonly IVendaRepository _vendas;
    private readonly IMovimentacaoEstoqueRepository _movimentacoes;
    private readonly IRelatorioRepository _relatorios;
    private readonly TimeZoneInfo _timeZone;

    public RelatorioService(
        IVendaRepository vendas,
        IMovimentacaoEstoqueRepository movimentacoes,
        IRelatorioRepository relatorios,
        TimeZoneInfo timeZone)
    {
        _vendas = vendas;
        _movimentacoes = movimentacoes;
        _relatorios = relatorios;
        _timeZone = timeZone;
    }

    public Task<List<ItemRelatorioDto>> ItensMaisVendidosAsync(DateTime inicio, DateTime fim, int topN, CancellationToken ct = default) =>
        _relatorios.ItensMaisVendidosAsync(inicio, fim, topN, ct);

    public Task<List<Produto>> EstoqueMinimoAsync(CancellationToken ct = default) =>
        _relatorios.EstoqueMinimoAsync(ct);

    /// <summary>Monta todos os indicadores do dashboard para o período informado.</summary>
    public async Task<DashboardDto> ObterDashboardAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var dashboard = new DashboardDto { Inicio = inicio, Fim = fim };

        dashboard.TotalPeriodo = await _vendas.CalcularTotalAsync(inicio, fim, null, ct);
        dashboard.QuantidadeVendas = await _vendas.ContarAsync(inicio, fim, ct);
        dashboard.ItensMaisVendidos = await _relatorios.ItensMaisVendidosAsync(inicio, fim, 10, ct);

        var alertas = await _relatorios.EstoqueMinimoAsync(ct);
        dashboard.AlertasEstoque = alertas.Select(p => new ProdutoDto
        {
            Id = p.Id,
            Nome = p.Nome,
            CodigoBarras = p.CodigoBarras,
            Preco = p.Preco,
            PrecoCusto = p.PrecoCusto,
            Estoque = p.Estoque,
            EstoqueAtual = p.EstoqueAtual,
            EstoqueMinimo = p.EstoqueMinimo,
            Categoria = p.Categoria,
            Descricao = p.Descricao,
            Ativo = p.Ativo
        }).ToList();

        // Distribuição por forma de pagamento (agrupamento em memória, como o original).
        var vendas = await _vendas.ListarConcluidasAsync(inicio, fim, ct);

        dashboard.PagamentosPorForma = vendas
            .GroupBy(v => v.FormaPagamento)
            .Select(g => new PagamentoPorFormaDto
            {
                Forma = g.Key,
                Quantidade = g.Count(),
                Total = g.Sum(v => v.ValorTotal)
            })
            .OrderBy(p => p.Forma)
            .ToList();

        dashboard.VendasPorDia = vendas
            .GroupBy(v => ParaDataLocal(v.DataVenda))
            .OrderBy(g => g.Key)
            .Select(g => new VendaPorDiaDto { Dia = g.Key, TotalVendas = g.Sum(v => v.ValorTotal) })
            .ToList();

        // Movimentações de estoque por dia (entradas x saídas).
        var movimentacoes = await _movimentacoes.ListarAsync(inicio, fim, ct);

        dashboard.MovimentacoesPorDia = movimentacoes
            .GroupBy(m => ParaDataLocal(m.DataMovimentacao))
            .OrderBy(g => g.Key)
            .Select(g => new MovimentacaoPorDiaDto
            {
                Dia = g.Key,
                Entradas = g.Where(m => m.Tipo == TiposMovimentacao.Entrada).Sum(m => m.Quantidade),
                Saidas = g.Where(m => m.Tipo == TiposMovimentacao.Saida).Sum(m => m.Quantidade)
            })
            .ToList();

        return dashboard;
    }

    public Task<int> TotalVendasAsync(CancellationToken ct = default) =>
        _vendas.ContarTodasAsync(ct);

    /// <summary>Converte a data UTC para a data local no fuso configurado.</summary>
    private DateTime ParaDataLocal(DateTime dataUtc)
    {
        var utc = DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone).Date;
    }
}
