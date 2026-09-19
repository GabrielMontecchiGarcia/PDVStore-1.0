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

        // CONSTRUTOR: injeta o PVDContext no campo _context. Este serviço executa as
        // verificações pré-login do sistema (configuração, conexão, migrações e
        // administrador). Quem o chama: o container de DI / tela de inicialização.
        // Não lança exceções; apenas armazena a dependência para os métodos
        // privados chamados por VerificarAsync.
        public VerificacaoSistemaService(PDVContext context)
        {
            _context = context;
        }

        // Orquestra as 4 verificações pré-login em sequência: config, conexão,
        // migrações e administrador. Cada passo é um delegate no array `passos` e
        // o resultado é reportado via callback onPasso (para a tela progresso).
        // REGRA de negócio: se QUALQUER passo falhar (Sucesso == false), o
        // processo é interrompido e retorna false — o sistema não segue em frente
        // com pré-requisitos não atendidos. Depende de ConnectionHelper (config)
        // e do PVDContext (conexão/migrações/usuários). Quem chama: a tela de
        // carregamento antes do login.
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

        // Passo 1: verifica se a configuração do sistema está presente. Depende de
        // ConnectionHelper.GetConnectionString (helper estático que retorna a
        // connection string). Considera sucesso quando a string não está vazia e
        // contém "Database=" (indicando uma configuração mínima válida). Não
        // contacta o banco; apenas valida o texto da configuração.
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

        // Passo 2: tenta estabelecer conexão com o banco via CanConnectAsync do EF
        // (depende do PVDContext). Em caso de falha captura a exceção, registra o
        // erro no log (Serilog via Log.Error) e retorna um VerificacaoItem com
        // Sucesso = false — sem deixar a exceção subir e derrubar a tela.
        // Sucesso indica que o SQL Server LocalDB está acessível.
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

        // Passo 3: verifica e aplica MIGRAÇÕES pendentes do EF (depende do
        // PVDContext). Se houver migrações pendentes, chama MigrateAsync para
        // atualizar o banco automaticamente (garante schema correto na primeira
        // execução) e reporta quantas foram aplicadas. Caso não haja pendências,
        // informa "Banco de dados atualizado". Falhas são capturadas, logadas
        // (Serilog) e convertidas em Sucesso = false.
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

        // Passo 4: garante que exista UM usuário administrador ativo no sistema.
        // Regras: se não houver administrador ativo, procura o primeiro usuário
        // existente; se ainda assim não houver nenhum, CRIA um "Admn" com senha
        // padrão "admin123" (primeiro acesso). Se houver admin mas a senha dele
        // não for "admin123", redefine a senha para o padrão e garante
        // Permissao/Ativo. Persiste com SaveChangesAsync. Depende de BCrypt
        // (verificação do hash) e do PVDContext. Falhas são logadas (Serilog).
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