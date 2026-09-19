using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class UsuarioServiceTests : TesteBanco
{
    // Método auxiliar de montagem: cria (sem gravar) um usuário com permissão Operador e
    // o nome informado. O valor em SenhaHash é apenas um placeholder — o hash real é
    // gerado pelo próprio serviço no CriarAsync, como os testes verificam.
    private static UsuarioCaixa NovoUsuario(string nome = "João") =>
        new()
        {
            Nome = nome,
            Permissao = TipoPermissao.Operador,
            SenhaHash = "123456" // SetSenha é chamado no CriarAsync
        };

    // Cenário: criação de usuário com nome em branco.
    // Valida a rejeição com ArgumentException mencionando "obrigatório". Protege a
    // regra de que todo usuário precisa de nome para ser identificado no sistema.
    [Test]
    public async Task CriarAsync_NomeObrigatorio_Rejeita()
    {
        var service = new UsuarioService(Context);
        var ex = Assert.ThrowsAsync<ArgumentException>(() => service.CriarAsync(new UsuarioCaixa { Nome = " " }));
        Assert.That(ex!.Message, Does.Contain("obrigatório"));
    }

    // Cenário: criação feliz de um usuário operador, com senha "123456" e verificação do
    // usuário administrador de seed (admin123).
    // Valida que o Id é gerado, a senha é armazenada como HASH (nunca texto plano), a
    // autenticação funciona e o usuário nasce ativo e com data de criação. Protege a
    // segurança do login e a existência de um admin inicial.
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

    // Cenário: consultar o usuário recém-criado pelo Id e pelo nome de login.
    // Valida os dois caminhos de busca e o retorno null para quem não existe. Protege
    // o login (busca por nome) e a edição (busca por Id) sem lançar exceções.
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

    // Cenário: criar um usuário, "deletá-lo" e depois listar com e sem filtro de ativos.
    // Valida que usuários deletados somem da lista de ativos e continuem aparecendo na
    // listagem geral. Protege o histórico de usuários sem liberar login para quem foi
    // desligado.
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

    // Cenário: tentar atualizar um usuário que não existe no banco.
    // Valida que o serviço lança KeyNotFoundException. Protege contra tentativas de
    // editar registros inexistentes ou já removidos.
    [Test]
    public async Task AtualizarAsync_UsuarioInexistente_Lanca()
    {
        var service = new UsuarioService(Context);
        Assert.ThrowsAsync<KeyNotFoundException>(() => service.AtualizarAsync(NovoUsuario()));
    }

    // Cenário: editar nome e permissão de um usuário já existente.
    // Valida que os campos são persistidos, mas a senha NÃO é alterada nessa operação.
    // Protege a regra de segurança de que a senha só muda pelo fluxo próprio de
    // alteração de senha, nunca na edição comum do cadastro.
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

    // Cenário: deletar um usuário existente e tentar deletar um Id inexistente (999).
    // Valida que a exclusão é lógica (Ativo = false) e que Id inválido retorna false.
    // Protege a auditoria/histórico de vendas que referenciam o usuário.
    [Test]
    public async Task DeletarAsync_DesativaUsuario()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Lucas"));

        Assert.That(await service.DeletarAsync(u.Id), Is.True);
        Assert.That((await Context.Usuarios.FindAsync(u.Id))!.Ativo, Is.False);
        Assert.That(await service.DeletarAsync(999), Is.False);
    }

    // Cenário: autenticar com usuário/senha corretos, com senha errada e com usuário
    // inexistente.
    // Valida que apenas credenciais válidas retornam o usuário. Protege o acesso ao
    // sistema: senha incorreta ou usuário desconhecido vem null.
    [Test]
    public async Task AutenticarAsync_ValidaCredenciais()
    {
        var service = new UsuarioService(Context);
        var u = await service.CriarAsync(NovoUsuario("Carlos"));

        Assert.That((await service.AutenticarAsync("Carlos", "123456"))?.Id, Is.EqualTo(u.Id));
        Assert.That(await service.AutenticarAsync("Carlos", "errada"), Is.Null);
        Assert.That(await service.AutenticarAsync("Ninguém", "123456"), Is.Null);
    }

    // Cenário: troca de senha com a senha atual errada, com usuário inexistente e com o
    // fluxo completo correto.
    // Valida que a troca exige a senha atual, que após a alteração apenas a nova senha
    // funciona e a antiga deixa de valer. Protege a segurança da conta: ninguém troca a
    // senha de outro sem conhecer a atual.
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

    // Cenário: verificação do papel administrativo de um admin (seed), de um operador e de
    // um Id inexistente.
    // Valida que somente o administrador retorna true. Protege o controle de acesso do
    // sistema, que restringe funcionalidades sensíveis a quem tem permissão de admin.
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

    // Cenário: listagem dos administradores do sistema em um banco com o usuário seed.
    // Valida que o usuário "Admin" (criado pela seed) aparece na relação. Protege a
    // gestão de usuários, que precisa saber quem pode gerenciar o sistema.
    [Test]
    public async Task ListarAdministradoresAsync()
    {
        var service = new UsuarioService(Context);
        var lista = await service.ListarAdministradoresAsync();

        Assert.That(lista.Count(), Is.EqualTo(1));
        Assert.That(lista.Single().Nome, Is.EqualTo("Admin"));
    }

    // Cenário: uso do método genérico GetById<T> com uma entidade do contexto (Produto).
    // Valida que o método encontra a entidade pelo Id e retorna null para Id inexistente.
    // Protege a utilidade genérica de consulta reutilizada nos serviços do sistema.
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