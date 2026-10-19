using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Tests.Infrastructure;

namespace PDVStore.Tests;

/// <summary>
/// Testes do dashboard/relatórios: mesmos indicadores do DashboardViewModel original.
/// </summary>
public class RelatorioServiceTests
{
    [Fact]
    public async Task Dashboard_DeveConsolidarVendasPagamentosRankingEAlertaEstoque()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);

        var produto = await cenario.CriarProdutoAsync(nome: "Café", preco: 10m, estoque: 5, minimo: 2);
        var produtoBaixo = await cenario.CriarProdutoAsync(nome: "Açúcar", preco: 5m, estoque: 2, minimo: 3);

        await cenario.Vendas.RegistrarVendaAsync(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Pix,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 2, PrecoUnitario = 10m },
                new() { ProdutoId = produtoBaixo.Id, Quantidade = 1, PrecoUnitario = 5m }
            }
        }, usuarioId);

        await cenario.Vendas.RegistrarVendaAsync(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Dinheiro,
            ValorRecebido = 20m,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = 10m }
            }
        }, usuarioId);

        var inicio = DateTime.UtcNow.AddMinutes(-5);
        var fim = DateTime.UtcNow.AddMinutes(5);

        var dashboard = await cenario.Relatorios.ObterDashboardAsync(inicio, fim);

        Assert.Equal(35m, dashboard.TotalPeriodo);
        Assert.Equal(2, dashboard.QuantidadeVendas);

        var pix = Assert.Single(dashboard.PagamentosPorForma, p => p.Forma == FormasPagamento.Pix);
        Assert.Equal(25m, pix.Total);
        Assert.Equal(1, pix.Quantidade);

        var dinheiro = Assert.Single(dashboard.PagamentosPorForma, p => p.Forma == FormasPagamento.Dinheiro);
        Assert.Equal(10m, dinheiro.Total);

        var maisVendido = dashboard.ItensMaisVendidos.First();
        Assert.Equal("Café", maisVendido.NomeProduto);
        Assert.Equal(3, maisVendido.TotalVendido);

        Assert.Contains(dashboard.AlertasEstoque, p => p.Nome == "Açúcar");
        Assert.NotEmpty(dashboard.VendasPorDia);
        Assert.NotEmpty(dashboard.MovimentacoesPorDia);
        Assert.Equal(4, dashboard.MovimentacoesPorDia.Sum(m => m.Saidas));
    }

    [Fact]
    public async Task EstoqueMinimo_DeveTrazerSomenteAtivosAbaixoOuIgualAoMinimo()
    {
        await using var cenario = CenarioTeste.Criar();
        var baixo = await cenario.CriarProdutoAsync(nome: "Baixo", estoque: 1, minimo: 2);
        await cenario.CriarProdutoAsync(nome: "Ok", estoque: 10, minimo: 2);

        var inativo = await cenario.CriarProdutoAsync(nome: "Inativo", estoque: 0, minimo: 5);
        await cenario.Produtos.DesativarAsync(inativo.Id);

        var produtos = await cenario.Relatorios.EstoqueMinimoAsync();

        var item = Assert.Single(produtos);
        Assert.Equal(baixo.Id, item.Id);
    }

    [Fact]
    public async Task TotalVendas_DeveContarTodasAsVendasRegistradas()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 10m, estoque: 10);

        await cenario.Vendas.RegistrarVendaAsync(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Pix,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = 10m }
            }
        }, usuarioId);

        Assert.Equal(1, await cenario.Relatorios.TotalVendasAsync());
    }
}
