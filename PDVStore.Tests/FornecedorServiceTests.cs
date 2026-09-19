using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class FornecedorServiceTests : TesteBanco
{
    private async Task<Fornecedor> CriarFornecedorAsync(string nome = "Distribuidora", bool ativo = true)
    {
        var f = new Fornecedor { Nome = nome, Cnpj = "11.222.333/0001-81", Email = nome + "@forn.com", Ativo = ativo };
        await Context.Fornecedores.AddAsync(f);
        await Context.SaveChangesAsync();
        return f;
    }

    [Test]
    public async Task ListarAsync_FiltraAtivosEFiltro()
    {
        var service = new FornecedorService(Context);
        await CriarFornecedorAsync("Açaí");
        await CriarFornecedorAsync("Bebidas", ativo: false);
        await CriarFornecedorAsync("Carnes");

        Assert.That((await service.ListarAsync()).Select(f => f.Nome), Is.EqualTo(new[] { "Açaí", "Carnes" }));
        Assert.That((await service.ListarAsync(true, "Aça")).Select(f => f.Nome), Is.EqualTo(new[] { "Açaí" }));
        Assert.That((await service.ListarAsync(false, "ebidas")).Select(f => f.Nome), Is.EqualTo(new[] { "Bebidas" }));
    }

    [Test]
    public async Task ObterPorIdAsync_RetornaOuNull()
    {
        var service = new FornecedorService(Context);
        var f = await CriarFornecedorAsync();

        Assert.That((await service.ObterPorIdAsync(f.Id))?.Nome, Is.EqualTo("Distribuidora"));
        Assert.That(await service.ObterPorIdAsync(999), Is.Null);
    }

    [Test]
    public async Task SalvarAsync_NomeObrigatorio_Rejeita()
    {
        var service = new FornecedorService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.SalvarAsync(new Fornecedor { Nome = "" }));
        Assert.That(ex!.Message, Does.Contain("obrigatório"));
    }

    [Test]
    public async Task SalvarAsync_NovoEUpdate()
    {
        var service = new FornecedorService(Context);

        var novo = await service.SalvarAsync(new Fornecedor { Nome = "Laticínios" });
        Assert.That(novo.Id, Is.GreaterThan(0));

        novo.Email = "novo@forn.com";
        await service.SalvarAsync(novo);

        var atual = await Context.Fornecedores.FindAsync(novo.Id);
        Assert.That(atual!.Email, Is.EqualTo("novo@forn.com"));
        Assert.That(await Context.Fornecedores.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task AtualizarStatusAsync()
    {
        var service = new FornecedorService(Context);
        var f = await CriarFornecedorAsync();

        Assert.That(await service.AtualizarStatusAsync(f.Id, false), Is.True);
        Assert.That((await Context.Fornecedores.FindAsync(f.Id))!.Ativo, Is.False);
        Assert.That(await service.AtualizarStatusAsync(999, false), Is.False);
    }
}