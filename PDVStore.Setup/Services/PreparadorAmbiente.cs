using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDVStore.Setup.Services;

/// <summary>
/// Executa as etapas de instalação e preparação do ambiente:
/// SDK .NET, LocalDB, dotnet-ef, PATH do usuário, instância LocalDB, migrações e publicação do aplicativo.
/// </summary>
public static class PreparadorAmbiente
{
    private const int PassosTotais = 7;

    /// <summary>
    /// Roda todas as etapas da preparação em sequência, reportando progresso.
    /// Etapas críticas lançam exceção; as demais são registradas como avisos e seguidas.
    /// </summary>
    public static async Task ExecutarAsync(
        SetupContext ctx,
        Action<int> onProgresso,
        Action<string> onLog,
        System.Threading.CancellationToken ct = default)
    {
        int passo = 0;

        try
        {
            passo = 1; onProgresso(passo); onLog("[1/7] Verificando/instalando o .NET SDK 8+...");
            if (!ctx.SdkDotNetInstalado)
                await InstalarSdkDotNetAsync(onLog, ct);
            else
                onLog("SDK .NET já instalado — ignorando instalação.");

            passo = 2; onProgresso(passo); onLog("[2/7] Verificando/instalando o SQL Server Express LocalDB...");
            if (!ctx.LocalDbInstalado)
                await InstalarLocalDbAsync(onLog, ct);
            else
                onLog("LocalDB já instalado — ignorando instalação.");

            passo = 3; onProgresso(passo); onLog("[3/7] Verificando/instalando a ferramenta dotnet-ef...");
            if (!ctx.DotNetEfInstalado)
                await InstalarDotNetEfAsync(onLog, ct);
            else
                onLog("dotnet-ef já instalado — ignorando instalação.");

            passo = 4; onProgresso(passo); onLog("[4/7] Configurando variáveis de ambiente (PATH)...");
            await ConfigurarPathAsync(onLog);

            passo = 5; onProgresso(passo); onLog("[5/7] Preparando a instância LocalDB (MSSQLLocalDB)...");
            await PrepararInstanciaLocalDbAsync(ctx, onLog, ct);

            passo = 6; onProgresso(passo); onLog("[6/7] Aplicando migrações do banco...");
            await AplicarMigracoesAsync(ctx, onLog, ct);

            passo = 7; onProgresso(passo); onLog("[7/7] Publicando o aplicativo...");
            await PublicarAplicativoAsync(ctx, onLog, ct);

            onLog("");
            onLog("Preparação concluída!");
        }
        catch (Exception ex)
        {
            onLog("");
            onLog("FALHA na etapa " + passo + " de " + PassosTotais + ": " + ex.Message);
            throw;
        }
    }

    private static async Task InstalarSdkDotNetAsync(Action<string> onLog, System.Threading.CancellationToken ct)
    {
        var pastaTemp = InstaladorInfo.DirTemporario;
        Directory.CreateDirectory(pastaTemp);
        var script = Path.Combine(pastaTemp, "dotnet-install.ps1");

        onLog("Baixando o script oficial de instalação do .NET (dot.net/v1/dotnet-install.ps1)...");
        await Downloader.DownloadAsync(
            InstaladorInfo.DotNetInstallScriptUrl,
            script,
            (recebido, total) => onLog($"   download .NET: {recebido / 1024d / 1024d:0.0} MB" +
                                        (total > 0 ? $" / {total / 1024d / 1024d:0.0} MB" : "")),
            ct);

        onLog($"Instalando o SDK .NET {InstaladorInfo.DotNetChannel} em \"{InstaladorInfo.DirDotNet}\" (sem privilégios de admin)...");
        var r = await ProcessUtil.RunAsync(
            "powershell.exe",
            new[]
            {
                "-NoProfile", "-ExecutionPolicy", "Bypass",
                "-File", script,
                "-Channel", InstaladorInfo.DotNetChannel,
                "-InstallDir", InstaladorInfo.DirDotNet,
                "-NoPath"
            },
            onLine: s => onLog("   " + s),
            ct: ct);

        if (r.ExitCode != 0)
            throw new Exception("Falha ao instalar o SDK .NET (código " + r.ExitCode + ").");
    }

