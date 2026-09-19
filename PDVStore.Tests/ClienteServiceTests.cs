using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class ClienteServiceTests : TesteBanco
{
    private async Task<Cliente> CriarClienteAsync(string nome = "José", string? doc = "123.456.789-09",
        decimal limite = 0, decimal saldo = 0, bool ativo = true)
    {
        var c = new Cliente
        {
            Nome = nome,
            CpfCnpj = doc,
            LimiteCredito = limite,
            SaldoDevedor = saldo,
            Ativo = ativo,
            Email = nome + "@email.com"
        };
        await Context.Clientes.AddAsync(c);
        await Context.SaveChangesAsync();
        return c;
    }

    [Test]
    public async Task ListarAsync_FiltraAtivosENomeEDocumento()
    {
        var service = new ClienteService(Context);
        await CriarClienteAsync("Ana", doc: "123.456.789-09");
        await CriarClienteAsync("Bruno", doc: "11.222.333/0001-81", ativo: false);
        await CriarClienteAsync("Carla", doc: "111.444.777-35");

        var ativos = await service.ListarAsync();
        Assert.That(ativos.Select(c => c.Nome), Is.EqualTo(new[] { "Ana", "Carla" }));

        var porNome = await service.ListarAsync(true, "Ana");
        Assert.That(porNome.Select(c => c.Nome), Is.EqualTo(new[] { "Ana" }));

        var porDoc = await service.ListarAsync(true, "11223000181");
        Assert.That(porDoc, Is.Empty); // Bruno está inativo

        var todos = await service.ListarAsync(false, "0001");
        Assert.That(todos.Select(c => c.Nome), Is.EqualTo(new[] { "Bruno" }));
    }

    [Test]
    public async Task ObterPorIdAsync_RetornaOuNull()
    {
        var service = new ClienteService(Context);
        var c = await CriarClienteAsync();

        Assert.That((await service.ObterPorIdAsync(c.Id))?.Nome, Is.EqualTo("José"));
        Assert.That(await service.ObterPorIdAsync(999), Is.Null);
    }

    [Test]
    public async Task SalvarAsync_NomeObrigatorio_Rejeita()
    {
        var service = new ClienteService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.SalvarAsync(new Cliente { Nome = "  " }));
        Assert.That(ex!.Message, Does.Contain("obrigatório"));
    }

    [Test]
    public async Task SalvarAsync_NovoCliente_AtribuiIdECadastradoEm()
    {
        var service = new ClienteService(Context);
        var antes = DateTime.UtcNow;

        var cliente = await service.SalvarAsync(new Cliente { Nome = "Marcio", CpfCnpj = "369.421.892-00" });

        Assert.That(cliente.Id, Is.GreaterThan(0));
        Assert.That(cliente.CadastradoEm, Is.GreaterThanOrEqualTo(antes.AddSeconds(-1)));
        Assert.That(await Context.Clientes.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task SalvarAsync_Update_PersisteAlteracoes()
    {
        var service = new ClienteService(Context);
        var c = await CriarClienteAsync(saldo: 50m);

        c.LimiteCredito = 500m;
        c.Email = "novo@email.com";
        await service.SalvarAsync(c);

        var atual = await Context.Clientes.FindAsync(c.Id);
        Assert.That(atual!.LimiteCredito, Is.EqualTo(500m));
        Assert.That(atual.Email, Is.EqualTo("novo@email.com"));
        Assert.That(await Context.Clientes.CountAsync(), Is.EqualTo(1)); // não duplica
    }

    [Test]
    public async Task AtualizarStatusAsync_InativaOuReativaCliente()
    {
        var service = new ClienteService(Context);
        var c = await CriarClienteAsync();

        Assert.That(await service.AtualizarStatusAsync(c.Id, false), Is.True);
        Assert.That((await Context.Clientes.FindAsync(c.Id))!.Ativo, Is.False);
        Assert.That(await service.AtualizarStatusAsync(999, false), Is.False);
    }

    // ===================== ReceberFiadoAsync =====================

    [Test]
    public async Task ReceberFiadoAsync_ValorNaoPositivo_Rejeita()
    {
        var service = new ClienteService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.ReceberFiadoAsync(1, 0));
        Assert.That(ex!.ParamName, Is.EqualTo("valor"));
    }

    [Test]
    public async Task ReceberFiadoAsync_ClienteInexistente_RetornaFalse()
    {
        var service = new ClienteService(Context);
        Assert.That(await service.ReceberFiadoAsync(999, 10m), Is.False);
    }

    [Test]
    public async Task ReceberFiadoAsync_PagamentoParcial_ReduzSaldo()
    {
        var service = new ClienteService(Context);
        var c = await CriarClienteAsync(saldo: 100m);

        Assert.That(await service.ReceberFiadoAsync(c.Id, 30m), Is.True);
        Assert.That((await Context.Clientes.FindAsync(c.Id))!.SaldoDevedor, Is.EqualTo(70m));
    }

    [Test]
    public async Task ReceberFiadoAsync_PagamentoMaiorQueSaldo_QuitaSemNegativar()
    {
        var service = new ClienteService(Context);
        var c = await CriarClienteAsync(saldo: 40m);

        Assert.That(await service.ReceberFiadoAsync(c.Id, 100m), Is.True);
        Assert.That((await Context.Clientes.FindAsync(c.Id))!.SaldoDevedor, Is.EqualTo(0m));
    }
}