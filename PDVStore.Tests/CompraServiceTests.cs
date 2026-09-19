using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class CompraServiceTests : TesteBanco
{
    private async Task<(CompraService Service, Produto Produto, Fornecedor Fornecedor)> SetupAsync(
        int estoque = 10, int qtd = 5, decimal custo = 2.50m)
    {
        var fornecedor = new Fornecedor { Nome = "Distribuidora" };
        await Context.Fornecedores.AddAsync(fornecedor);

        var produto = new Produto { Nome = "Arroz", CodigoBarras = "789", Preco = 5m, PrecoCusto = 2m, Estoque = estoque, Ativo = true };
        await Context.Produtos.AddAsync(produto);
        await Context.SaveChangesAsync();

        return (new CompraService(Context), produto, fornecedor);
    }

    private Compra CriarCompra(int produtoId, int qtd = 5, decimal custo = 2.50m)
    {
        return new Compra
        {
            FornecedorId = 1,
            UsuarioCaixaId = 1,
            NumeroNota = "N-001",
            Itens = new List<ItemCompra>
            {
                new() { ProdutoId = produtoId, Quantidade = qtd, PrecoCusto = custo }
            }
        };
    }

    [Test]
    public async Task RegistrarCompraAsync_SemItens_Rejeita()
    {
        var (service, _, _) = await SetupAsync();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(new Compra { Itens = new List<ItemCompra>() }));
        Assert.That(ex!.Message, Does.Contain("pelo menos um item"));
    }

    [Test]
    public async Task RegistrarCompraAsync_QuantidadeInvalida_Rejeita()
    {
        var (service, p, _) = await SetupAsync();
        var compra = CriarCompra(p.Id, qtd: 0);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(compra));
        Assert.That(ex!.Message, Does.Contain("maior que zero"));
    }

    [Test]
    public async Task RegistrarCompraAsync_CustoNegativo_Rejeita()
    {
        var (service, p, _) = await SetupAsync();
        var compra = CriarCompra(p.Id, custo: -1m);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(compra));
        Assert.That(ex!.Message, Does.Contain("não pode ser negativo"));
    }

    [Test]
    public async Task RegistrarCompraAsync_ProdutoInexistente_Rejeita()
    {
        var (service, _, _) = await SetupAsync();
        var compra = CriarCompra(produtoId: 999);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(compra));
        Assert.That(ex!.Message, Does.Contain("não encontrado"));
    }

    [Test]
    public async Task RegistrarCompraAsync_Sucesso_EntraEstoqueEGravaMovimentacao()
    {
        var (service, p, _) = await SetupAsync(estoque: 3);

        var compra = CriarCompra(p.Id, qtd: 4, custo: 2.50m);
        var registrada = await service.RegistrarCompraAsync(compra);

        Assert.That(registrada.Id, Is.GreaterThan(0));
        Assert.That(registrada.Status, Is.EqualTo("Concluida"));
        Assert.That(registrada.ValorTotal, Is.EqualTo(10m)); // 4 * 2,50
        Assert.That(registrada.DataCompra, Is.Not.EqualTo(default));

        var produto = await Context.Produtos.FindAsync(p.Id);
        Assert.That(produto!.Estoque, Is.EqualTo(7)); // 3 + 4
        Assert.That(produto.PrecoCusto, Is.EqualTo(2.50m));

        var mov = await Context.MovimentacoesEstoque.ToListAsync();
        Assert.That(mov.Count, Is.EqualTo(1));
        Assert.That(mov[0].Tipo, Is.EqualTo("Entrada"));
        Assert.That(mov[0].Quantidade, Is.EqualTo(4));
        Assert.That(mov[0].Motivo, Does.Contain("Compra #"));
    }

    [Test]
    public async Task RegistrarCompraAsync_CustoZero_NaocalculaPrecoCusto()
    {
        var (service, p, _) = await SetupAsync(estoque: 0, custo: 0m);
        var compra = CriarCompra(p.Id, qtd: 2, custo: 0m);

        await service.RegistrarCompraAsync(compra);

        var produto = await Context.Produtos.FindAsync(p.Id);
        Assert.That(produto!.Estoque, Is.EqualTo(2));
        Assert.That(produto.PrecoCusto, Is.EqualTo(2m)); // inalterado
    }

    [Test]
    public async Task ObterPorIdAsync_ComInclude_RetornaDadosRelacionados()
    {
        var (service, p, f) = await SetupAsync();
        var registrada = await service.RegistrarCompraAsync(CriarCompra(p.Id));

        var obtida = await service.ObterPorIdAsync(registrada.Id);

        Assert.That(obtida, Is.Not.Null);
        Assert.That(obtida!.Fornecedor?.Nome, Is.EqualTo(f.Nome));
        Assert.That(obtida.Itens.Count, Is.EqualTo(1));
        Assert.That(obtida.Itens.Single().Produto?.Id, Is.EqualTo(p.Id));

        Assert.That(await service.ObterPorIdAsync(999), Is.Null);
    }

    [Test]
    public async Task ListarAsync_FiltraPorPeriodoEOrdenaDesc()
    {
        var (service, p, _) = await SetupAsync();
        await service.RegistrarCompraAsync(CriarCompra(p.Id));

        Context.Compras.Add(new Compra
        {
            FornecedorId = 1,
            UsuarioCaixaId = 1,
            DataCompra = DateTime.UtcNow.AddMonths(-2),
            Itens = new List<ItemCompra> { new() { ProdutoId = p.Id, Quantidade = 1, PrecoCusto = 1m } }
        });
        await Context.SaveChangesAsync();

        var todas = await service.ListarAsync();
        Assert.That(todas.Count, Is.EqualTo(2));
        Assert.That(todas[0].DataCompra >= todas[1].DataCompra, Is.True);

        var recentes = await service.ListarAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.That(recentes.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task CancelarCompraAsync_EstornaEstoqueSoUmaVez()
    {
        var (service, p, _) = await SetupAsync(estoque: 2);
        var compra = await service.RegistrarCompraAsync(CriarCompra(p.Id, qtd: 5));

        Assert.That((await Context.Produtos.FindAsync(p.Id))!.Estoque, Is.EqualTo(7));

        Assert.That(await service.CancelarCompraAsync(compra.Id), Is.True);
        Assert.That((await Context.Produtos.FindAsync(p.Id))!.Estoque, Is.EqualTo(2));
        Assert.That((await service.ObterPorIdAsync(compra.Id))!.Status, Is.EqualTo("Cancelada"));

        Assert.That(await service.CancelarCompraAsync(compra.Id), Is.False); // já cancelada
        Assert.That(await service.CancelarCompraAsync(999), Is.False);
    }
}