using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Data;

namespace PDVStore.Tests;

/// <summary>
/// Base compartilhada para os testes que dependem de banco de dados.
/// Cada teste usa um SQLite em memória próprio (mesma conexao para todos os
/// contextos), garantindo isolamento total entre testes.
/// </summary>
public abstract class TesteBanco
{
    protected SqliteConnection _conexao = null!;
    protected PDVContext Context = null!;

    [SetUp]
    public void BaseSetup()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        var options = new DbContextOptionsBuilder<PDVContext>()
            .UseSqlite(_conexao)
            .Options;

        Context = new PDVContext(options);
        Context.Database.EnsureCreated();
    }

    [TearDown]
    public void BaseTearDown()
    {
        Context.Dispose();
        _conexao.Dispose();
    }
}