using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDVStore.Setup.Services;

/// <summary>
/// Verifica a presença dos pré-requisitos (SDK .NET, LocalDB, dotnet-ef).
/// </summary>
public static class VerificadorRequisitos
{
    // Verifica em paralelo os três pré-requisitos (SDK .NET, LocalDB e dotnet-ef), limpa a lista
    // anterior e preenche ctx.Requisitos com o resultado de cada checagem.
    // POR QUE esvaziar primeiro: garante que a lista reflita apenas o estado atual da verificação,
    // sem resíduos de uma execução anterior.
    // Dependências: SetupContext (onde os resultados são gravados), os métodos privados
    // VerificarSdkAsync/VerificarLocalDbAsync/VerificarDotNetEfAsync e o enum StatusRequisito.
    public static async Task CarregarAsync(SetupContext ctx, Action<string>? onLine = null)
    {
        ctx.Requisitos.Clear();

        ctx.Requisitos.Add(await VerificarSdkAsync(ctx));
        ctx.Requisitos.Add(await VerificarLocalDbAsync(ctx));
        ctx.Requisitos.Add(await VerificarDotNetEfAsync(ctx));
    }

    // Verifica se há um SDK .NET 8+ instalado, executando 'dotnet --list-sdks' e procurando linhas
    // cuja versão comece com 8 ou 9.
    // POR QUE exigir 8 ou 9: o projeto foi desenvolvido para .NET 8, e o SDK 9 também compila
    // projetos 8; qualquer versão anterior não atende aos requisitos.
    // Dependências: InstaladorInfo.LocalizarDotnet, ProcessUtil.RunAsync e SetupContext, onde o
    // resultado é gravado na flag SdkDotNetInstalado.
    private static async Task<Requisito> VerificarSdkAsync(SetupContext ctx)
    {
        var requisito = new Requisito
        {
            Nome = ".NET SDK 8+",
            Detalhe = "Necessário para compilar, publicar e aplicar migrações do EF Core."
        };

        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (string.IsNullOrEmpty(dotnet))
        {
            requisito.Status = StatusRequisito.Ausente;
            requisito.Detalhe += " Não encontrado na máquina.";
            ctx.SdkDotNetInstalado = false;
            return requisito;
        }

        try
        {
            var r = await ProcessUtil.RunAsync(dotnet, new[] { "--list-sdks" });
            if (r.ExitCode == 0 && r.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Any(l => l.TrimStart().StartsWith("8") || l.TrimStart().StartsWith("9")))
            {
                requisito.Status = StatusRequisito.Instalado;
                requisito.Detalhe = "Versões encontradas:" + Environment.NewLine + r.Output.Trim();
                ctx.SdkDotNetInstalado = true;
            }
            else
            {
                requisito.Status = StatusRequisito.Ausente;
                requisito.Detalhe = "SDK 8+ não localizado (encontrado: " + r.Output.Trim() + ")";
                ctx.SdkDotNetInstalado = false;
            }
        }
        catch (Exception ex)
        {
            requisito.Status = StatusRequisito.Ausente;
            requisito.Detalhe = "Erro ao verificar SDK: " + ex.Message;
            ctx.SdkDotNetInstalado = false;
        }
        return requisito;
    }

