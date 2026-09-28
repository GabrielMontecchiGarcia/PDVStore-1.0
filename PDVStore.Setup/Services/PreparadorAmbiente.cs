using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PDVStore.Setup.Services;

public enum StatusEtapa
{
    Pendente,
    EmAndamento,
    Concluido,
    Ignorado,
    Falhou
}

// Uma etapa visível da instalação. A tela monta a lista uma única vez a partir de MontarEtapas
// e só atualiza o status/detalhe, o que evita recriar os itens da lista a cada progresso.
public sealed class Etapa
{
    public int Numero { get; init; }
    public string Titulo { get; init; } = "";
    public string Detalhe { get; set; } = "";
    public StatusEtapa Status { get; set; } = StatusEtapa.Pendente;

    public static Etapa Criar(int numero, string titulo) => new() { Numero = numero, Titulo = titulo };
}

/// <summary>
/// Executa a instalação completa: baixa o código do GitHub, instala o que faltar, recria a
/// instância do banco, restaura as dependências, aplica as migrações e publica o aplicativo.
/// </summary>
public static class PreparadorAmbiente
{
    public const int PassosTotais = 10;

    // As 10 etapas, na ordem em que precisam acontecer. A ordem impõe as dependências:
    // o código precisa existir antes do restore; o nome do banco precisa estar no
    // ConnectionHelper antes das migrações (o EF lê a connection string de lá); e as migrações
    // precisam rodar antes do publish, para que o banco já esteja pronto quando o app abrir.
    public static List<Etapa> MontarEtapas() => new()
    {
        Etapa.Criar(1,  "Baixar o código-fonte do GitHub"),
        Etapa.Criar(2,  "Verificar/instalar o .NET SDK 8+"),
        Etapa.Criar(3,  "Verificar/instalar o SQL Server Express LocalDB"),
        Etapa.Criar(4,  "Verificar/instalar a ferramenta dotnet-ef"),
        Etapa.Criar(5,  "Configurar as variáveis de ambiente (PATH)"),
        Etapa.Criar(6,  "Recriar a instância do banco de dados"),
        Etapa.Criar(7,  "Aplicar o nome do banco no código"),
        Etapa.Criar(8,  "Restaurar as dependências (NuGet)"),
        Etapa.Criar(9,  "Aplicar as migrações do banco"),
        Etapa.Criar(10, "Publicar o aplicativo e criar o atalho"),
    };

