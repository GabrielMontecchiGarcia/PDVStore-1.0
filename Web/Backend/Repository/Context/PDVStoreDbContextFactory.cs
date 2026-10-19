using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PDVStore.Repository.Context;

/// <summary>
/// Factory usada apenas pelas ferramentas de design do EF Core
/// (dotnet ef migrations add/update). Fora do runtime da API; a connection string
/// vem da variável de ambiente ConnectionStrings__DefaultConnection ou usa o
/// SQL Server local como padrão.
/// </summary>
public class PDVStoreDbContextFactory : IDesignTimeDbContextFactory<PDVStoreDbContext>
{
    public PDVStoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=PDVStore;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

        var builder = new DbContextOptionsBuilder<PDVStoreDbContext>();
        builder.UseSqlServer(connectionString);

        return new PDVStoreDbContext(builder.Options);
    }
}
