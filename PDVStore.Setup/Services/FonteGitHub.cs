using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PDVStore.Setup.Services;

/// <summary>
/// Obtém o código-fonte do PDVStore a partir do GitHub: baixa o zip do repositório (caminho
/// padrão, que não exige Git instalado) e, se o download falhar, usa 'git clone' como plano B.
/// Também localiza o .csproj e aplica o nome do banco escolhido no ConnectionHelper.
/// </summary>
public static class FonteGitHub
{
    // Descobre a branch padrão do repositório.
    // Ordem de tentativa: (1) API do GitHub, que responde com "default_branch"; (2) sondar as
    // branches master e main pelo ZIP, para funcionar mesmo sem acesso à API (a API exige token
    // em alguns cenários e tem limite de requisições); (3) cair em "master".
    public static async Task<string> ResolverBranchAsync(Action<string> onLog, CancellationToken ct)
    {
        try
        {
            using var client = Downloader.CriarCliente();
            client.Timeout = TimeSpan.FromSeconds(30);
            Downloader.ConfigurarCabecalho(client);

            using var r = await client.GetAsync(InstaladorInfo.RepoApi, ct);
            if (r.IsSuccessStatusCode)
            {
                var json = await r.Content.ReadAsStringAsync(ct);
                var m = Regex.Match(json, "\"default_branch\"\\s*:\\s*\"(?<b>[^\"]+)\"");
                if (m.Success)
                {
                    var branch = m.Groups["b"].Value;
                    onLog("Branch padrão detectada pela API do GitHub: " + branch);
                    return branch;
                }
            }
            onLog("API do GitHub indisponível; deduzindo a branch pelo ZIP...");
        }
        catch (Exception ex)
        {
            onLog("API do GitHub indisponível (" + ex.Message + "); deduzindo a branch pelo ZIP...");
        }

        foreach (var branch in new[] { InstaladorInfo.BranchPadrao, InstaladorInfo.BranchAlternativa })
        {
            if (await Downloader.UrlExisteAsync(InstaladorInfo.ZipUrl(branch), ct))
            {
                onLog("Branch encontrada por sondagem: " + branch);
                return branch;
            }
        }

        onLog("Nenhuma branch pôde ser confirmada; usando \"" + InstaladorInfo.BranchPadrao + "\".");
        return InstaladorInfo.BranchPadrao;
    }

    // Baixa e extrai o código do repositório, deixando a pasta de trabalho limpa e apontando
    // ctx.PastaFonte / ctx.CaminhoProjeto para a raiz real do projeto.
    // Sempre parte de uma cópia nova: a pasta anterior é apagada para que uma execução
    // atualize o sistema para a versão mais recente do GitHub em vez de reaproveitar código velho.
    public static async Task ObterAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        ctx.Branch = await ResolverBranchAsync(onLog, ct);

        var pasta = ctx.DirFonte;
        await ApagarPastaAsync(pasta, onLog, ct);

        string? erroZip = null;
        try
        {
            var zip = await BaixarZipAsync(ctx.Branch, onLog, ct);
            try
            {
                onLog("Extraindo o projeto...");
                var arquivos = Downloader.ExtrairZip(zip, pasta, ct);
                onLog($"{arquivos} arquivos extraídos.");
            }
            catch (Exception ex)
            {
                erroZip = "falha ao extrair: " + ex.Message;
            }
        }
        catch (Exception ex)
        {
            erroZip = ex.Message;
        }

        if (erroZip == null)
        {
            var achado = Downloader.LocalizarProjeto(pasta, InstaladorInfo.CsprojPrincipal);
            if (!string.IsNullOrEmpty(achado))
            {
                AplicarResultado(ctx, achado, "download do ZIP");
                CorrigirBotoesDuplicados(ctx.PastaFonte, onLog);
                return;
            }
            erroZip = $"o ZIP foi extraído, mas {InstaladorInfo.CsprojPrincipal} não foi encontrado em \"{pasta}\".";
        }

        onLog("Falha no ZIP (" + erroZip + ")");
        onLog("Tentando o plano B: 'git clone' (requer Git instalado)...");

        // Descarta o que o ZIP tiver deixado (download ok mas extração/csproj incompleto) para
        // que o clone comece de uma pasta limpa, no mesmo lugar do ZIP.
        await ApagarPastaAsync(ctx.DirFonte, onLog, ct);