    // Roda todas as etapas em sequência. Cada mudança de status é reportada em onEtapa (para a
    // lista da tela) e em onLog (para o log detalhado). Etapas obrigatórias que falham marcam a
    // etapa como Falhou e relançam a exceção; etapas tolerantes marcam Ignorado e seguem.
    public static async Task ExecutarAsync(
        SetupContext ctx,
        Action<Etapa> onEtapa,
        Action<string> onLog,
        CancellationToken ct = default)
    {
        var etapas = MontarEtapas();
        var atual = -1;

        Etapa Esta() => etapas[atual - 1];

        void Iniciar(int numero, string detalhe)
        {
            atual = numero;
            Esta().Status = StatusEtapa.EmAndamento;
            Esta().Detalhe = detalhe;
            onEtapa(Esta());
            onLog($"[{numero}/{PassosTotais}] {Esta().Titulo}");
        }

        void Concluir(string detalhe)
        {
            Esta().Status = StatusEtapa.Concluido;
            Esta().Detalhe = detalhe;
            onEtapa(Esta());
            if (!string.IsNullOrWhiteSpace(detalhe))
                onLog("    " + detalhe);
        }

        void Ignorar(string detalhe)
        {
            Esta().Status = StatusEtapa.Ignorado;
            Esta().Detalhe = detalhe;
            onEtapa(Esta());
            onLog("    " + detalhe);
        }

        try
        {
            // ---- 1. Código-fonte ----
            Iniciar(1, "Iniciando o download...");
            await FonteGitHub.ObterAsync(ctx, s => onLog("    " + s), ct);
            Concluir($"Fonte em \"{ctx.PastaFonte}\" (branch {ctx.Branch})");

            // ---- 2. SDK .NET ----
            Iniciar(2, "Procurando um SDK .NET 8+...");
            if (!ctx.SdkDotNetInstalado)
                await InstalarSdkDotNetAsync(onLog, ct);
            Concluir("SDK .NET 8+ disponível: " + InstaladorInfo.LocalizarDotnet());

            // ---- 3. LocalDB ----
            Iniciar(3, "Procurando o SqlLocalDB.exe...");
            if (!ctx.LocalDbInstalado)
                await InstalarLocalDbAsync(onLog, ct);
            if (string.IsNullOrEmpty(ctx.CaminhoSqlLocalDb))
                ctx.CaminhoSqlLocalDb = LocalizarSqlLocalDb();
            Concluir("LocalDB disponível: " + ctx.CaminhoSqlLocalDb);

            // ---- 4. dotnet-ef ----
            Iniciar(4, "Consultando as ferramentas globais...");
            if (!ctx.DotNetEfInstalado)
                await InstalarDotNetEfAsync(onLog, ct);
            Concluir("Ferramenta dotnet-ef disponível.");

            // ---- 5. PATH ----
            Iniciar(5, "Lendo HKCU\\Environment...");
            ConfigurarPathAsync(onLog);
            Concluir("PATH do usuário conferido.");

            // ---- 6. Instância do banco ----
            Iniciar(6, ctx.RecriarBanco
                ? "A instância será recriada do zero (os dados existentes serão apagados)."
                : "A instância existente será preservada.");
            await RecriarInstanciaLocalDbAsync(ctx, onLog, ct);
            Concluir($"Instância (localdb)\\{InstaladorInfo.InstanciaLocalDb} pronta.");

            // ---- 7. Nome do banco no código ----
            Iniciar(7, $"Ajustando ConnectionHelper.cs para o banco \"{ctx.NomeBanco}\"...");
            if (FonteGitHub.AplicarNomeBanco(ctx.PastaFonte, ctx.NomeBanco, out var mensagem))
                Concluir(mensagem);
            else
                Ignorar(mensagem + " O nome padrão do repositório será mantido.");

            // ---- 8. Restore ----
            Iniciar(8, "Baixando os pacotes NuGet...");
            await RestaurarPacotesAsync(ctx, onLog, ct);
            Concluir("Dependências restauradas.");

            // ---- 9. Migrações ----
            Iniciar(9, "Criando o banco e as tabelas...");
            var avisoMigracoes = await AplicarMigracoesAsync(ctx, onLog, ct);
            if (string.IsNullOrEmpty(avisoMigracoes))
                Concluir("Banco migrado: " + ctx.NomeBanco);
            else
                Ignorar(avisoMigracoes);

            // ---- 10. Publicação ----
            Iniciar(10, "Publicando em Release...");
            await PublicarAplicativoAsync(ctx, onLog, ct);
            Concluir("Aplicativo publicado em: " + ctx.DirInstalacao);

            onLog("");
            onLog("Instalação concluída!");
        }
        catch (Exception ex)
        {
            if (atual > 0)
            {
                Esta().Status = StatusEtapa.Falhou;
                Esta().Detalhe = ex.Message;
                onEtapa(Esta());
            }
            onLog("");
            onLog($"FALHA na etapa {atual} de {PassosTotais}: {ex.Message}");
            throw;
        }
    }

    // Baixa o script oficial dotnet-install.ps1 e instala o SDK .NET 8 na pasta do usuário,
    // sem exigir privilégios de administrador.
    private static async Task InstalarSdkDotNetAsync(Action<string> onLog, CancellationToken ct)
    {
        Directory.CreateDirectory(InstaladorInfo.DirTemporario);
        var script = Path.Combine(InstaladorInfo.DirTemporario, "dotnet-install.ps1");

        onLog("  Baixando o script oficial de instalação do .NET (dot.net/v1/dotnet-install.ps1)...");
        await Downloader.BaixarAsync(
            InstaladorInfo.DotNetInstallScriptUrl,
            script,
            (recebido, total) => onLog($"     download .NET: {recebido / 1024d / 1024d:0.0} MB" +
                                       (total > 0 ? $" / {total / 1024d / 1024d:0.0} MB" : "")),
            ct);

        onLog($"  Instalando o SDK .NET {InstaladorInfo.DotNetChannel} em \"{InstaladorInfo.DirDotNet}\" (sem privilégios de admin)...");
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
            onLine: s => onLog("     " + s),
            ct: ct);

