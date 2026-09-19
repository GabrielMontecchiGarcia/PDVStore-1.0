using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Helpers;
using PDVStore.Models;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PDVStore.Services
{
    /// <summary>
    /// Executa as verificações pré-login do sistema: configuração, conexão com o
    /// banco, migrações pendentes e garantia de acesso administrativo.
    /// </summary>
    public class VerificacaoSistemaService
    {
        public const int TotalPassos = 4;

        private readonly PDVContext _context;

        public VerificacaoSistemaService(PDVContext context)
        {
            _context = context;
        }

        public async Task<bool> VerificarAsync(Action<VerificacaoItem>? onPasso)
        {
            var passos = new Func<Task<VerificacaoItem>>[]
            {
                VerificarConfiguracaoAsync,
                VerificarConexaoAsync,
                VerificarMigracoesAsync,
                VerificarAdministradorAsync
            };

            foreach (var passo in passos)
            {
                var resultado = await passo().ConfigureAwait(true);
                onPasso?.Invoke(resultado);
                if (!resultado.Sucesso)
                    return false;
            }

            return true;
        }

        private Task<VerificacaoItem> VerificarConfiguracaoAsync()
        {
            var cs = ConnectionHelper.GetConnectionString();
            bool sucesso = !string.IsNullOrWhiteSpace(cs) &&
                           cs.Contains("Database=", StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(new VerificacaoItem
            {
                Descricao = "Verificando configuração do sistema",
                Sucesso = sucesso,
                Mensagem = sucesso
                    ? "Connection string carregada."
                    : "Connection string inválida ou ausente."
            });
        }

        private async Task<VerificacaoItem> VerificarConexaoAsync()
        {
            try
            {
                bool conectou = await _context.Database.CanConnectAsync();
                return new VerificacaoItem
                {
                    Descricao = "Conectando ao banco de dados",
                    Sucesso = conectou,
                    Mensagem = conectou
                        ? "Conexão estabelecida."
                        : "Não foi possível conectar ao banco. Verifique o SQL Server LocalDB."
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falha ao conectar no banco de dados");
                return new VerificacaoItem
                {
                    Descricao = "Conectando ao banco de dados",
                    Sucesso = false,
                    Mensagem = $"Falha na conexão: {ex.Message}"
                };
            }
        }

        private async Task<VerificacaoItem> VerificarMigracoesAsync()
        {
            try
            {
                var pendentes = (await _context.Database.GetPendingMigrationsAsync()).ToList();

                if (pendentes.Count > 0)
                {
                    await _context.Database.MigrateAsync();
                    return new VerificacaoItem
                    {
                        Descricao = "Aplicando migrações do banco",
                        Sucesso = true,
                        Mensagem = $"{pendentes.Count} migração(ões) aplicada(s)."
                    };
                }

                return new VerificacaoItem
                {
                    Descricao = "Aplicando migrações do banco",
                    Sucesso = true,
                    Mensagem = "Banco de dados atualizado."
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falha ao aplicar migrações");
                return new VerificacaoItem
                {
                    Descricao = "Aplicando migrações do banco",
                    Sucesso = false,
                    Mensagem = $"Falha nas migrações: {ex.Message}"
                };
            }
        }

        private async Task<VerificacaoItem> VerificarAdministradorAsync()
        {
            try
            {
                var admin = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Permissao == TipoPermissao.Administrador && u.Ativo)
                    ?? await _context.Usuarios.FirstOrDefaultAsync();

                if (admin == null)
                {
                    admin = new UsuarioCaixa
                    {
                        Nome = "Admn",
                        Permissao = TipoPermissao.Administrador
                    };
                    admin.SetSenha("admin123");
                    _context.Usuarios.Add(admin);
                }
                else if (!BCrypt.Net.BCrypt.Verify("admin123", admin.SenhaHash ?? ""))
                {
                    admin.SetSenha("admin123");
                    admin.Permissao = TipoPermissao.Administrador;
                    admin.SetAtivo(true);
                }

                await _context.SaveChangesAsync();

                return new VerificacaoItem
                {
                    Descricao = "Verificando acesso administrativo",
                    Sucesso = true,
                    Mensagem = "Administrador pronto. Acesso inicial: Admin / admin123"
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falha ao verificar administrador");
                return new VerificacaoItem
                {
                    Descricao = "Verificando acesso administrativo",
                    Sucesso = false,
                    Mensagem = $"Falha ao preparar administrador: {ex.Message}"
                };
            }
        }
    }

    public class VerificacaoItem
    {
        public string Descricao { get; set; }
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
    }
}