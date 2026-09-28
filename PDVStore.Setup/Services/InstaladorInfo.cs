using System;
using System.Collections.Generic;
using System.IO;

namespace PDVStore.Setup.Services;

/// <summary>
/// Informações centralizadas do instalador: repositório de origem no GitHub, URLs oficiais de
/// download e caminhos usados em todo o processo.
/// </summary>
public static class InstaladorInfo
{
    // ---- Código-fonte: tudo vem do GitHub ----
    // O instalador baixa o zip do repositório, que já traz o projeto completo (.csproj,
    // Migrations, Resourses, etc.). O Git é apenas um plano B quando o download falha.
    public const string RepoDono = "marciodeandrade1";
    public const string RepoNome = "PDVStore-1.0";
    public const string RepoUrlGit = "https://github.com/marciodeandrade1/PDVStore-1.0.git";
    public const string RepoPagina = "https://github.com/marciodeandrade1/PDVStore-1.0";
    public const string RepoApi = "https://api.github.com/repos/marciodeandrade1/PDVStore-1.0";
    public const string BranchPadrao = "master";
    public const string BranchAlternativa = "main";
    public const string CsprojPrincipal = "PDVStore.csproj";

    // ---- Pré-requisitos baixados sob demanda ----
    public const string DotNetInstallScriptUrl = "https://dot.net/v1/dotnet-install.ps1";
    public const string DotNetChannel = "8.0";
    public const string SqlLocalDbMsiUrl =
        "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi";

    // ---- Banco de dados ----
    public const string InstanciaLocalDb = "MSSQLLocalDB";
    public const string BancoPadrao = "PDV_StoreDB";

    // ---- Destino da instalação ----
    // A instalação acontece em C:\PDVStore, criada pelo próprio instalador. O código-fonte
    // baixado do GitHub fica em uma subpasta "source" para que tudo fique junto e rastreável.
    public const string PastaPadraoInstalacao = @"C:\PDVStore";
    public const string PastaFonteRelativa = "source";

    // Monta a URL do zip do repositório para a branch informada (o GitHub empacota a branch
    // como /archive/refs/heads/<branch>.zip). O zip sempre chega dentro de uma pasta única
    // chamada "<repo>-<branch>", que é desembrulhada na extração.
    public static string ZipUrl(string branch)
        => $"https://github.com/{RepoDono}/{RepoNome}/archive/refs/heads/{branch}.zip";

    // Pasta temporária para o script de instalação do .NET e o MSI do LocalDB (baixados na
    // etapa de preparação e descartados com o %TEMP% do Windows).
    public static string DirTemporario => Path.Combine(Path.GetTempPath(), "PDVStoreSetup");

    // Subpasta da pasta temporária onde o zip do repositório é gravado antes da extração.
    public static string DirDownload => Path.Combine(DirTemporario, "download");

    // Pasta onde o SDK .NET é instalado (instalação "por usuário", sem precisar de admin).
    public static string DirDotNet => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "dotnet");

    // Pasta padrão das ferramentas globais do .NET instaladas para o usuário (ex.: dotnet-ef).
    public static string DirFerramentasDotNet => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".dotnet", "tools");

    public static string ExeDotNet => Path.Combine(DirDotNet, "dotnet.exe");

    public static string ExeDotNetEf => Path.Combine(DirFerramentasDotNet, "dotnet-ef.exe");

    public static string ConexaoPadrao(string nomeBanco)
        => $"Server=(localdb)\\{InstanciaLocalDb};Database={nomeBanco};Trusted_Connection=True;";

    // Monta as variáveis de ambiente que os comandos do .NET precisam quando o SDK foi
    // instalado pelo próprio instalador (fora do PATH do sistema): DOTNET_ROOT aponta para a
    // instalação por usuário e o PATH ganha as pastas de ferramentas e do SDK. Sem isso,
    // restore/publish/ef abrem falha "dotnet não encontrado" nos processos filhos.
    public static Dictionary<string, string> AmbienteDotNet()
    {
        var env = new Dictionary<string, string>
        {
            ["PATH"] = string.Join(";", new[]
            {
                DirFerramentasDotNet,
                DirDotNet,
                Environment.GetEnvironmentVariable("PATH") ?? ""
            })
        };
        if (File.Exists(ExeDotNet))
            env["DOTNET_ROOT"] = DirDotNet;
        return env;
    }

    // Descobre o executável do .NET, priorizando a instalação por usuário e caindo para o
    // 'where dotnet.exe' (que procura no PATH).
    public static string LocalizarDotnet()
    {
        if (File.Exists(ExeDotNet)) return ExeDotNet;
        return LocalizarNoPath("dotnet.exe");
    }

    // Descobre o executável do Git. É OPCIONAL: o caminho padrão é o zip do GitHub, e o Git só
    // entra como plano B quando o download falha. Por isso procuramos também os caminhos
    // típicos de instalação, que podem não estar no PATH do processo.
    public static string LocalizarGit()
    {
        var noPath = LocalizarNoPath("git.exe");
        if (!string.IsNullOrEmpty(noPath)) return noPath;

        var candidatos = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "git.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Git", "cmd", "git.exe")
        };
        foreach (var c in candidatos)
            if (File.Exists(c))
                return c;

        return "";
    }

    // Executa 'where <arquivo>' e devolve o primeiro caminho válido, ou "" se não existir.
    private static string LocalizarNoPath(string arquivo)
    {
        try
        {
            using var p = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("where", arquivo)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
            var linha = p?.StandardOutput.ReadLine()?.Trim();
            p?.WaitForExit(8000);
            if (!string.IsNullOrWhiteSpace(linha) && File.Exists(linha))
                return linha;
        }
        catch
        {
            // ignorado -> retorna "" (não instalado)
        }
        return "";
    }
}
