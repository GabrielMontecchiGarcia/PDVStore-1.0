using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class UsuarioServiceTests : TesteBanco
{
    private static UsuarioCaixa NovoUsuario(string nome = "João") =>
        new()
        {
            Nome = nome,
            Permissao = TipoPermissao.Operador,
            SenhaHash = "123456" // SetSenha é chamado no CriarAsync
        };

    [Test]
    public async Task CriarAsync_NomeObrigatorio_Rejeita()
    {
        var service = new UsuarioService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.CriarAsync(new UsuarioCaixa { Nome = " " }));
        Assert.That(ex!.Message, Does.Contain("obrigatório"));
    }

    [Test]
    public async Task CriarAsync_Sucesso_HasheaSenhaEAtiva()
    {
        var service = new UsuarioService(Context);
        var usuario = await service.CriarAsync(NovoUsuario());

        Assert.That(usuario.Id, Is.GreaterThan(0));
        Assert.That(usuario.SenhaHash, Is.Not.EqualTo("123456")); // hash não é o texto plano
        Assert.That(usuario.Autenticar("123456"), Is.True);
        Assert.That(usuario.GetAtivo(), Is.True);
        Assert.That(usuario.CreatedAt, Is.Not.EqualTo(default));

        var admin = await Context.Usuarios.FindAsync(1);
        Assert.That(admin!.Autenticar("admin123"), Is.True);
    }

    [Test]
    public async Task ObterPorIdAsyncEObterPorNomeAsync()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Maria"));

        Assert.That((await service.ObterPorIdAsync(u.Id))?.Nome, Is.EqualTo("Maria"));
        Assert.That((await service.ObterPorNomeAsync("Maria"))?.Id, Is.EqualTo(u.Id));
        Assert.That(await service.ObterPorIdAsync(999), Is.Null);
        Assert.That(await service.ObterPorNomeAsync("Inexistente"), Is.Null);
    }

    [Test]
    public async Task ListarTodosAsync_FiltraAtivos()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Pedro"));

        Assert.That((await service.ListarTodosAsync(true)).Select(x => x.Id), Does.Contain(u.Id));
        Assert.That((await service.ListarTodosAsync(true)).Count(), Is.EqualTo(2)); // seed Admin + Pedro

        await service.DeletarAsync(u.Id);
        var ativos = (await service.ListarTodosAsync(true)).Select(x => x.Id).ToList();
        Assert.That(ativos, Does.Not.Contain(u.Id));
        Assert.That((await service.ListarTodosAsync(false)).Select(x => x.Id), Does.Contain(u.Id));
    }

    [Test]
    public async Task AtualizarAsync_UsuarioInexistente_Lanca()
    {
        var service = new UsuarioService(Context);
        Assert.ThrowsAsync<KeyNotFoundException>(() => service.AtualizarAsync(NovoUsuario()));
    }

    [Test]
    public async Task AtualizarAsync_AtualizaCamposPermitidos()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Ana"));
        var senhaAntiga = u.SenhaHash;

        u.Nome = "Ana Souza";
        u.Permissao = TipoPermissao.Estoquista;
        await service.AtualizarAsync(u);

        var atual = await Context.Usuarios.FindAsync(u.Id);
        Assert.That(atual!.Nome, Is.EqualTo("Ana Souza"));
        Assert.That(atual.Permissao, Is.EqualTo(TipoPermissao.Estoquista));
        Assert.That(atual.SenhaHash, Is.EqualTo(senhaAntiga)); // senha não altera aqui
    }

    [Test]
    public async Task DeletarAsync_DesativaUsuario()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Lucas"));

        Assert.That(await service.DeletarAsync(u.Id), Is.True);
        Assert.That((await Context.Usuarios.FindAsync(u.Id))!.Ativo, Is.False);
        Assert.That(await service.DeletarAsync(999), Is.False);
    }

    [Test]
    public async Task AutenticarAsync_ValidaCredenciais()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Carlos"));

        Assert.That((await service.AutenticarAsync("Carlos", "123456"))?.Id, Is.EqualTo(u.Id));
        Assert.That(await service.AutenticarAsync("Carlos", "errada"), Is.Null);
        Assert.That(await service.AutenticarAsync("Ninguém", "123456"), Is.Null);
    }

    [Test]
    public async Task AlterarSenhaAsync_FluxoCompleto()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Rita"));

        Assert.That(await service.AlterarSenhaAsync(u.Id, "senhaErrada", "nova"), Is.False);
        Assert.That(await service.AlterarSenhaAsync(999, "123456", "nova"), Is.False);

        Assert.That(await service.AlterarSenhaAsync(u.Id, "123456", "novaSenha"), Is.True);
        Assert.That((await service.AutenticarAsync("Rita", "novaSenha"))?.Id, Is.EqualTo(u.Id));
        Assert.That(await service.AutenticarAsync("Rita", "123456"), Is.Null);
    }

    [Test]
    public async Task EhAdministradorAsync()
    {
        var service = new UsuarioService(Context);
        var admin = await Context.Usuarios.FirstAsync(u => u.Permissao == TipoPermissao.Administrador);
        var op = await service.CriarAsync(NovoUsuario("Zé"));

        Assert.That(await service.EhAdministradorAsync(admin.Id), Is.True);
        Assert.That(await service.EhAdministradorAsync(op.Id), Is.False);
        Assert.That(await service.EhAdministradorAsync(999), Is.False);
    }

    [Test]
    public async Task ListarAdministradoresAsync()
    {
        var service = new UsuarioService(Context);
        var lista = await service.ListarAdministradoresAsync();

        Assert.That(lista.Count(), Is.EqualTo(1));
        Assert.That(lista.Single().Nome, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task GetById_GenericoEncontraEntidade()
    {
        var service = new UsuarioService(Context);
        var p = new Produto { Nome = "Genérico", CodigoBarras = "1" };
        await Context.Produtos.AddAsync(p);
        await Context.SaveChangesAsync();

        var encontrado = service.GetById<Produto>(p.Id);
        Assert.That(encontrado?.Nome, Is.EqualTo("Genérico"));
        Assert.That(service.GetById<Produto>(999), Is.Null);
    }
}