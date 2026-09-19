using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class EstoqueServiceTests : TesteBanco
{
    private async Task<Produto> CriarProdutoAsync(string nome, int estoque = 10, int minimo = 2, string? codigo = null, bool ativo = true)
    {
        var p = new Produto
        {
            Nome = nome,
            CodigoBarras = codigo ?? nome,
            Preco = 10m,
            PrecoCusto = 5m,
            Estoque = estoque,
            EstoqueMinimo = minimo,
            Ativo = ativo
        };
        await Context.Produtos.AddAsync(p);
        await Context.SaveChangesAsync();
        return p;
    }

    [Test]
    public async Task GetAllAsync_FiltraAtivosEOrdenaPorNome()
    {
        var service = new EstoqueService(Context);
        await CriarProdutoAsync("Banana", ativo: false);
        await CriarProdutoAsync("Café");
        await CriarProdutoAsync("Arroz");

        var lista = await service.GetAllAsync();

        Assert.That(lista.Select(p => p.Nome), Is.EqualTo(new[] { "Arroz", "Café" }));
    }

    [Test]
    public async Task GetByIdAsync_RetornaProdutoOuNull()
    {
        var service = new EstoqueService(Context);
        var p = await CriarProdutoAsync("Arroz");

        Assert.That((await service.GetByIdAsync(p.Id))?.Nome, Is.EqualTo("Arroz"));
        Assert.That(await service.GetByIdAsync(999), Is.Null);
    }

    [Test]
    public async Task BuscarAsync_FiltraPorNomeECodigo()
    {
        var service = new EstoqueService(Context);
        await CriarProdutoAsync("Arroz", codigo: "7890001");
        await CriarProdutoAsync("Feijão", codigo: "7890002");
        await CriarProdutoAsync("Arroz Integral", ativo: false);

        Assert.That((await service.BuscarAsync("Arroz")).Select(p => p.Nome), Is.EqualTo(new[] { "Arroz" }));
        Assert.That((await service.BuscarAsync("0002")).Select(p => p.Nome), Is.EqualTo(new[] { "Feijão" }));

        var tudo = await service.BuscarAsync("   ");
        Assert.That(tudo.Select(p => p.Nome), Is.EqualTo(new[] { "Arroz", "Feijão" })); // sem busca retorna os ativos
    }

    [Test]
    public async Task AddAsync_PersisteProduto()
    {
        var service = new EstoqueService(Context);
        var p = new Produto { Nome = "Novo", CodigoBarras = "1", Preco = 9.99m };

        await service.AddAsync(p);

        Assert.That(p.Id, Is.GreaterThan(0));
        Assert.That((await service.GetByIdAsync(p.Id))!.Nome, Is.EqualTo("Novo"));
    }

    [Test]
    public async Task UpdateAsync_PersisteAlteracoes()
    {
        var service = new EstoqueService(Context);
        var p = await CriarProdutoAsync("Arroz", estoque: 5);

        p.Preco = 22.50m;
        p.Estoque = 7;
        await service.UpdateAsync(p);

        var atual = await service.GetByIdAsync(p.Id);
        Assert.That(atual!.Preco, Is.EqualTo(22.50m));
        Assert.That(atual.Estoque, Is.EqualTo(7));
    }

    [Test]
    public async Task DeleteAsync_DesativaProduto()
    {
        var service = new EstoqueService(Context);
        var p = await CriarProdutoAsync("Arroz");

        await service.DeleteAsync(p.Id);

        var atual = await service.GetByIdAsync(p.Id);
        Assert.That(atual!.Ativo, Is.False);
        Assert.That(await service.GetAllAsync(), Is.Empty);
    }

    // ===================== Baixar estoque =====================

    [Test]
    public async Task BaixarEstoqueAsync_QuantidadeInvalida_Rejeita()
    {
        var service = new EstoqueService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.BaixarEstoqueAsync(1, 0));
        Assert.That(ex!.ParamName, Is.EqualTo("quantidade"));
    }

    [Test]
    public async Task BaixarEstoqueAsync_ProdutoInexistente_RetornaFalse()
    {
        var service = new EstoqueService(Context);
        Assert.That(await service.BaixarEstoqueAsync(999, 1), Is.False);
    }

    [Test]
    public async Task BaixarEstoqueAsync_EstoqueInsuficiente_RetornaFalseSemGravar()
    {
        var service = new EstoqueService(Context);
        await CriarProdutoAsync("Café", estoque: 2);

        Assert.That(await service.BaixarEstoqueAsync(1, 5), Is.False);
        Assert.That(await Context.MovimentacoesEstoque.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task BaixarEstoqueAsync_Sucesso_ReduzEstoqueERegistraSaida()
    {
        var service = new EstoqueService(Context);
        var p = await CriarProdutoAsync("Café", estoque: 10);

        Assert.That(await service.BaixarEstoqueAsync(p.Id, 3, "Vencido"), Is.True);

        var atual = await service.GetByIdAsync(p.Id);
        Assert.That(atual!.Estoque, Is.EqualTo(7));

        var mov = await Context.MovimentacoesEstoque.ToListAsync();
        Assert.That(mov.Count, Is.EqualTo(1));
        Assert.That(mov[0].Tipo, Is.EqualTo("Saída"));
        Assert.That(mov[0].Quantidade, Is.EqualTo(3));
        Assert.That(mov[0].Motivo, Is.EqualTo("Vencido"));
        Assert.That(mov[0].ProdutoId, Is.EqualTo(p.Id));
    }

    // ===================== Adicionar estoque =====================

    [Test]
    public async Task AdicionarEstoqueAsync_Sucesso_AdicionaERegistraEntrada()
    {
        var service = new EstoqueService(Context);
        var p = await CriarProdutoAsync("Café", estoque: 5);

        Assert.That(await service.AdicionarEstoqueAsync(p.Id, 4, "Devolução"), Is.True);

        var atual = await service.GetByIdAsync(p.Id);
        Assert.That(atual!.Estoque, Is.EqualTo(9));

        var mov = await Context.MovimentacoesEstoque.ToListAsync();
        Assert.That(mov[0].Tipo, Is.EqualTo("Entrada"));
        Assert.That(mov[0].Quantidade, Is.EqualTo(4));
    }

    [Test]
    public async Task AdicionarEstoqueAsync_ProdutoInexistente_RetornaFalse()
    {
        var service = new EstoqueService(Context);
        Assert.That(await service.AdicionarEstoqueAsync(999, 1), Is.False);
    }

    // ===================== Código de barras / histórico =====================

    [Test]
    public async Task GetByCodigoBarrasAsync_SomenteAtivos()
    {
        var service = new EstoqueService(Context);
        await CriarProdutoAsync("Ativo", codigo: "111", ativo: true);
        await CriarProdutoAsync("Inativo", codigo: "222", ativo: false);

        Assert.That((await service.GetByCodigoBarrasAsync("111"))?.Nome, Is.EqualTo("Ativo"));
        Assert.That(await service.GetByCodigoBarrasAsync("222"), Is.Null);
    }

    [Test]
    public async Task ObterHistoricoMovimentacoesAsync_FiltraPorPeriodoEOrdena()
    {
        var service = new EstoqueService(Context);
        var p = await CriarProdutoAsync("Café", estoque: 0);

        await service.AdicionarEstoqueAsync(p.Id, 5);
        Context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            ProdutoId = p.Id,
            Tipo = "Saída",
            Quantidade = 2,
            DataMovimentacao = DateTime.UtcNow.AddMonths(-2)
        });
        await Context.SaveChangesAsync();

        var todas = await service.ObterHistoricoMovimentacoesAsync();
        Assert.That(todas.Count, Is.EqualTo(2));

        var noMes = await service.ObterHistoricoMovimentacoesAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.That(noMes.Count, Is.EqualTo(1));
        Assert.That(noMes[0].Tipo, Is.EqualTo("Entrada"));

        var antigas = await service.ObterHistoricoMovimentacoesAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow.AddMonths(-1));
        Assert.That(antigas.Count, Is.EqualTo(1));
        Assert.That(antigas[0].Tipo, Is.EqualTo("Saída"));
    }
}