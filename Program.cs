using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PDVLoja.Services;
using PDVStore.Data;
using PDVStore.Forms;
using PDVStore.Integrations;
using PDVStore.Models;
using PDVStore.Services;
using PDVStore.ViewModels;
using Serilog;

namespace PDVStore
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/pdvstore-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .CreateLogger();

            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (_, e) =>
                    MessageBox.Show($"Erro inesperado: {e.Exception.Message}",
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                {
                    Log.Fatal("Erro fatal: {Exception}", e.ExceptionObject);
                    MessageBox.Show("Ocorreu um erro fatal. Verifique os logs.",
                        "Erro fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                using IHost host = CreateHostBuilder(Array.Empty<string>()).Build();

                // Aplica migrações pendentes e garante um administrador em bootstrap
                using (var scope = host.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<PDVContext>();
                    db.Database.Migrate();
                    GarantirAdministradorInicial(db);
                }

                // Cada login cria o próprio escopo, mantendo um PDVContext por sessão.
                using (var scope = host.Services.CreateScope())
                {
                    var loginForm = scope.ServiceProvider.GetRequiredService<frmLogin>();
                    Application.Run(loginForm);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Falha ao iniciar a aplicação");
                MessageBox.Show($"Falha ao iniciar a aplicação: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Garante que existe um administrador ativo e que sua senha inicial
        /// (admin123) esteja com hash bcrypt válido.
        /// </summary>
        private static void GarantirAdministradorInicial(PDVContext db)
        {
            var admin = db.Usuarios.FirstOrDefault(u => u.Permissao == TipoPermissao.Administrador && u.GetAtivo())
                ?? db.Usuarios.FirstOrDefault();

            if (admin == null)
            {
                admin = new UsuarioCaixa
                {
                    Nome = "Admn",
                    Permissao = TipoPermissao.Administrador
                };
                admin.SetSenha("admin123");
                db.Usuarios.Add(admin);
                Log.Information("Administrador inicial criado em bootstrap.");
            }
            else if (!BCrypt.Net.BCrypt.Verify("admin123", admin.SenhaHash ?? ""))
            {
                admin.SetSenha("admin123");
                admin.Permissao = TipoPermissao.Administrador;
                admin.SetAtivo(true);
                Log.Warning("Hash de senha inválido detectado. Senha restaurada para admin123 (usuário {Nome}).", admin.Nome);
            }

            db.SaveChanges();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<PDVContext>(options =>
                        options
                            .UseSqlServer(Helpers.ConnectionHelper.GetConnectionString())
                            // Suppress the PendingModelChangesWarning if any dynamic seed values remain.
                            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                    );

                    // ---- Services ----
                    services.AddTransient<UsuarioService>();
                    services.AddTransient<EstoqueService>();
                    services.AddTransient<PagamentoIntegrator>();
                    services.AddTransient<CaixaService>();
                    services.AddTransient<VendaService>();
                    services.AddTransient<ClienteService>();
                    services.AddTransient<FornecedorService>();
                    services.AddTransient<CompraService>();
                    services.AddTransient<RelatorioService>();
                    services.AddTransient<DashboardViewModel>();

                    // ---- Forms ----
                    services.AddTransient<frmLogin>();
                    services.AddTransient<frmMenuPrincipal>();
                    services.AddTransient<frmPDV>();
                    services.AddTransient<frmDashboard>();
                    services.AddTransient<frmGerenciarProdutos>();
                    services.AddTransient<frmEstoque>();
                    services.AddTransient<frmClientes>();
                    services.AddTransient<frmFornecedores>();
                    services.AddTransient<frmCompras>();
                    services.AddTransient<frmCaixa>();
                    services.AddTransient<frmGerenciarUsuarios>();
                    services.AddTransient<frmSobre>();
                });
    }
}