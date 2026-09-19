using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;
using PDVStore.Data;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

/// <summary>
/// Testa o fluxo de verificação pré-login. Usa uma base SQLite em memória nova
/// (sem EnsureCreated) para que as migrações sejam realmente aplicadas,
/// simulando uma instalação limpa.
/// </summary>
[TestFixture]
public class VerificacaoSistemaServiceTests
{
    private SqliteConnection _conexao = null!;
    private PDVContext _context = null!;

    // Método de setup executado ANTES de cada teste: abre um SQLite em memória e cria o
    // contexto SEM EnsureCreated. Isso permite que as migrações reais sejam aplicadas,
    // simulando uma instalação limpa do sistema.
    [SetUp]
    public async Task Setup()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

var options = new DbContextOptionsBuilder<PDVContext>()
                .UseSqlite(_conexao)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;
        _context = new PDVContext(options);
        // Sem EnsureCreated: as migrações criam o esquema.
    }

    // Método de limpeza executado DEPOIS de cada teste: libera o contexto e a conexão de
    // memória, devolvendo os recursos utilizados pela verificação.
    [TearDown]
    public async Task TearDown()
    {
        _context.Dispose();
        _conexao.Dispose();
    }

    // Cenário: execução completa da verificação pré-login em uma base recém-criada.
    // Valida que os quatro passos (configuração, banco, migrações e usuário admin) são
    // executados e todos passam. Protege o bootstrap do sistema: sem essa verificação, o
    // login nem mesmo tenta abrir o banco.
    [Test]
    public async Task VerificarAsync_FluxoCompleto_RetornaSucesso()
    {
        var passos = new List<VerificacaoItem>();
        var service = new VerificacaoSistemaService(_context);

        bool ok = await service.VerificarAsync(item => passos.Add(item));

        Assert.That(ok, Is.True, string.Join(" | ", passos.Select(p => $"{p.Descricao} -> {p.Sucesso} ({p.Mensagem})")));
        Assert.That(passos.Count, Is.EqualTo(VerificacaoSistemaService.TotalPassos));
        Assert.That(passos.All(p => p.Sucesso), Is.True);
        Assert.That(passos[0].Descricao, Does.Contain("configuração"));
        Assert.That(passos[1].Descricao, Does.Contain("banco"));
        Assert.That(passos[2].Descricao, Does.Contain("migrações"));
        Assert.That(passos[3].Descricao, Does.Contain("administrativo"));
    }

    // Cenário: usar a verificação para aplicar as migrações em uma base limpa.
    // Valida que, após a verificação, o usuário administrador padrão (admin123) existe e
    // autentica. Protege a regra de que toda instalação ganha um admin inicial para o
    // primeiro acesso.
    [Test]
    public async Task VerificarAsync_ComMigracoesCriaUsuarioAdministrador()
    {
        var service = new VerificacaoSistemaService(_context);

        Assert.That(await service.VerificarAsync(null), Is.True);

        var admin = await _context.Usuarios.FirstOrDefaultAsync(u => u.Permissao == TipoPermissao.Administrador);
        Assert.That(admin, Is.Not.Null);
        Assert.That(admin!.Autenticar("admin123"), Is.True);
    }
}