        var destino = await ClonarAsync(ctx, ctx.DirFonte, onLog, ct);
        AplicarResultado(ctx, Downloader.LocalizarProjeto(destino, InstaladorInfo.CsprojPrincipal), "git clone");
        CorrigirBotoesDuplicados(ctx.PastaFonte, onLog);
    }

    // Correção de defeito conhecido no código publicado do repositório.
    //
    // Forms/frmEstoque.cs no branch master declara os botões btnExportarPdf e btnExportarExcel
    // DUAS vezes dentro do mesmo método (uma na posição (542, 9)/(542, 39) e outra na
    // (587, 90)/(587, 125)). Isso produz CS0128 e derruba o 'dotnet publish' da etapa 10, ou
    // seja, a instalação inteira falha por um defeito do código baixado — não do instalador.
    // Como o instalador consome o repositório já publicado, ele remove a primeira ocorrência de
    // cada botão duplicado, preservando a que está na posição correta.
    //
    // É idempotente: quando o defeito já estiver corrigido no repositório, nada é alterado e
    // este método vira um no-op. Debe ser removido assim que a correção for enviada ao master.
    private static void CorrigirBotoesDuplicados(string raizProjeto, Action<string> onLog)
    {
        var caminho = Path.Combine(raizProjeto, "Forms", "frmEstoque.cs");
        if (!File.Exists(caminho))
            return;

        var texto = File.ReadAllText(caminho);
        var declaracoes = Regex.Matches(
            texto,
            @"^[ \t]*var (?<nome>btnExportar[A-Za-z0-9_]*)[ \t]*=[ \t]*new Button[^\r\n]*\r?\n",
            RegexOptions.Multiline);

        // Agrupa por nome de variável e marca a PRIMEIRA ocorrência de cada nome declarado mais
        // de uma vez — é a cópia sobrando que precisa sair.
        var aRemover = declaracoes
            .Cast<Match>()
            .GroupBy(m => m.Groups["nome"].Value, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.First())
            .OrderByDescending(m => m.Index)
            .ToList();

        if (aRemover.Count == 0)
            return;

        foreach (var m in aRemover)
            texto = texto.Remove(m.Index, m.Length);

        File.WriteAllText(caminho, texto, new UTF8Encoding(false));
        onLog($"   correção aplicada: removidas {aRemover.Count} declarações duplicadas em Forms/frmEstoque.cs (CS0128).");
    }

    private static async Task<string> BaixarZipAsync(string branch, Action<string> onLog, CancellationToken ct)
    {
        Directory.CreateDirectory(InstaladorInfo.DirDownload);
        var zip = Path.Combine(InstaladorInfo.DirDownload, $"{InstaladorInfo.RepoNome}-{branch}.zip");

        onLog($"Baixando {InstaladorInfo.RepoPagina} (branch {branch})...");
        long ultimaDezena = -1;
        await Downloader.BaixarAsync(
            InstaladorInfo.ZipUrl(branch),
            zip,
            (recebido, total) =>
            {
                // O log cresce a cada bloco lido; arredondar para 10% evita milhares de linhas.
                var dezena = total > 0 ? recebido * 10 / total : -1;
                if (dezena == ultimaDezena) return;
                ultimaDezena = dezena;
                onLog(total > 0
                    ? $"   download: {recebido / 1024d / 1024d:0.0} MB / {total / 1024d / 1024d:0.0} MB ({dezena * 10}%)"
                    : $"   download: {recebido / 1024d / 1024d:0.0} MB");
            },
            ct);

        onLog("Download concluído: " + zip);
        return zip;
    }

    // Plano B: clona o repositório. Usa --depth 1 (só a última revisão, muito mais rápido) e
    // --branch explícita, coerente com a branch resolvida na etapa anterior. Devolve a pasta
    // onde o repositório foi clonado.
    private static async Task<string> ClonarAsync(SetupContext ctx, string destino, Action<string> onLog, CancellationToken ct)
    {
        var git = InstaladorInfo.LocalizarGit();
        if (string.IsNullOrEmpty(git))
            throw new Exception(
                "Não foi possível obter o código do GitHub (falha no ZIP) e o Git não está instalado. " +
                "Instale o Git (https://git-scm.com/download/win) e execute o instalador novamente.");

        onLog("Git localizado em: " + git);

        // O clone vai para a MESMA pasta do ZIP (ctx.DirFonte), e não para uma pasta "-git"
        // separada. Assim o layout instalado é o mesmo nos dois caminhos: o código sempre fica
        // em "<instalação>\source". A limpeza dos arquivos que o ZIP deixou parcialmente
        // extraídos é feita pelo chamador antes deste método, evitando misturar as duas cópias.
        await ApagarPastaAsync(destino, onLog, ct);

        onLog($"git clone --depth 1 --branch {ctx.Branch} ...");
        var r = await ProcessUtil.RunAsync(
            git,
            new[] { "clone", "--depth", "1", "--branch", ctx.Branch, InstaladorInfo.RepoUrlGit, destino },
            onLine: s => onLog("   " + s),
            ct: ct);

        if (r.ExitCode != 0)
        {
            // Limpeza do clone parcial: nao pode mascarar o erro do git que vem logo abaixo.
            try { await ApagarPastaAsync(destino, _ => { }, CancellationToken.None); } catch { }
            throw new Exception("Falha no 'git clone' (código " + r.ExitCode + ").");
        }

        return destino;
    }

    private static void AplicarResultado(SetupContext ctx, string caminhoCsproj, string origem)
    {
        if (string.IsNullOrEmpty(caminhoCsproj) || !File.Exists(caminhoCsproj))
            throw new Exception(
                $"PDVStore.csproj não localizado após a etapa de {origem}. " +
                $"Verifique se {InstaladorInfo.RepoNome} ainda existe em {InstaladorInfo.RepoPagina}.");

        ctx.CaminhoProjeto = caminhoCsproj;
        // PastaFonte passa a ser a raiz que contém o .csproj, para que as demais etapas
        // (dotnet restore/publish) apontem para a pasta de trabalho correta e não para o
        // embrulho "<repo>-<branch>" do ZIP.
        ctx.PastaFonte = Path.GetDirectoryName(caminhoCsproj)!;
    }

    // Reescreve o nome do banco dentro de Helpers/ConnectionHelper.cs do código baixado.
    // POR QUE necessário: o app fixa "PDV_StoreDB" em uma string literal, então o campo "nome
    // do banco" da tela de boas-vindas não teria efeito nenhum sem este ajuste. Devolve false
    // (sem lançar) quando o arquivo ou o padrão não existem — a instalação segue com o valor
    // original do repositório em vez de falhar.
    public static bool AplicarNomeBanco(string raizProjeto, string nomeBanco, out string mensagem)
    {
        var caminho = Path.Combine(raizProjeto, "Helpers", "ConnectionHelper.cs");
        if (!File.Exists(caminho))
        {
            mensagem = $"ConnectionHelper.cs não encontrado em \"{raizProjeto}\".";
            return false;
        }

        var texto = File.ReadAllText(caminho);
        var padrao = new Regex(@"Database\s*=\s*[A-Za-z0-9_]+", RegexOptions.IgnoreCase);
        var achado = padrao.Match(texto);
        if (!achado.Success)
        {
            mensagem = $"Nenhuma ocorrência de \"Database=...\" em {caminho}.";
            return false;
        }

        // MatchEvaluator em vez de string de substituição: o nome do banco nunca é
        // interpretado como sequência "$1" de Regex.Replace.
        var original = achado.Value;
        var novo = "Database=" + nomeBanco;
        File.WriteAllText(caminho, padrao.Replace(texto, _ => novo, 1));

        mensagem = original + " -> " + novo;
        return true;
    }

    // Apaga a pasta de trabalho, tolerando o caso de o build anterior ainda estar com arquivos
    // abertos (o compilador do Roslyn costuma segurar o binário por alguns instantes).
    private static async Task ApagarPastaAsync(string pasta, Action<string> onLog, CancellationToken ct)
    {
        if (!Directory.Exists(pasta)) return;

        // Desliga os servidores de build do .NET antes de remover a pasta. O compilador do
        // Roslyn (VBCSCompiler.exe) e o MSBuild ficam como processos de servidor depois de um
        // restore/publish e seguram os assemblies em obj\ e bin\. Sem este desligamento, a
        // segunda execucao do instalador falha no Directory.Delete com "arquivo em uso",
        // mesmo com a pasta aparentemente livre.
        var dotnet = InstaladorInfo.LocalizarDotnet();
        if (!string.IsNullOrEmpty(dotnet))
        {
            try
            {
                await ProcessUtil.RunAsync(dotnet, new[] { "build-server", "shutdown" }, onLine: s => onLog("   " + s), ct: ct);
            }
            catch
            {
                // Sem servidor de build ativo nao ha o que desligar; a remocao abaixo basta.
            }
        }

        onLog("Limpando a pasta de trabalho anterior: " + pasta);
        for (int tentativa = 1; ; tentativa++)
        {
            try
            {
                Directory.Delete(pasta, recursive: true);
                return;
            }
            catch (Exception ex) when (tentativa < 4)
            {
                onLog($"   pasta ainda em uso ({ex.GetType().Name}), tentativa {tentativa}...");
                await Task.Delay(1200, ct);
            }
        }
    }
}
