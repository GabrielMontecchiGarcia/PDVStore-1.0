using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class RelatorioServiceTests : TesteBanco
{
    private async Task<(RelatorioService Service, Produto Arroz, Produto Cuscuz)> SetupAsync()
    {
        var arroz = new Produto { Nome = "Arroz", CodigoBarras = "1", Preco = 20m, Estoque = 2, EstoqueMinimo = 5, Ativo = true };
        var cuscuz = new Produto { Nome = "Cuscuz", CodigoBarras = "2", Preco = 10m, Estoque = 50, EstoqueMinimo = 3, Ativo = true };
        var tempero = new Produto { Nome = "Tempero", CodigoBarras = "3", Preco = 5m, Estoque = 10, EstoqueMinimo = 2, Ativo = true };
        await Context.Produtos.AddRangeAsync(arroz, cuscuz, tempero);
        await Context.SaveChangesAsync();

        await RegistrarVendaAsync(arroz.Id, "Concluida", qtd1: 3, qtd2: 1);
        await RegistrarVendaAsync(arroz.Id, "Concluida", qtd1: 2, qtd2: 0);
        await RegistrarVendaAsync(cuscuz.Id, "Cancelada", qtd1: 1, qtd2: 0);

        return (new RelatorioService(Context), arroz, cuscuz);
    }

    private async Task RegistrarVendaAsync(int produtoMenorId, string status, int qtd1, int qtd2 = 0)
    {
        var venda = new Venda
        {
            UsuarioCaixaId = 1,
            DataVenda = DateTime.UtcNow,
            FormaPagamento = "Dinheiro",
            Status = status
        };

        var itens = new List<ItemVenda> { new() { ProdutoId = produtoMenorId, Quantidade = qtd1, PrecoUnitario = 20m } };
        if (qtd2 > 0)
        {
            var outro = await Context.Produtos.Where(p => p.Id != produtoMenorId && p.Nome != "Cuscuz").Select(p => p.Id).FirstAsync();
            itens.Add(new ItemVenda { ProdutoId = outro, Quantidade = qtd2, PrecoUnitario = 10m });
        }

        venda.Itens = itens;
        Context.Vendas.Add(venda);
        await Context.SaveChangesAsync();
    }

    [Test]
    public async Task GerarRelatorioItensMaisVendidos_ApenasConcluidasEEmPeriodo()
    {
        var (service, _, _) = await SetupAsync();

        var relatorio = service.GerarRelatorioItensMaisVendidos(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), topN: 10);

        var arroz = relatorio.Single(r => r.NomeProduto == "Arroz");
        Assert.That(arroz.TotalVendido, Is.EqualTo(5)); // 3 + 2 (cancelada não conta)
        Assert.That(arroz.ValorTotalVendido, Is.EqualTo(100m));
        Assert.That(arroz.CodigoBarras, Is.EqualTo("1"));
        Assert.That(arroz.StatusMinimo, Is.EqualTo("Baixo")); // estoque 2 <= mínimo 5

        var cuscuz = relatorio.SingleOrDefault(r => r.NomeProduto == "Cuscuz");
        Assert.That(cuscuz, Is.Null); // todas as vendas canceladas

        Assert.That(relatorio.First().NomeProduto, Is.EqualTo("Arroz")); // ordem por quantidade
    }

    [Test]
    public async Task GerarRelatorioItensMaisVendidos_TopN_RespeitaLimite()
    {
        var (service, _, _) = await SetupAsync();
        var relatorio = service.GerarRelatorioItensMaisVendidos(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), topN: 1);
        Assert.That(relatorio.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GerarRelatorioEstoqueMinimo_SomenteProdutosEmAlerta()
    {
        var (service, arroz, _) = await SetupAsync();

        var alerta = service.GerarRelatorioEstoqueMinimo();

        Assert.That(alerta.Select(p => p.Id), Is.EqualTo(new[] { arroz.Id }));
        Assert.That(alerta.Single().Estoque, Is.EqualTo(2));
    }

    [Test]
    public async Task GetTotalVendasAsync_ContaTodas()
    {
        var (service, _, _) = await SetupAsync();
        Assert.That(await service.GetTotalVendasAsync(), Is.EqualTo(3));
    }

    [Test]
    public async Task ExportarPDF_CriaArquivoValido()
    {
        var (service, _, _) = await SetupAsync();
        var caminho = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"rel_{Guid.NewGuid():N}.pdf");

        var dados = service.GerarRelatorioItensMaisVendidos(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        service.ExportarPDF(dados, caminho);

        Assert.That(File.Exists(caminho), Is.True);
        Assert.That(new FileInfo(caminho).Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task ExportarPdf_ListaVazia_NaoFalha()
    {
        var (service, _, _) = await SetupAsync();
        var caminho = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"rel_vazio_{Guid.NewGuid():N}.pdf");

        service.ExportarPDF(new List<ItemRelatorio>(), caminho);

        Assert.That(File.Exists(caminho), Is.True);
    }

    [Test]
    public async Task ExportarExcel_CriaArquivoValido()
    {
        var (service, _, _) = await SetupAsync();
        var caminho = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"xlsx_{Guid.NewGuid():N}.xlsx");

        var dados = service.GerarRelatorioEstoqueMinimo();
        service.ExportarExcel(dados, caminho);

        Assert.That(File.Exists(caminho), Is.True);
        Assert.That(new FileInfo(caminho).Length, Is.GreaterThan(0));
    }
}