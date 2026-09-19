using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class FornecedorServiceTests : TesteBanco
{
    // Método auxiliar de montagem: cadastra um fornecedor com nome, CNPJ, e-mail e status
    // ativo/inativo. Usado para preparar os cenários de listagem e edição.
    private async Task<Fornecedor> CriarFornecedorAsync(string nome = "Distribuidora", bool ativo = true)
    {
        var f = new Fornecedor { Nome = nome, Cnpj = "11.222.333/0001-81", Email = nome + "@forn.com", Ativo = ativo };
        await Context.Fornecedores.AddAsync(f);
        await Context.SaveChangesAsync();
        return f;
    }

    // Cenário: fornecedores ativos e inativos, com filtros por nome e por status.
    // Valida que a listagem traz apenas ativos por padrão e que o filtro respeita a
    // flag "incluir inativos". Protege a tela de compras contra fornecedores suspensos.
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

    // Cenário: fornecedor existente e um Id inexistente (999).
    // Valida o retorno do fornecedor ou null. Protege a consulta individual usada na
    // edição da ficha do fornecedor sem lançar exceção para Ids inválidos.
    [Test]
    public async Task ObterPorIdAsync_RetornaOuNull()
    {
        var service = new FornecedorService(Context);
        var f = await CriarFornecedorAsync();

        Assert.That((await service.ObterPorIdAsync(f.Id))?.Nome, Is.EqualTo("Distribuidora"));
        Assert.That(await service.ObterPorIdAsync(999), Is.Null);
    }

    // Cenário: fornecedor com nome vazio.
    // Valida a rejeição com ArgumentException mencionando "obrigatório". Protege a
    // regra de que todo fornecedor precisa de nome para ser identificado.
    [Test]
    public async Task SalvarAsync_NomeObrigatorio_Rejeita()
    {
        var service = new FornecedorService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.SalvarAsync(new Fornecedor { Nome = "" }));
        Assert.That(ex!.Message, Does.Contain("obrigatório"));
    }

    // Cenário: cadastro de fornecedor novo e, em seguida, edição do e-mail.
    // Valida que o salvar cria (Id gerado) e atualiza sem duplicar o registro.
    // Protege o fluxo de inclusão/edição de fornecedores usado nas compras.
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

    // Cenário: inativar um fornecedor existente e tentar inativar um Id inexistente.
    // Valida o retorno true/false e a persistência do status. Protege a inativação
    // lógica, que impede novas compras do fornecedor sem apagar seu histórico.
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