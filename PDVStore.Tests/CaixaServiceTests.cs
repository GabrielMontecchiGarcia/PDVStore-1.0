using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class CaixaServiceTests : TesteBanco
{
    // Cenário: não existe nenhum caixa aberto cadastrado no banco.
    // Valida que ObterCaixaAbertoAsync retorna null em vez de lançar exceção. Protege
    // a regra de que a ausência de caixa é um estado normal — a tela apenas precisa
    // ser orientada a abrir um novo caixa.
    [Test]
    public async Task ObterCaixaAberto_QuandoNaoHa_Null()
    {
        var service = new CaixaService(Context);
        Assert.That(await service.ObterCaixaAbertoAsync(), Is.Null);
    }

    // Cenário: fluxo feliz de abertura de caixa com valor inicial de R$ 50 para o usuário 1.
    // Valida que o caixa é persistido com Id gerado, status "Aberto", valor inicial e
    // vínculo com o usuário registrados. Protege a regra de que todo caixa aberto nasce
    // identificado e no estado correto para receber vendas.
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

    // Cenário: já existe um caixa aberto e tenta-se abrir outro.
    // Valida a rejeição com InvalidOperationException ("Já existe um caixa aberto").
    // Protege a regra de negócio de que só pode haver um caixa ativo por vez no PDV.
    [Test]
    public async Task AbrirCaixa_ComCaixaAberto_Rejeita()
    {
        var service = new CaixaService(Context);
        await service.AbrirCaixaAsync(0m, 1);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.AbrirCaixaAsync(0m, 1));
        Assert.That(ex!.Message, Does.Contain("Já existe um caixa aberto"));
    }

    // Cenário: caixa aberto com R$ 100 inicial e uma venda concluída de R$ 250.
    // Valida o fechamento: valor final = inicial + vendas (350), status "Fechado" e
    // data de fechamento gravados. Protege o cálculo do resumo financeiro do dia.
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

    // Cenário: caixa com R$ 10 inicial e uma venda CANCELADA de R$ 999.
    // Valida que vendas canceladas não entram no valor final (permanece 10). Protege
    // a regra de que só vendas concluídas geram dinheiro de verdade no caixa.
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

    // Cenário: fechar caixa com Id inexistente (999) ou já fechado (seed).
    // Valida que a operação falha de forma silenciosa (retorna false, sem exceção).
    // Protege contra cliques duplicados e Ids inválidos vindos da interface.
    [Test]
    public async Task FecharCaixa_InexistenteOuJaFechado_RetornaFalse()
    {
        var service = new CaixaService(Context);

        Assert.That(await service.FecharCaixaAsync(999), Is.False);
        Assert.That(await service.FecharCaixaAsync(1), Is.False); // caixa 1 da seed já está fechado
    }

    // Cenário: sangria com valor zero.
    // Valida a rejeição com ArgumentException apontando o parâmetro "valor". Protege
    // a regra de que retiradas de caixa só são aceitas com valores positivos.
    [Test]
    public async Task RegistrarSangria_ValorNaoPositivo_Rejeita()
    {
        var service = new CaixaService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.RegistrarSangriaAsync(1, 0));
        Assert.That(ex!.ParamName, Is.EqualTo("valor"));
    }

    // Cenário: duas sangrias (30 e 10) em um caixa aberto.
    // Valida que o campo Sangria acumula os valores (40). Protege o total de dinheiro
    // retirado durante o expediente, informação usada no fechamento do caixa.
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

    // Cenário: sangria em caixa inexistente (999) ou já fechado (seed).
    // Valida o retorno false (falha silenciosa). Protege a regra de que só caixas
    // abertos podem sofrer retirada de dinheiro.
    [Test]
    public async Task RegistrarSangria_CaixaInexistenteOuFechado_RetornaFalse()
    {
        var service = new CaixaService(Context);
        Assert.That(await service.RegistrarSangriaAsync(999, 10m), Is.False);
        Assert.That(await service.RegistrarSangriaAsync(1, 10m), Is.False); // fechado
    }

    // Cenário: dois caixas abertos em sequência (totalizando 3 registros com a seed).
    // Valida que a listagem retorna ordenada pela abertura mais recente primeiro.
    // Protege a tela de histórico, que deve exibir o caixa mais novo no topo.
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