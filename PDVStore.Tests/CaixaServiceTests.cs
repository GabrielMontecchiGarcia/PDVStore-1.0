using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class CaixaServiceTests : TesteBanco
{
    [Test]
    public async Task ObterCaixaAberto_QuandoNaoHa_Null()
    {
        var service = new CaixaService(Context);
        Assert.That(await service.ObterCaixaAbertoAsync(), Is.Null);
    }

    [Test]
    public async Task AbrirCaixa_Click()
    {
        var service = new CaixaService(Context);
        var caixa = await service.AbrirCaixaAsync(50m, 1);

        Assert.That(caixa.Id, Is.GreaterThan(0));
        Assert.That(caixa.Status, Is.EqualTo("Aberto"));
        Assert.That(caixa.ValorInicial, Is.EqualTo(50m));
        Assert.That(caixa.UsuarioCaixaId, Is.EqualTo(1));
    }

    [Test]
    public async Task AbrirCaixa_ComCaixaAberto_Rejeita()
    {
        var service = new CaixaService(Context);
        await service.AbrirCaixaAsync(0m, 1);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.AbrirCaixaAsync(0m, 1));
        Assert.That(ex!.Message, Does.Contain("Já existe um caixa aberto"));
    }

    [Test]
    public async Task FecharCaixa_CalculaValorFinal()
    {
        var service = new CaixaService(Context);
        var caixa = await service.AbrirCaixaAsync(100m, 1);

        Context.Vendas.Add(new Venda
        {
            CaixaId = caixa.Id,
            UsuarioCaixaId = 1,
            ValorTotal = 250m,
            Status = "Concluida",
            FormaPagamento = "Dinheiro"
        });
        await Context.SaveChangesAsync();

        bool ok = await service.FecharCaixaAsync(caixa.Id);

        Assert.That(ok, Is.True);
        var fechado = await Context.Caixas.FindAsync(caixa.Id);
        Assert.That(fechado!.Status, Is.EqualTo("Fechado"));
        Assert.That(fechado.ValorFinal, Is.EqualTo(350m)); // 100 inicial + 250 vendas
        Assert.That(fechado.Fechamento, Is.Not.Null);
    }

    [Test]
    public async Task FecharCaixa_DesconsideraVendasCanceladas()
    {
        var service = new CaixaService(Context);
        var caixa = await service.AbrirCaixaAsync(10m, 1);

        Context.Vendas.Add(new Venda { CaixaId = caixa.Id, UsuarioCaixaId = 1, ValorTotal = 999m, Status = "Cancelada" });
        await Context.SaveChangesAsync();

        await service.FecharCaixaAsync(caixa.Id);

        var fechado = await Context.Caixas.FindAsync(caixa.Id);
        Assert.That(fechado!.ValorFinal, Is.EqualTo(10m));
    }

    [Test]
    public async Task FecharCaixa_InexistenteOuJaFechado_RetornaFalse()
    {
        var service = new CaixaService(Context);

        Assert.That(await service.FecharCaixaAsync(999), Is.False);
        Assert.That(await service.FecharCaixaAsync(1), Is.False); // caixa 1 da seed já está fechado
    }

    [Test]
    public async Task RegistrarSangria_ValorNaoPositivo_Rejeita()
    {
        var service = new CaixaService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.RegistrarSangriaAsync(1, 0));
        Assert.That(ex!.ParamName, Is.EqualTo("valor"));
    }

    [Test]
    public async Task RegistrarSangria_EmCaixaAberto_Acumula()
    {
        var service = new CaixaService(Context);
        var caixa = await service.AbrirCaixaAsync(100m, 1);

        Assert.That(await service.RegistrarSangriaAsync(caixa.Id, 30m), Is.True);
        Assert.That(await service.RegistrarSangriaAsync(caixa.Id, 10m), Is.True);

        var atual = await Context.Caixas.FindAsync(caixa.Id);
        Assert.That(atual!.Sangria, Is.EqualTo(40m));
    }

    [Test]
    public async Task RegistrarSangria_CaixaInexistenteOuFechado_RetornaFalse()
    {
        var service = new CaixaService(Context);
        Assert.That(await service.RegistrarSangriaAsync(999, 10m), Is.False);
        Assert.That(await service.RegistrarSangriaAsync(1, 10m), Is.False); // fechado
    }

    [Test]
    public async Task ListarAsync_OrdenaPorAberturaDesc()
    {
        var service = new CaixaService(Context);
        var c1 = await service.AbrirCaixaAsync(0m, 1);
        await service.FecharCaixaAsync(c1.Id);
        var c2 = await service.AbrirCaixaAsync(0m, 1);

        var lista = await service.ListarAsync();

        Assert.That(lista.Count, Is.EqualTo(3)); // seed (id 1) + 2 abertos
        Assert.That(lista[0].Id, Is.EqualTo(c2.Id));
    }
}