    private static async Task InstalarLocalDbAsync(Action<string> onLog, System.Threading.CancellationToken ct)
    {
        var msi = Path.Combine(InstaladorInfo.DirTemporario, "SqlLocalDB.msi");

        onLog("Baixando o SQL Server Express LocalDB (SqlLocalDB.msi, en-US)...");
        await Downloader.DownloadAsync(
            InstaladorInfo.SqlLocalDbMsiUrl,
            msi,
            (recebido, total) => onLog($"   download LocalDB: {recebido / 1024d / 1024d:0.0} MB" +
                                        (total > 0 ? $" / {total / 1024d / 1024d:0.0} MB" : "")),
            ct);

        onLog("Instalando o LocalDB (uma confirmação do UAC pode ser solicitada pelo Windows)...");
        var r = await ProcessUtil.RunAsync(
            "msiexec.exe",
            new[] { "/i", msi, "/qn", "/norestart", "IACCEPTSQLEXPRESSLICENSETERMS=YES" },
            onLine: s => onLog("   " + s),
            ct: ct);

        // 0 = sucesso; 3010 = sucesso com reboot pendente (aceitável para LocalDB)
        if (r.ExitCode != 0 && r.ExitCode != 3010)
            throw new Exception("Falha ao instalar o LocalDB (código MSI " + r.ExitCode + ").");
    }

    private static async Task InstalarDotNetEfAsync(Action<string> onLog, System.Threading.CancellationToken ct)
    {
        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (string.IsNullOrEmpty(dotnet))
            throw new Exception("Sem dotnet para instalar a ferramenta global dotnet-ef.");

        onLog($"Instalando a ferramenta global dotnet-ef em \"{InstaladorInfo.DirFerramentasDotNet}\"...");
        var r = await ProcessUtil.RunAsync(
            dotnet,
            new[] { "tool", "install", "--global", "dotnet-ef", "--version", "9.0.*" },
            onLine: s => onLog("   " + s),
            ct: ct);

        if (r.ExitCode != 0)
            throw new Exception("Falha ao instalar o dotnet-ef (código " + r.ExitCode + ").");
    }

