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

    // Método de setup executado ANTES de cada teste: cria uma conexão SQLite em memória
    // própria, abre o PDVContext e gera o esquema (EnsureCreated). Garante que cada
    // teste começa com um banco limpo e isolado dos demais.
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

    // Método de limpeza executado DEPOIS de cada teste: libera o contexto e fecha a
    // conexão SQLite. Garante que os recursos sejam devolvidos e que não haja
    // "vazamento" de estado entre um teste e outro.
    [TearDown]
    public void BaseTearDown()
    {
        Context.Dispose();
        _conexao.Dispose();
    }
}