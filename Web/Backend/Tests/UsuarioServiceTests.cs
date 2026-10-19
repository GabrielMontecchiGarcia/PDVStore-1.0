using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Tests.Infrastructure;

namespace PDVStore.Tests;

/// <summary>Testes de usuários: hash BCrypt, unicidade, autenticação e soft delete.</summary>
public class UsuarioServiceTests
{
    [Fact]
    public void Seed_Admin_DeveAutenticarComSenhaInicialAdmin123()
    {
        // Mesmo hash do seed do projeto Windows: garante interoperabilidade e
        // que o primeiro acesso (Admin/admin123) funciona na versão web.
        var admin = new UsuarioCaixa
        {
            Nome = "Admin",
            SenhaHash = "$2a$11$ZBR/L6.DiCjgBWjvnJOsMeVfeHtigReLCpU15E8R8FK/Kgn8n1nZa"
        };

        Assert.True(admin.Autenticar("admin123"));
        Assert.False(admin.Autenticar("outra-senha"));
    }

    [Fact]
    public async Task CriarUsuario_DeveArmazenarHashEAutenticar()
    {
        await using var cenario = CenarioTeste.Criar();

        var usuario = await cenario.Usuarios.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "joao",
            Senha = "senha123",
            ConfirmacaoSenha = "senha123",
            Permissao = (int)TipoPermissao.Administrador
        });

        Assert.NotEqual("senha123", usuario.SenhaHash);
        Assert.StartsWith("$2", usuario.SenhaHash);
        Assert.True(usuario.Ativo);
        Assert.Equal(TipoPermissao.Administrador, usuario.Permissao);

        var autenticado = await cenario.Usuarios.AutenticarAsync("joao", "senha123");
        Assert.NotNull(autenticado);
        Assert.Equal(usuario.Id, autenticado!.Id);

        Assert.Null(await cenario.Usuarios.AutenticarAsync("joao", "senha-errada"));
        Assert.Null(await cenario.Usuarios.AutenticarAsync("inexistente", "senha123"));
    }

    [Fact]
    public async Task CriarUsuario_ComNomeDuplicado_DeveSerBloqueado()
    {
        await using var cenario = CenarioTeste.Criar();
        await cenario.CriarOperadorAsync();

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Usuarios.CriarAsync(new CriarUsuarioRequest
            {
                Nome = "operador",
                Senha = "123456",
                ConfirmacaoSenha = "123456",
                Permissao = (int)TipoPermissao.Operador
            }));

        Assert.Equal("Este nome de usuário já está em uso!", ex.Message);
    }

    [Fact]
    public async Task DesativarUsuario_DeveImpedirAcesso()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();

        await cenario.Usuarios.DesativarAsync(usuarioId);

        var usuario = await cenario.Usuarios.AutenticarAsync("operador", "123456");
        Assert.NotNull(usuario);
        Assert.False(usuario!.Ativo);
    }

    [Fact]
    public async Task AlterarSenha_ComSenhaAtualErrada_DeveRetornarFalse()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();

        var ok = await cenario.Usuarios.AlterarSenhaAsync(usuarioId, "errada", "nova123");

        Assert.False(ok);
        Assert.NotNull(await cenario.Usuarios.AutenticarAsync("operador", "123456"));
    }

    [Fact]
    public async Task AlterarSenha_ComSenhaAtualCorreta_DeveAtualizarHash()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();

        var ok = await cenario.Usuarios.AlterarSenhaAsync(usuarioId, "123456", "nova123");

        Assert.True(ok);
        Assert.Null(await cenario.Usuarios.AutenticarAsync("operador", "123456"));
        Assert.NotNull(await cenario.Usuarios.AutenticarAsync("operador", "nova123"));
    }

    [Fact]
    public async Task EhAdministrador_DeveRefletirOPerfil()
    {
        await using var cenario = CenarioTeste.Criar();
        var admin = await cenario.Usuarios.CriarAsync(new CriarUsuarioRequest
        {
            Nome = "admin2",
            Senha = "123456",
            ConfirmacaoSenha = "123456",
            Permissao = (int)TipoPermissao.Administrador
        });

        Assert.True(await cenario.Usuarios.EhAdministradorAsync(admin.Id));
        Assert.False(await cenario.Usuarios.EhAdministradorAsync(await cenario.CriarOperadorAsync()));
    }
}