    // Verifica se o SQL Server Express LocalDB está instalado, procurando o SqlLocalDB.exe nas
    // pastas de instalação e na pasta do sistema (32/64 bits).
    // POR QUE duas fontes de busca: o LocalDB pode estar no "Microsoft SQL Server" (instalação
    // tradicional) ou no System32 (instalação como ferramenta nativa); o caminho encontrado fica
    // salvo em ctx.CaminhoSqlLocalDb para uso posterior na criação da instância.
    // Dependências: LocalizarSqlLocalDb(), System.IO e SetupContext (flags LocalDbInstalado).
    private static async Task<Requisito> VerificarLocalDbAsync(SetupContext ctx)
    {
        var requisito = new Requisito
        {
            Nome = "SQL Server Express LocalDB",
            Detalhe = "Necessário para hospedar o banco PDV_StoreDB em (localdb)\\MSSQLLocalDB."
        };

        var caminho = LocalizarSqlLocalDb();
        if (!string.IsNullOrEmpty(caminho))
        {
            requisito.Status = StatusRequisito.Instalado;
            requisito.Detalhe = "Ferramenta localizada: " + caminho;
            ctx.LocalDbInstalado = true;
            ctx.CaminhoSqlLocalDb = caminho;
            return requisito;
        }

        var arq = Environment.Is64BitOperatingSystem ? "SqlLocalDB_64.exe" : "SqlLocalDB_32.exe";
        var caminhoSys = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), arq);
        if (File.Exists(caminhoSys))
        {
            requisito.Status = StatusRequisito.Instalado;
            requisito.Detalhe = "Ferramenta localizada: " + caminhoSys;
            ctx.LocalDbInstalado = true;
            ctx.CaminhoSqlLocalDb = caminhoSys;
            return requisito;
        }

        requisito.Status = StatusRequisito.Ausente;
        requisito.Detalhe = "Não instalado. O instalador baixará o SqlLocalDB.msi oficial.";
        ctx.LocalDbInstalado = false;
        return requisito;
    }

    // Percorre as pastas "Microsoft SQL Server" (Program Files e Program Files (x86)) em busca do
    // SqlLocalDB.exe, que indica a instalação do LocalDB.
    // POR QUE varrer por versão: cada versão instalada cria sua própria subpasta; a primeira
    // encontrada com Tools\Binn\SqlLocalDB.exe já é suficiente para o instalador.
    // Dependências: System.IO (DirectoryInfo/File.Exists) e Environment.SpecialFolder para
    // resolver as pastas de programa; retorna "" quando não encontra nada.
    private static string LocalizarSqlLocalDb()
    {
        foreach (var raiz in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        })
        {
            var baseSql = Path.Combine(raiz, "Microsoft SQL Server");
            if (!Directory.Exists(baseSql)) continue;
            try
            {
                foreach (var versao in Directory.GetDirectories(baseSql))
                {
                    var candidato = Path.Combine(versao, "Tools", "Binn", "SqlLocalDB.exe");
                    if (File.Exists(candidato))
                        return candidato;
                }
            }
            catch
            {
                // continua procurando
            }
        }
        return "";
    }

    // Verifica se a ferramenta global dotnet-ef está instalada, executando 'dotnet tool list
    // --global' e procurando pelo nome na lista.
    // POR QUE a checagem da ferramenta: sem ela o instalador não consegue aplicar migrações do
    // banco; a flag DotNetEfInstalado no contexto orienta o PreparadorAmbiente a instalá-la.
    // Dependências: InstaladorInfo.LocalizarDotnet, ProcessUtil.RunAsync e SetupContext (flag
    // DotNetEfInstalado).
    private static async Task<Requisito> VerificarDotNetEfAsync(SetupContext ctx)
    {
        var requisito = new Requisito
        {
            Nome = "dotnet-ef (EF Core Tools)",
            Detalhe = "Necessário para aplicar migrações de banco pelo instalador."
        };

        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (string.IsNullOrEmpty(dotnet))
        {
            requisito.Status = StatusRequisito.Ausente;
            requisito.Detalhe = "Sem SDK .NET para executar a ferramenta.";
            ctx.DotNetEfInstalado = false;
            return requisito;
        }

        try
        {
            var r = await ProcessUtil.RunAsync(dotnet, new[] { "tool", "list", "--global" });
            if (r.ExitCode == 0 && r.Output.Contains("dotnet-ef", StringComparison.OrdinalIgnoreCase))
            {
                requisito.Status = StatusRequisito.Instalado;
                requisito.Detalhe = r.Output.Trim();
                ctx.DotNetEfInstalado = true;
            }
            else
            {
                requisito.Status = StatusRequisito.Ausente;
                requisito.Detalhe = "Ferramenta global não instalada. O instalador executará 'dotnet tool install --global dotnet-ef'.";
                ctx.DotNetEfInstalado = false;
            }
        }
        catch (Exception ex)
        {
            requisito.Status = StatusRequisito.Ausente;
            requisito.Detalhe = "Erro ao verificar dotnet-ef: " + ex.Message;
            ctx.DotNetEfInstalado = false;
        }
        return requisito;
    }
}