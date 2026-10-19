using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace PDVStore.Tests.Integration;

/// <summary>
/// Fábrica de testes de integração da Web API.
/// A configuração é injetada por VARIÁVEIS DE AMBIENTE porque o Program.cs
/// (top-level statements) lê a configuração já na inicialização, antes das
/// sobrescritas do WebApplicationFactory. Cada instância usa um banco InMemory
/// isolado, dispensando SQL Server durante os testes.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _nomeBanco = $"pdvstore-api-tests-{Guid.NewGuid():N}";

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", "InMemory");
        Environment.SetEnvironmentVariable("Database__InMemoryName", _nomeBanco);
        Environment.SetEnvironmentVariable("Database__SeedDemoData", "true");
        Environment.SetEnvironmentVariable("Database__ApplyMigrationsOnStartup", "false");
        Environment.SetEnvironmentVariable("Jwt__Key", "chave-de-teste-com-no-minimo-de-32-caracteres-0123456789");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "PDVStore.WebAPI");
        Environment.SetEnvironmentVariable("Jwt__Audience", "PDVStore.Web");
        Environment.SetEnvironmentVariable("Cors__Origins__0", "http://localhost:5173");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

