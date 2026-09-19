using System;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PDVLoja.Services;
using PDVStore.Data;
using PDVStore.Forms;
using PDVStore.Integrations;
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

                // Splash: verifica configuração, conexão, migrações e o acesso
                // administrativo antes de liberar o login.
                frmLogin? loginForm = null;
                IServiceScope? loginScope = null;

                using (var splashScope = host.Services.CreateScope())
                {
                    var splash = splashScope.ServiceProvider.GetRequiredService<frmSplash>();
                    splash.VerificacaoConcluida += (_, ok) =>
                    {
                        if (ok)
                        {
                            // Cada login cria o próprio escopo, mantendo um
                            // PDVContext por sessão.
                            loginScope = host.Services.CreateScope();
                            loginForm = loginScope.ServiceProvider.GetRequiredService<frmLogin>();
                        }
                        else
                        {
                            // Falha já é exibida na própria splash.
                            Log.Warning("Verificação do sistema falhou: {Falha}", splash.UltimaFalha);
                        }
                    };

                    Application.Run(splash);
                }

                if (loginForm != null)
                {
                    Application.Run(loginForm);
                    loginScope?.Dispose();
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
                    services.AddTransient<VerificacaoSistemaService>();
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
                    services.AddTransient<frmSplash>();
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