        if (r.ExitCode != 0)
            throw new Exception("Falha ao instalar o SDK .NET (código " + r.ExitCode + ").");
    }

    // Baixa o SQL Server Express LocalDB e o instala de forma silenciosa via MSI (msiexec /qn).
    // POR QUE aceitar o código 3010 como sucesso: 3010 significa "instalado com reinicialização
    // pendente", comum no LocalDB, e não deve ser tratado como erro de instalação.
    private static async Task InstalarLocalDbAsync(Action<string> onLog, CancellationToken ct)
    {
        var msi = Path.Combine(InstaladorInfo.DirTemporario, "SqlLocalDB.msi");

        onLog("  Baixando o SQL Server Express LocalDB (SqlLocalDB.msi, en-US)...");
        await Downloader.BaixarAsync(
            InstaladorInfo.SqlLocalDbMsiUrl,
            msi,
            (recebido, total) => onLog($"     download LocalDB: {recebido / 1024d / 1024d:0.0} MB" +
                                       (total > 0 ? $" / {total / 1024d / 1024d:0.0} MB" : "")),
            ct);

        onLog("  Instalando o LocalDB (uma confirmação do UAC pode ser solicitada pelo Windows)...");
        var r = await ProcessUtil.RunAsync(
            "msiexec.exe",
            new[] { "/i", msi, "/qn", "/norestart", "IACCEPTSQLEXPRESSLICENSETERMS=YES" },
            onLine: s => onLog("     " + s),
            ct: ct);

        if (r.ExitCode != 0 && r.ExitCode != 3010)
            throw new Exception("Falha ao instalar o LocalDB (código MSI " + r.ExitCode + ").");
    }

    // Instala a ferramenta global dotnet-ef (EF Core Tools), requisito para aplicar as
    // migrações do banco.
    private static async Task InstalarDotNetEfAsync(Action<string> onLog, CancellationToken ct)
    {
        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (string.IsNullOrEmpty(dotnet))
            throw new Exception("Sem dotnet para instalar a ferramenta global dotnet-ef.");

        onLog($"  Instalando a ferramenta global dotnet-ef em \"{InstaladorInfo.DirFerramentasDotNet}\"...");
        var r = await ProcessUtil.RunAsync(
            dotnet,
            new[] { "tool", "install", "--global", "dotnet-ef", "--version", "9.0.*" },
            onLine: s => onLog("     " + s),
            ct: ct);

        if (r.ExitCode != 0)
            throw new Exception("Falha ao instalar o dotnet-ef (código " + r.ExitCode + ").");
    }

    // Adiciona os diretórios do .NET (ferramentas globais e SDK por usuário) ao PATH do usuário,
    // gravando na chave HKCU\Environment do registro do Windows.
    // POR QUE mexer só no HKCU: alterar o PATH do sistema exigiria elevação/UAC; o PATH do
    // usuário é suficiente e menos invasivo. O valor expandido mantém referências como %VAR%.
    // Evita duplicar diretórios que já existem (compara ignorando maiúsculas e a barra final).
    private static void ConfigurarPathAsync(Action<string> onLog)
    {
        var dirs = new[] { InstaladorInfo.DirFerramentasDotNet, InstaladorInfo.DirDotNet };

        using var hkcu = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Environment", writable: true);
        if (hkcu == null)
        {
            onLog("  AVISO: não foi possível abrir a chave HKCU\\Environment.");
            return;
        }

        var atual = hkcu.GetValue("Path", "", Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
        var partes = atual.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        var adicionados = new List<string>();
        foreach (var dir in dirs)
        {
            if (partes.Any(p => string.Equals(p.Replace("\"", "").TrimEnd('\\'), dir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                continue;
            partes.Add(dir);
            adicionados.Add(dir);
        }

        if (adicionados.Count == 0)
        {
            onLog("  O PATH do usuário já contém os diretórios necessários.");
            return;
        }

        hkcu.SetValue("Path", string.Join(";", partes), Microsoft.Win32.RegistryValueKind.ExpandString);
        onLog("  PATH do usuário atualizado com: " + string.Join("; ", adicionados));
    }

    // Garante que a instância LocalDB exista e esteja rodando, recriando-a do zero quando o
    // usuário deixou "Recriar banco do zero" marcado (o padrão).
    // A recriação é feita nesta ordem: parar -> excluir -> criar -> iniciar. Sem a parada antes
    // da exclusão, o LocalDB recusa o 'delete' da instância que estiver em uso.
    //
    // IMPORTANTE: SqlLocalDB.exe devolve o código de saída 0 mesmo quando o comando falha
    // ("The specified LocalDB instance does not exist." também sai com 0). Por isso nenhuma
    // checagem aqui usa ExitCode: o sucesso é confirmado por InstanciaExisteAsync, que consulta
    // 'sqllocaldb info <nome>' depois de cada comando, e por FalhouNoLocalDb, que procura a
    // mensagem de erro no texto.
    private static async Task RecriarInstanciaLocalDbAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        var exe = ctx.CaminhoSqlLocalDb;
        if (string.IsNullOrEmpty(exe))
            throw new Exception("SqlLocalDB.exe não localizado; o LocalDB precisa estar instalado.");

        var nome = InstaladorInfo.InstanciaLocalDb;
        ctx.InstanciaExiste = await InstanciaExisteAsync(exe, nome, ct);

        if (ctx.InstanciaExiste && ctx.RecriarBanco)
        {
            onLog($"  A instância \"{nome}\" já existe e será recriada. TODOS os dados do banco atual serão perdidos.");

            await EncerrarAplicativoEmExecucao(onLog);

            // Parar antes de excluir é obrigatório. Uma falha aqui significa que a instância já
            // estava parada, o que é aceitável.
            await ProcessUtil.RunAsync(exe, new[] { "stop", nome }, onLine: s => onLog("     " + s), ct: ct);

            var excluir = await ProcessUtil.RunAsync(exe, new[] { "delete", nome }, onLine: s => onLog("     " + s), ct: ct);
            if (FalhouNoLocalDb(excluir))
                throw new Exception("Não foi possível excluir a instância LocalDB: " + Resumir(excluir.Output));

            // Confirma pelo estado real, e não pelo código de saída.
            if (await InstanciaExisteAsync(exe, nome, ct))
                throw new Exception(
                    $"O comando delete foi executado, mas a instância \"{nome}\" continua registrada. " +
                    "Verifique se ela está em uso por outro aplicativo.");

            ctx.InstanciaExiste = false;
            onLog($"  Instância \"{nome}\" excluída.");

            // A exclusão da instância não leva junto os arquivos do banco; precisam sair antes
            // de a etapa 9 tentar o CREATE DATABASE.
            RemoverArquivosBancoOrfaos(ctx.NomeBanco, onLog);
        }
        else if (ctx.InstanciaExiste)
        {
            onLog($"  A instância \"{nome}\" existe e será reaproveitada (recriação desmarcada).");
        }

        if (!ctx.InstanciaExiste)
        {
            onLog($"  Criando a instância \"{nome}\"...");
            var criar = await ProcessUtil.RunAsync(exe, new[] { "create", nome }, onLine: s => onLog("     " + s), ct: ct);
            if (FalhouNoLocalDb(criar) || !await InstanciaExisteAsync(exe, nome, ct))
                throw new Exception("Não foi possível criar a instância LocalDB: " + Resumir(criar.Output));
        }

        onLog($"  Iniciando a instância \"{nome}\"...");
        var iniciar = await ProcessUtil.RunAsync(exe, new[] { "start", nome }, onLine: s => onLog("     " + s), ct: ct);
        if (FalhouNoLocalDb(iniciar))
            throw new Exception("Não foi possível iniciar a instância LocalDB: " + Resumir(iniciar.Output));
    }

    // Verifica se a instância existe consultando 'sqllocaldb info <nome>', que é a única
    // forma confiável. POR QUE não usar 'sqllocaldb info' sem argumento: esse comando lista
    // SEMPRE a instância automática "MSSQLLocalDB", mesmo quando ela ainda não foi criada, então
    // a busca por linha dá falso positivo e o instalador tentaria stop/delete num nome que não
    // existe. A forma detalhada responde de forma explícita:
    //   - existe  -> bloco com "Auto-create:", "State:", "Instance pipe name";
    //   - não existe -> 'The automatic instance "mssqllocaldb" is not created.' (automática)
    //                 ou 'The specified LocalDB instance does not exist.' (instância nomeada).
    private static async Task<bool> InstanciaExisteAsync(string exe, string nome, CancellationToken ct)
    {
        var r = await ProcessUtil.RunAsync(exe, new[] { "info", nome }, ct: ct);
        var saida = r.Output;

        if (saida.Contains("is not created", StringComparison.OrdinalIgnoreCase)
            || saida.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            return false;

        return saida.Contains("Auto-create", StringComparison.OrdinalIgnoreCase)
            || saida.Contains("Instance pipe name", StringComparison.OrdinalIgnoreCase);
    }

    // Remove os arquivos de dados órfãos do banco na pasta de perfil do usuário.
    //
    // POR QUE necessário: o 'sqllocaldb delete' NÃO apaga os arquivos .mdf/.ldf dos bancos de
    // dados do usuário — ele só remove o registro da instância. Depois de recriar a instância,
    // o 'CREATE DATABASE' da etapa 9 falha com:
    //     Cannot create file 'C:\Users\<usuário>\<banco>.mdf' because it already exists.
    // Como o LocalDB guarda os bancos do usuário diretamente no perfil (sem pasta própria),
    // o instalador precisa remover esses arquivos para que a recriação do zero funcione de fato.
    private static void RemoverArquivosBancoOrfaos(string nomeBanco, Action<string> onLog)
    {
        var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(perfil))
            return;

        var removidos = 0;
        foreach (var nome in new[] { nomeBanco + ".mdf", nomeBanco + "_log.ldf", nomeBanco + ".ldf" })
        {
            var caminho = Path.Combine(perfil, nome);
            if (!File.Exists(caminho))
                continue;

            try
            {
                File.Delete(caminho);
                onLog("     arquivo de dado antigo removido: " + caminho);
                removidos++;
            }
            catch (Exception ex)
            {
                onLog($"     AVISO: não foi possível remover \"{caminho}\" ({ex.Message}). " +
                      "A criação do banco pode falhar.");
            }
        }

        if (removidos == 0)
            onLog("     nenhum arquivo de dado antigo do banco foi encontrado.");
    }

    // Detecta a mensagem de erro do SqlLocalDB.exe no texto de saída. As mensagens são em inglês
    // mesmo em Windows localizado, e as de sucesso nunca contêm estas palavras.
    private static bool FalhouNoLocalDb((int ExitCode, string Output) r)
        => r.Output.Contains("failed", StringComparison.OrdinalIgnoreCase)
        || r.Output.Contains("error", StringComparison.OrdinalIgnoreCase);

    // Condensa a saída do SqlLocalDB em uma linha legível para a mensagem de erro, já que ela
    // vem com as quebras de linha do console.
    private static string Resumir(string saida)
    {
        var texto = string.Join(" ", saida.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim()));
        return texto.Length <= 300 ? texto : texto[..300] + "...";
    }

    // Fecha o PDVStore caso ele esteja aberto, porque a conexão com o banco impede a exclusão
    // da instância. Tenta primeiro um fechamento normal da janela e, se o processo continuar
    // vivo, encerra à força — sem isso a etapa 6 falharia com o aplicativo aberto.
    private static async Task EncerrarAplicativoEmExecucao(Action<string> onLog)
    {
        Process[] processos;
        try
        {
            processos = Process.GetProcessesByName("PDVStore");
        }
        catch
        {
            return;
        }

        if (processos.Length == 0)
            return;

        onLog("  AVISO: o PDVStore está em execução e será fechado para liberar o banco.");
        var ids = processos.Select(p => p.Id).Where(id => id != Environment.ProcessId).ToArray();
        foreach (var p in processos)
        {
            try { p.CloseMainWindow(); } catch { }
        }

        await Task.Delay(1500);

        foreach (var p in processos)
        {
            try
            {
                if (!p.HasExited)
                    p.Kill();
                p.WaitForExit(5000);
            }
            catch { }
        }

        if (ids.Length > 0)
            onLog("  Processo(s) encerrado(s): PID " + string.Join(", ", ids.Select(i => i.ToString())));
    }

    // Restaura os pacotes NuGet do projeto baixado. Etapa obrigatória: sem ela o 'dotnet ef' e
    // o 'dotnet publish' falham ao tentar compilar sem as dependências declaradas no .csproj.
    private static async Task RestaurarPacotesAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        var dotnet = ExigirDotnet();
        var projeto = ExigirProjeto(ctx);
        var pastaProjeto = Path.GetDirectoryName(projeto)!;

        var r = await ProcessUtil.RunAsync(
            dotnet,
            new[] { "restore", projeto, "--nologo" },
            pastaProjeto,
            s => onLog("     " + s),
            ct,
            InstaladorInfo.AmbienteDotNet());

        if (r.ExitCode != 0)
            throw new Exception("Falha em 'dotnet restore' (código " + r.ExitCode + ").");
    }

    // Aplica as migrações do EF Core, criando o banco caso ainda não exista.
    // Não aborta a instalação quando falha: o próprio aplicativo aplica as migrações pendentes
    // no primeiro acesso (frmSplash), então o sistema continua utilizável mesmo sem o dotnet-ef.
    // Devolve "" em caso de sucesso ou o motivo da falha, para que a tela marque a etapa como
    // "Ignorado" — e não "OK" — quando algo deu errado.
    private static async Task<string> AplicarMigracoesAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        var projeto = ExigirProjeto(ctx);
        var pastaProjeto = Path.GetDirectoryName(projeto)!;

        if (!File.Exists(InstaladorInfo.ExeDotNetEf))
        {
            const string aviso = "dotnet-ef indisponível; as migrações serão aplicadas pelo aplicativo no primeiro acesso.";
            onLog("  AVISO: " + aviso);
            return aviso;
        }

        onLog("  Executando 'dotnet ef database update' (pode levar alguns minutos na primeira vez)...");

        var r = await ProcessUtil.RunAsync(
            InstaladorInfo.ExeDotNetEf,
            new[] { "database", "update", "--project", projeto, "--startup-project", projeto },
            pastaProjeto,
            s => onLog("     " + s),
            ct,
            InstaladorInfo.AmbienteDotNet());

        if (r.ExitCode == 0)
            return "";

        const string falha = "'dotnet ef database update' falhou; o aplicativo aplicará as migrações no primeiro acesso.";
        onLog("  AVISO: " + falha + " (código " + r.ExitCode + ")");
        return falha;
    }

    // Publica o aplicativo em Release e cria um atalho na área de trabalho. Esta é a etapa que
    // produz o resultado final, então uma falha aqui é FATAL e aborta a instalação: o código
    // acabou de ser baixado do GitHub, não existe build anterior para o usuário recorrer, e um
    // "instalado" sem executável seria enganoso. Os erros de compilação do MSBuild já foram
    // repassados linha a linha para o log pelo ProcessUtil.
    //
    // POR QUE publicar numa pasta de preparo e só depois copiar para a pasta de instalação:
    // o SDK do .NET inclui "$(OutputPath)**" na lista DefaultItemExcludes. Com o padrão do
    // instalador (código em "<instalação>\source" e saída em "<instalação>"), a pasta de saída
    // é um ANCESTOR da pasta do projeto, então o glob "<instalação>\**" apaga todos os itens
    // Compile do próprio projeto. O resultado é o publish falhar com
    // "CS5001: Programa não contém um método Main estático adequado" — sem nenhum erro de
    // código, apenas porque a saída está acima da fonte. Publicar em uma pasta temporária
    // (fora da árvore do projeto) evita isso; depois os arquivos são copiados para o destino.
    private static async Task PublicarAplicativoAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        var dotnet = ExigirDotnet();
        var projeto = ExigirProjeto(ctx);
        var pastaProjeto = Path.GetDirectoryName(projeto)!;

        var Preparo = Path.Combine(InstaladorInfo.DirTemporario, "publish");
        ApagarPasta(Preparo);
        Directory.CreateDirectory(Preparo);

        Directory.CreateDirectory(ctx.DirInstalacao);

        onLog($"  Publicando (Release) em \"{Preparo}\"...");
        var r = await ProcessUtil.RunAsync(
            dotnet,
            new[] { "publish", projeto, "-c", "Release", "-o", Preparo, "--nologo", "-v", "m" },
            pastaProjeto,
            s => onLog("     " + s),
            ct,
            InstaladorInfo.AmbienteDotNet());

        if (r.ExitCode != 0)
            throw new Exception(
                "'dotnet publish' falhou (código " + r.ExitCode + "). Veja as mensagens de erro " +
                "de compilação no log acima. É comum o repositório no GitHub estar com um erro de " +
                "build; nesse caso, abra uma issue em " + InstaladorInfo.RepoPagina + ".");

        var exePreparo = Path.Combine(Preparo, "PDVStore.exe");
        if (!File.Exists(exePreparo))
            throw new Exception(
                "'dotnet publish' terminou sem erro, mas o executável não foi encontrado em \"" +
                exePreparo + "\".");

        var total = CopiarConteudo(Preparo, ctx.DirInstalacao);
        onLog($"  {total} arquivos copiados para \"{ctx.DirInstalacao}\".");

        if (!File.Exists(ctx.CaminhoExe))
            throw new Exception(
                "A cópia para a pasta de instalação terminou, mas o executável não foi encontrado em \"" +
                ctx.CaminhoExe + "\".");

        ctx.CaminhoExeApp = ctx.CaminhoExe;
        onLog("  Executável: " + ctx.CaminhoExeApp);

        await CriarAtalhoAsync(ctx, onLog);
    }

    // Copia recursivamente o conteúdo de uma pasta para outra, devolvendo a contagem de arquivos.
    private static int CopiarConteudo(string origem, string destino)
    {
        var total = 0;
        foreach (var arquivo in Directory.EnumerateFiles(origem, "*", SearchOption.AllDirectories))
        {
            var relativo = Path.GetRelativePath(origem, arquivo);
            var alvo = Path.Combine(destino, relativo);
            Directory.CreateDirectory(Path.GetDirectoryName(alvo)!);
            File.Copy(arquivo, alvo, overwrite: true);
            total++;
        }
        return total;
    }

    // Remove uma pasta ignorando arquivos em uso: a pasta de preparo é descartada e o
    // instalador continua mesmo assim, já que o conteúdo já foi copiado para o destino.
    private static void ApagarPasta(string pasta)
    {
        if (!Directory.Exists(pasta))
            return;
        try
        {
            Directory.Delete(pasta, recursive: true);
        }
        catch
        {
            // Sem problema: o próximo publish recria/sobrescreve por cima.
        }
    }

    // Cria "PDV Store.lnk" na área de trabalho via WScript.Shell. O caminho do executável é
    // escapado com Replace("'", "''") porque o script é montado em PowerShell entre aspas
    // simples, onde uma aspa interna encerraria a string.
    private static async Task CriarAtalhoAsync(SetupContext ctx, Action<string> onLog)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var atalho = Path.Combine(desktop, "PDV Store.lnk");

        try
        {
            var script =
                "$ws = New-Object -ComObject WScript.Shell; " +
                $"$s = $ws.CreateShortcut('{atalho.Replace("'", "''")}'); " +
                $"$s.TargetPath = '{ctx.CaminhoExe.Replace("'", "''")}'; " +
                "$s.Save();";

            var r = await ProcessUtil.RunAsync(
                "powershell.exe",
                new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script });

            onLog(r.ExitCode == 0 && File.Exists(atalho)
                ? "  Atalho criado na área de trabalho: " + atalho
                : "  AVISO: falha ao criar o atalho na área de trabalho.");
        }
        catch (Exception ex)
        {
            onLog("  AVISO: falha ao criar o atalho na área de trabalho: " + ex.Message);
        }
    }

    private static string ExigirDotnet()
    {
        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (string.IsNullOrEmpty(dotnet))
            throw new Exception("dotnet não localizado após a etapa de instalação do SDK.");
        return dotnet;
    }

    private static string ExigirProjeto(SetupContext ctx)
    {
        var projeto = ctx.LocalizarProjeto();
        if (string.IsNullOrEmpty(projeto))
            throw new Exception("PDVStore.csproj não localizado no código baixado do GitHub.");
        return projeto;
    }

    // Reaproveita a busca de caminho do verificador: se o SqlLocalDB.exe só foi encontrado
    // depois da instalação do MSI, é preciso localizá-lo de novo.
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
            catch { }
        }
        return "";
    }
}
