using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PDVStore.Helpers;

namespace PDVStore.Data
{
    public class PDVContextFactory : IDesignTimeDbContextFactory<PDVContext>
    {
        // Factory usada apenas pelas ferramentas de design do EF Core (Add-Migration,
        // Update-Database e similares), que não enxergam o container de DI da aplicação
        // WinForms. Cria um PDVContext avulso usando a mesma string de conexão do
        // ConnectionHelper, permitindo executar migrações fora do runtime do app.
        public PDVContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<PDVContext>();
            builder.UseSqlServer(ConnectionHelper.GetConnectionString());
            return new PDVContext(builder.Options);
        }
    }
}