    private static async Task ConfigurarPathAsync(Action<string> onLog)
    {
        var dirs = new[] { InstaladorInfo.DirFerramentasDotNet, InstaladorInfo.DirDotNet }
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToArray();

        var adicionados = new System.Collections.Generic.List<string>();

        using (var hkcu = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Environment", writable: true))
        {
            if (hkcu == null)
            {
                onLog("AVISO: não foi possível abrir a chave HKCU\\Environment.");
                return;
            }

            var atual = hkcu.GetValue("Path", "", Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
            var partes = atual.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            foreach (var dir in dirs)
            {
                if (partes.Any(p => string.Equals(p.Replace("\"", "").TrimEnd('\\'), dir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                    continue;
                partes.Add(dir);
                adicionados.Add(dir);
            }

            if (adicionados.Count > 0)
            {
                hkcu.SetValue("Path", string.Join(";", partes), Microsoft.Win32.RegistryValueKind.ExpandString);
                onLog("PATH do usuário atualizado com: " + string.Join("; ", adicionados));
            }
            else
            {
                onLog("PATH do usuário já contém os diretórios necessários.");
            }
        }
    }

    private static async Task PrepararInstanciaLocalDbAsync(SetupContext ctx, Action<string> onLog, System.Threading.CancellationToken ct)
    {
        if (string.IsNullOrEmpty(ctx.CaminhoSqlLocalDb))
        {
            onLog("AVISO: SqlLocalDB.exe não localizado; a instância será preparada na primeira execução do app.");
            return;
        }

        onLog("Criando a instância LocalDB 'MSSQLLocalDB' (se ainda não existir)...");
        var criar = await ProcessUtil.RunAsync(ctx.CaminhoSqlLocalDb, new[] { "create", "MSSQLLocalDB" }, onLine: s => onLog("   " + s), ct: ct);
        if (criar.ExitCode != 0 && !criar.Output.Contains("already exists", StringComparison.OrdinalIgnoreCase)
            && !criar.Output.Contains("já existe", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Falha ao criar a instância LocalDB: " + criar.Output.Trim());

        onLog("Iniciando a instância 'MSSQLLocalDB'...");
        var iniciar = await ProcessUtil.RunAsync(ctx.CaminhoSqlLocalDb, new[] { "start", "MSSQLLocalDB" }, onLine: s => onLog("   " + s), ct: ct);
        if (iniciar.ExitCode != 0)
            throw new Exception("Falha ao iniciar a instância LocalDB: " + iniciar.Output.Trim());

        ctx.LocalDbInstalado = true;
    }

    private static async Task AplicarMigracoesAsync(SetupContext ctx, Action<string> onLog, System.Threading.CancellationToken ct)
    {
        ctx.CaminhoProjeto = ctx.LocalizarProjeto();
        if (string.IsNullOrEmpty(ctx.CaminhoProjeto))
        {
            onLog("AVISO: PDVStore.csproj não localizado; migrações serão aplicadas automaticamente na primeira execução do aplicativo.");
            return;
        }

        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (string.IsNullOrEmpty(dotnet) || !File.Exists(InstaladorInfo.ExeDotNetEf))
        {
            onLog("AVISO: dotnet/EF Tools indisponíveis; migrações serão aplicadas automaticamente pelo aplicativo na primeira execução.");
            return;
        }

        var pastaProjeto = Path.GetDirectoryName(ctx.CaminhoProjeto)!;
        onLog("Executando 'dotnet ef database update' no projeto PDVStore (pode levar alguns minutos na primeira vez)...");

        var env = new System.Collections.Generic.Dictionary<string, string>
        {
            ["PATH"] = InstaladorInfo.DirFerramentasDotNet + ";" + InstaladorInfo.DirDotNet + ";" + Environment.GetEnvironmentVariable("PATH")
        };
        if (File.Exists(InstaladorInfo.ExeDotNet))
            env["DOTNET_ROOT"] = InstaladorInfo.DirDotNet;

        var r = await ProcessUtil.RunAsync(
            InstaladorInfo.ExeDotNetEf,
            new[] { "database", "update", "--project", ctx.CaminhoProjeto, "--startup-project", ctx.CaminhoProjeto },
            pastaProjeto,
            s => onLog("   " + s),
            ct,
            env);

        if (r.ExitCode != 0)
        {
            onLog("AVISO: 'dotnet ef database update' falhou (" + r.ExitCode + "). O aplicativo aplicará as migrações automaticamente no primeiro acesso.");
        }
    }

    private static async Task PublicarAplicativoAsync(SetupContext ctx, Action<string> onLog, System.Threading.CancellationToken ct)
    {
        ctx.CaminhoProjeto = ctx.LocalizarProjeto();
        if (string.IsNullOrEmpty(ctx.CaminhoProjeto))
        {
            onLog("AVISO: PDVStore.csproj não localizado; a publicação para a pasta de instalação foi pulada.");
            return;
        }

        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (string.IsNullOrEmpty(dotnet))
        {
            onLog("AVISO: dotnet não localizado; a publicação foi pulada.");
            return;
        }

        var pastaProjeto = Path.GetDirectoryName(ctx.CaminhoProjeto)!;
        var dirPublicacao = ctx.DirInstalacao;
        Directory.CreateDirectory(dirPublicacao);

        onLog($"Publicando o aplicativo (Release) em \"{dirPublicacao}\"...");
        var r = await ProcessUtil.RunAsync(
            dotnet,
            new[] { "publish", ctx.CaminhoProjeto, "-c", "Release", "-o", dirPublicacao, "--nologo", "-v", "m" },
            pastaProjeto,
            s => onLog("   " + s),
            ct);

        if (r.ExitCode != 0)
        {
            onLog("AVISO: 'dotnet publish' falhou (" + r.ExitCode + "). O aplicativo pode ser executado a partir da pasta de build existente.");
            return;
        }

        ctx.CaminhoExeApp = Path.Combine(dirPublicacao, "PDVStore.exe");
        onLog("Aplicativo publicado. Executável: " + ctx.CaminhoExeApp);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var atalho = Path.Combine(desktop, "PDV Store.lnk");
        try
        {
            var script =
                "$ws = New-Object -ComObject WScript.Shell; " +
                $"$s = $ws.CreateShortcut('{atalho.Replace("'", "''")}'); " +
                $"$s.TargetPath = '{ctx.CaminhoExeApp.Replace("'", "''")}'; " +
                "$s.Save();";
            var sc = await ProcessUtil.RunAsync("powershell.exe", new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script });
            onLog(sc.ExitCode == 0 && File.Exists(atalho)
                ? "Atalho criado na área de trabalho: " + atalho
                : "AVISO: falha ao criar o atalho na área de trabalho.");
        }
        catch (Exception ex)
        {
            onLog("AVISO: falha ao criar o atalho na área de trabalho: " + ex.Message);
        }
    }
}