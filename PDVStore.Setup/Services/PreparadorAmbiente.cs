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
    // Orquestra as 7 etapas da preparação em sequência, atualizando a barra de progresso (1 a 7)
    // e emitindo o log de cada ação. Etapas já satisfeitas são puladas com aviso no log.
    // POR QUE em um só fluxo: garante a ordem correta das dependências — primeiro o SDK e o
    // LocalDB, depois a ferramenta de migrações, o PATH, a instância LocalDB, as migrações e,
    // por fim, a publicação do aplicativo. Exceções indicam a etapa que falhou e são relançadas.
    // Dependências: SetupContext (estado), InstaladorInfo (caminhos), Downloader (downloads) e
    // ProcessUtil (processos externos); chama os métodos privados de instalação desta classe.
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

    // Baixa o script oficial dotnet-install.ps1 e instala o SDK .NET 8 na pasta do usuário,
    // sem exigir privilégios de administrador.
    // POR QUE o modo "sem admin": o SDK é instalado por usuário (via -InstallDir e -NoPath),
    // evitando UAC e mantendo o instalador simples; se o processo falhar, lança exceção.
    // Dependências: InstaladorInfo (URL, canais e caminhos), Downloader (download com progresso)
    // e ProcessUtil (executa o powershell.exe com o script baixado).
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

    // Baixa o SQL Server Express LocalDB e o instala de forma silenciosa via MSI (msiexec /qn).
    // POR QUE aceitar o código 3010 como sucesso: 3010 significa "instalado com reinicialização
    // pendente", comum no LocalDB, e não deve ser tratado como erro de instalação.
    // Dependências: InstaladorInfo.SqlLocalDbMsiUrl, Downloader para baixar o .msi e ProcessUtil
    // para executar o msiexec; a instalação pode exibir um pedido de confirmação do UAC.
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

    // Instala a ferramenta global dotnet-ef (EF Core Tools), requisito para aplicar migrações
    // de banco pelo instalador.
    // POR QUE exigir o dotnet antes: sem o SDK localizado não há como executar 'dotnet tool
    // install'; por isso o método lança exceção orientando qual o problema real.
    // Dependências: InstaladorInfo.LocalizarDotnet, ProcessUtil para rodar a instalação e o
    // caminho DirFerramentasDotNet onde a ferramenta global será gravada.
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

    // Adiciona os diretórios do .NET (ferramentas globais e SDK por usuário) ao PATH do usuário,
    // gravando na chave HKCU\Environment do registro do Windows.
    // POR QUE mexe só no HKCU: alterar o PATH do sistema exigiria elevação/UAC; o PATH do
    // usuário é suficiente e menos invasivo. O valor expandido mantém referências como %VAR%.
    // Regras: evita duplicar diretórios que já existem (compara ignorando maiúsculas e a barra
    // final) e apenas anexa os que faltam.
    // Dependências: Microsoft.Win32.Registry (HKCU\Environment) e InstaladorInfo.DirDotNet e
    // DirFerramentasDotNet.
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

    // Garante que a instância LocalDB 'MSSQLLocalDB' exista e esteja em execução, criando e
    // iniciando via SqlLocalDB.exe quando necessário.
    // POR QUE aceitar "already exists"/"já existe": criar uma instância já existente retorna
    // erro amigável; esse aviso é esperado num reboot posterior e não deve quebrar o fluxo.
    // Dependências: ctx.CaminhoSqlLocalDb (preenchido pelo VerificadorRequisitos) e ProcessUtil;
    // ao final marca ctx.LocalDbInstalado = true para os relatórios.
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

    // Aplica as migrações do EF Core executando 'dotnet ef database update' no projeto PDVStore,
    // criando o banco PDV_StoreDB caso ainda não exista.
    // POR QUE tratar falha como AVISO: se o projeto ou o dotnet-ef não forem encontrados, ou se
    // a execução falhar, o próprio aplicativo aplica as migrações no primeiro acesso — então o
    // instalador não deve abortar a instalação inteira por causa disso.
    // Dependências: ctx.LocalizarProjeto(), InstaladorInfo (dotnet, dotnet-ef, PATH) e
    // ProcessUtil com variáveis de ambiente extras (PATH e DOTNET_ROOT).
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

    // Publica o aplicativo PDVStore em modo Release para o diretório de instalação escolhido e,
    // quando possível, cria um atalho na área de trabalho.
    // POR QUE falha vira AVISO: a publicação pode falhar por problemas de build/NuGet; o aplicativo
    // continua executável pela pasta de build existente, então não vale bloquear a instalação.
    // Dependências: ctx.LocalizarProjeto(), InstaladorInfo.LocalizarDotnet, ProcessUtil (dotnet
    // publish e powershell para criar o .lnk via WScript.Shell) e System.IO para o diretório.
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