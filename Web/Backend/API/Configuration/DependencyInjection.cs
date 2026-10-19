using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDVStore.API.Common;
using PDVStore.API.Security;
using PDVStore.Application.Interfaces;
using PDVStore.Application.Validators;
using PDVStore.Repository;
using PDVStore.Repository.Context;
using PDVStore.Repository.Interfaces;
using PDVStore.Repository.Repositories;
using PDVStore.Services;
using PDVStore.Services.Integrations;

namespace PDVStore.API.Configuration;

/// <summary>
/// Registro de dependências da aplicação (Dependency Injection).
/// Mantém o mesmo mapeamento serviço → implementação do Program.cs original,
/// porém com ciclo de vida Scoped por requisição HTTP (equivalente ao escopo
/// criado por login no WinForms).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPdvStore(this IServiceCollection services, IConfiguration configuration, TimeZoneInfo timeZone)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' não configurada. Defina ConnectionStrings__DefaultConnection.");

        // Banco de dados — SQL Server, mesmo provider do sistema original.
        services.AddDbContext<PDVStoreDbContext>(options => options.UseSqlServer(connectionString));

        // Infra transversal
        services.AddSingleton(timeZone);
        services.AddSingleton<PeriodoService>();
        services.AddSingleton<JwtTokenService>();

        // Unit of Work e repositórios (Repository Pattern)
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IFornecedorRepository, FornecedorRepository>();
        services.AddScoped<ICaixaRepository, CaixaRepository>();
        services.AddScoped<IVendaRepository, VendaRepository>();
        services.AddScoped<ICompraRepository, CompraRepository>();
        services.AddScoped<IMovimentacaoEstoqueRepository, MovimentacaoEstoqueRepository>();
        services.AddScoped<IRelatorioRepository, RelatorioRepository>();

        // Serviços de negócio
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IProdutoService, ProdutoService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IFornecedorService, FornecedorService>();
        services.AddScoped<ICaixaService, CaixaService>();
        services.AddScoped<IVendaService, VendaService>();
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IRelatorioService, RelatorioService>();
        services.AddScoped<IExportacaoService, ExportacaoService>();

        // Integrações (mock de pagamento PIX/cartão)
        services.AddScoped<IPagamentoIntegrator, PagamentoIntegrator>();

        // Validações server-side (FluentValidation)
        services.AddValidatorsFromAssemblyContaining<ClienteRequestValidator>();

        return services;
    }
}
