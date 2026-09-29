using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

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

            // O LocalDB cria bancos de usuário no PERFIL do Windows (%USERPROFILE%), o que
            // espalha os arquivos .mdf fora da pasta de instalação. Criar o banco antes, com
            // FILENAME explícito, é o que faz os arquivos nascerem em <instalação>\.
            await CriarBancoNaPastaDeInstalacaoAsync(ctx, onLog, ct);

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
    // IMPORTANTE: SqlLocalDB.exe devolve o código de saída 0 mesmo quando o comando falha, e a
    // SAÍDA DE TEXTO É TRADUZIDA conforme o idioma do Windows. Nenhum dos dois serve para dizer
    // se a operação deu certo. Todo o sucesso aqui é confirmado POSITIVAMENTE pelo registro
    // (ConsultarInstancia / MotorRodando), a única fonte que não muda com o idioma.
    private static async Task RecriarInstanciaLocalDbAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        var exe = ctx.CaminhoSqlLocalDb;
        if (string.IsNullOrEmpty(exe))
            throw new Exception("SqlLocalDB.exe não localizado; o LocalDB precisa estar instalado.");

        var nome = InstaladorInfo.InstanciaLocalDb;
        ctx.InstanciaExiste = InstanciaExiste(nome);

        if (ctx.InstanciaExiste && ctx.RecriarBanco)
        {
            onLog($"  A instância \"{nome}\" já existe e será recriada. TODOS os dados do banco atual serão perdidos.");

            await EncerrarAplicativoEmExecucao(onLog);

            // Parar antes de excluir é obrigatório. Falha aqui costuma significar conexão ainda
            // aberta (app, SSMS ou pool que não fechou), que é justamente o caso que quebra o
            // 'delete'/'create' logo em seguida.
            await ProcessUtil.RunAsync(exe, new[] { "stop", nome }, onLine: s => onLog("     " + s), ct: ct);
            if (MotorRodando(nome))
                onLog("     o 'stop' deixou o motor no ar; pode haver conexão aberta. Seguindo mesmo assim.");

            // O 'stop' responde antes de o motor realmente encerrar: o sqlservr.exe da instância
            // só libera a pasta dela alguns segundos depois. Sem esta espera o 'create' seguinte
            // roda com a pasta ainda em uso e falha.
            await AguardarMotorDaInstanciaSairAsync(nome, onLog, ct);

            var excluir = await ProcessUtil.RunAsync(exe, new[] { "delete", nome }, onLine: s => onLog("     " + s), ct: ct);

            // Confirma pelo estado real: registro sem a instância, e não pelo texto do comando.
            if (InstanciaExiste(nome))
                throw new Exception(
                    "O comando delete foi executado, mas a instância \"" + nome + "\" continua registrada. " +
                    "Verifique se ela está em uso por outro aplicativo. Resposta do SqlLocalDB: " + Resumir(excluir.Output));

            ctx.InstanciaExiste = false;
            onLog($"  Instância \"{nome}\" excluída.");

            // A exclusão da instância não leva junto os arquivos do banco; precisam sair antes
            // de a etapa 9 tentar o CREATE DATABASE. Eles só podem ser apagados com o motor
            // antigo encerrado, por isso a remoção só agora e com uma segunda tentativa.
            await RemoverArquivosBancoOrfaosAsync(ctx, onLog, ct);
        }
        else if (ctx.InstanciaExiste)
        {
            onLog($"  A instância \"{nome}\" existe e será reaproveitada (recriação desmarcada).");
        }

        if (!ctx.InstanciaExiste)
        {
            onLog($"  Criando a instância \"{nome}\"...");
            await CriarInstanciaAsync(exe, nome, onLog, ct);
        }

        onLog($"  Iniciando a instância \"{nome}\"...");
        await IniciarInstanciaAsync(exe, nome, onLog, ct);
    }

    // O 'sqllocaldb stop' é assíncrono: ele responde assim que o pedido é aceito, mas o
    // sqlservr.exe da instância continua vivo por alguns segundos segurando a pasta dela. Como
    // o 'create' seguinte precisa criar o master.mdf exatamente nessa pasta, a corrida faz a
    // etapa 6 falhar de forma intermitente.
    //
    // A espera é pelo PID que o registro informa, e não por tempo fixo nem por texto traduzido.
    private static async Task AguardarMotorDaInstanciaSairAsync(string nome, Action<string> onLog, CancellationToken ct)
    {
        for (int i = 1; i <= 20; i++)
        {
            ct.ThrowIfCancellationRequested();

            var (_, _, pid) = ConsultarInstancia(nome);
            if (!ProcessoVivo(pid))
                return;

            await Task.Delay(1000, ct);
        }

        onLog("     o motor não encerrou em 20s; finalizando o processo do LocalDB.");
        EncerrarMotoresLocalDb(onLog);
        await Task.Delay(2000, ct);
    }

    // Encerra apenas os motores do LocalDB. A distinção é pelo caminho do executável: o motor do
    // LocalDB fica em "...\LocalDB\Binn\SqlServr.exe" e o SQL Server completo (que pode estar
    // rodando a mesma máquina) em "...\Binn\SqlServr.exe". Derrubar o SQL Server completo por
    // causa de um instalador seria um dano sério, então o filtro é obrigatório.
    private static void EncerrarMotoresLocalDb(Action<string> onLog)
    {
        Process[] motores;
        try { motores = Process.GetProcessesByName("sqlservr"); }
        catch { return; }

        foreach (var p in motores)
        {
            string? caminho = null;
            try { caminho = p.MainModule?.FileName; } catch { }

            if (caminho == null || caminho.IndexOf(@"\LocalDB\Binn\", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            try
            {
                onLog("     motor do LocalDB encerrado: PID " + p.Id);
                p.Kill();
                p.WaitForExit(5000);
            }
            catch { }
        }
    }

    // Pasta onde o LocalDB guarda os arquivos de uma instância. O caminho é do SqlLocalDB e NÃO
    // segue o formato "%LOCALAPPDATA%\Microsoft\SqlServer\LocalDB" (esse não existe); o real é
    // "%LOCALAPPDATA%\Microsoft\Microsoft SQL Server Local DB\Instances\<nome>".
    //
    // O nome dessa pasta depende do idioma do Windows, então ela só é usada como CHUTE, e apenas
    // quando o registro não tem o caminho real (que é o caso da instância órfã, já sem registro).
    private static string PastaInstanciaLocalDb(string nome)
    {
        var (_, pasta, _) = ConsultarInstancia(nome);
        if (!string.IsNullOrEmpty(pasta))
            return pasta;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Microsoft SQL Server Local DB", "Instances", nome);
    }

    // Desloca a pasta de uma instância que NÃO está registrada.
    //
    // POR QUE: o 'sqllocaldb delete' remove o registro mas deixa a pasta, e uma instalação
    // interrompida deixa a pasta sem registro nenhum. Nesse estado o 'create' falha, porque
    // tenta criar a pasta por cima de uma que já existe com arquivos do motor pela metade. A
    // pasta é RENOMEADA, não apagada: destrói-se o menos possível para desbloquear a instalação.
    private static void AfastarPastaInstanciaOrfa(string nome, Action<string> onLog)
    {
        var pasta = PastaInstanciaLocalDb(nome);
        if (!Directory.Exists(pasta))
            return;

        var destino = pasta + ".antigo-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        try
        {
            Directory.Move(pasta, destino);
            onLog("     pasta de instância órfã afastada (instância não estava registrada):");
            onLog("       " + pasta);
            onLog("       -> " + destino);
        }
        catch
        {
            // Pastas presas por um motor que não shutou: encerra o motor e tenta de novo.
            EncerrarMotoresLocalDb(onLog);
            try
            {
                Directory.Move(pasta, destino);
                onLog("     pasta de instância órfã afastada após encerrar o motor: " + destino);
            }
            catch (Exception ex)
            {
                onLog("     AVISO: não foi possível afastar a pasta de instância órfã (" + ex.Message + ").");
                onLog("       " + pasta);
            }
        }
    }

    // Deixa a instância pronta para uso.
    //
    // POR QUE não basta um 'create': "MSSQLLocalDB" é a instância AUTOMÁTICA e o LocalDB já a
    // cria sozinho no primeiro 'start'. Insistir no 'create' é o que quebrava em máquinas
    // diferentes da de desenvolvimento — onde o create funciona — e o sintoma é exatamente o
    // erro reportado. A ordem agora é: afastar pasta órfã -> 'create' ->, se falhar e a
    // instância for a automática, 'start', que a cria sob demanda.
    private static async Task CriarInstanciaAsync(string exe, string nome, Action<string> onLog, CancellationToken ct)
    {
        const int tentativas = 3;
        var automatica = string.Equals(nome, "MSSQLLocalDB", StringComparison.OrdinalIgnoreCase);

        if (automatica)
            onLog("     instância automática: o LocalDB a cria sozinho no primeiro 'start'.");

        if (!InstanciaExiste(nome))
            AfastarPastaInstanciaOrfa(nome, onLog);

        for (int t = 1; t <= tentativas; t++)
        {
            var r = await ProcessUtil.RunAsync(exe, new[] { "create", nome }, onLine: s => onLog("     " + s), ct: ct);

            // Sucesso = a instância passou a existir. A resposta do comando só vai para o log,
            // porque em Windows em português ela vem traduzida e não serve para decidir nada.
            if (InstanciaExiste(nome))
                return;

            if (t < tentativas)
            {
                var espera = t * 2000;
                onLog($"     o 'create' não registrou a instância; o motor antigo pode ainda estar segurando os arquivos. Nova tentativa em {espera / 1000}s.");
                EncerrarMotoresLocalDb(onLog);
                await Task.Delay(espera, ct);
                continue;
            }

            if (!automatica)
                throw new Exception(
                    $"Não foi possível criar a instância LocalDB \"{nome}\" mesmo após {tentativas} tentativas. " +
                    "Resposta do SqlLocalDB: " + Resumir(r.Output) + DiagnosticoLocalDb(nome) + DiagnosticoAmbiente(nome));

            // Última via para a instância automática: 'start' sem 'create'. O LocalDB cria a
            // instância sob demanda e é o caminho que todo mundo usa no dia a dia.
            onLog("     'create' não funcionou; tentando iniciar a instância automática direto, sem criar antes.");
            var s = await ProcessUtil.RunAsync(exe, new[] { "start", nome }, onLine: x => onLog("     " + x), ct: ct);

            if (InstanciaExiste(nome))
            {
                onLog("     instância automática criada e iniciada pelo 'start'.");
                return;
            }

            throw new Exception(
                $"Não foi possível criar a instância LocalDB \"{nome}\" mesmo após {tentativas} tentativas de 'create' " +
                "e uma tentativa de 'start' direto. " +
                "Resposta do 'create': " + Resumir(r.Output) +
                " | Resposta do 'start': " + Resumir(s.Output) + DiagnosticoLocalDb(nome) + DiagnosticoAmbiente(nome));
        }
    }

    // Inicia a instância e confere o estado de verdade. O 'sqllocaldb start' pode responder que
    // iniciou e o motor morrer logo em seguida (memória, arquivo preso, permissão); confiar só
    // no texto deixava a falha aparecer depois, na etapa 9, com uma mensagem que não aponta
    // a causa. Aqui a conferência é pelo PID do motor registrado.
    private static async Task IniciarInstanciaAsync(string exe, string nome, Action<string> onLog, CancellationToken ct)
    {
        const int tentativas = 3;

        for (int t = 1; t <= tentativas; t++)
        {
            var r = await ProcessUtil.RunAsync(exe, new[] { "start", nome }, onLine: s => onLog("     " + s), ct: ct);

            if (MotorRodando(nome))
                return;

            if (t == tentativas)
                throw new Exception(
                    $"Não foi possível iniciar a instância LocalDB \"{nome}\": o motor não ficou no ar. " +
                    "Resposta do SqlLocalDB: " + Resumir(r.Output) + DiagnosticoLocalDb(nome) + DiagnosticoAmbiente(nome));

            var espera = t * 3000;
            onLog($"     a instância não ficou em execução; nova tentativa em {espera / 1000}s.");
            await Task.Delay(espera, ct);
        }
    }

    // O SqlLocalDB não informa por que o motor não subiu: o motivo fica no error.log do próprio
    // motor, dentro da pasta da instância. Apontar esse arquivo na mensagem de erro evita ter
    // que caçar o motivo na máquina depois.
    private static string DiagnosticoLocalDb(string nome)
    {
        var log = Path.Combine(PastaInstanciaLocalDb(nome), "error.log");
        if (!File.Exists(log))
            return "";

        try
        {
            var linhas = File.ReadAllLines(log);
            var ultimas = linhas
                .Skip(Math.Max(0, linhas.Length - 10))
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToArray();

            if (ultimas.Length == 0)
                return "";

            return Environment.NewLine + "      Últimas linhas de " + log + ":"
                 + Environment.NewLine + "      " + string.Join(Environment.NewLine + "      ", ultimas);
        }
        catch
        {
            return "";
        }
    }

    // Causas que não são do instalador, e que o LocalDB não explica na saída. Sem isto a mensagem
    // só diz "não foi possível criar" e o usuário não tem por onde começar. Estas três são as que
    // mais aparecem em outra máquina: disco cheio no volume do perfil, pasta de perfil sem
    // permissão de escrita, e instalação do LocalDB divergente da ferramenta encontrada.
    private static string DiagnosticoAmbiente(string nome)
    {
        var linhas = new List<string>();

        try
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var raiz = Path.GetPathRoot(perfil);
            if (!string.IsNullOrEmpty(raiz))
            {
                var livre = new DriveInfo(raiz).AvailableFreeSpace;
                var gb = Math.Round(livre / 1024d / 1024d, 1);
                linhas.Add($"Espaço livre em {raiz}: {gb} GB (o LocalDB precisa de alguns centenas de MB; abaixo de 1 GB a criação falha).");
                if (livre < 1024L * 1024 * 1024)
                    linhas.Add("      -> O espaço livre parece ser a causa. Libere espaço e execute de novo.");
            }

            var pasta = PastaInstanciaLocalDb(nome);
            linhas.Add("Pasta de perfil do LocalDB: " + pasta);

            if (!Directory.Exists(pasta))
            {
                try
                {
                    Directory.CreateDirectory(pasta);
                    linhas.Add("      -> A pasta não existia e foi criada agora: o perfil tem permissão de escrita.");
                }
                catch (Exception ex)
                {
                    linhas.Add("      -> A pasta não pôde ser criada (" + ex.Message + "). Perfil de usuário sem permissão de escrita ou redirecionado para rede.");
                }
            }
        }
        catch (Exception ex)
        {
            linhas.Add("Não foi possível coletar o diagnóstico do ambiente: " + ex.Message);
        }

        if (linhas.Count == 0)
            return "";

        return Environment.NewLine + "      Ambiente:"
             + Environment.NewLine + "      " + string.Join(Environment.NewLine + "      ", linhas);
    }

    // Consulta a instância no REGISTRO do Windows, em vez de ler a saída do SqlLocalDB.
    //
    // POR QUE o registro: a saída do SqlLocalDB é traduzida. Numa máquina em português o
    // 'create' responde "Instância LocalDB ... criada com versão ..." e o 'info' traz
    // "Criação automática:" e "Nome do pipe da instância:". O código procurava 'Auto-create' e
    // 'Instance pipe name', não encontrava, e declarava falha DEPOIS de criar a instância com
    // sucesso. O registro não é traduzido: cada instância tem uma subchave em
    // HKCU\SOFTWARE\Microsoft\Microsoft SQL Server\UserInstances, e o valor 'DataDirectory'
    // termina com o nome da instância. O 'InstanceProcessId' traz o PID do motor (e só existe
    // enquanto ele está no ar), o que dá um jeito preciso de esperar o motor encerrar.
    private static (bool Existe, string Pasta, int PidMotor) ConsultarInstancia(string nome)
    {
        try
        {
            using var raiz = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            using var chaves = raiz.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\UserInstances");
            if (chaves == null)
                return (false, "", 0);

            foreach (var sub in chaves.GetSubKeyNames())
            {
                using var k = chaves.OpenSubKey(sub);
                var dir = k?.GetValue("DataDirectory") as string;
                if (string.IsNullOrEmpty(dir))
                    continue;

                var pasta = dir.TrimEnd('\\');
                if (!pasta.EndsWith("\\" + nome, StringComparison.OrdinalIgnoreCase))
                    continue;

                return (true, pasta, k?.GetValue("InstanceProcessId") is int pid ? pid : 0);
            }
        }
        catch
        {
            // Sem acesso ao registro: cai no método textual como último recurso.
        }

        return (false, "", 0);
    }

    private static bool ProcessoVivo(int pid)
    {
        if (pid <= 0)
            return false;

        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static bool InstanciaExiste(string nome)
        => ConsultarInstancia(nome).Existe;

    // O motor está no ar quando o registro traz um InstanceProcessId cujo processo existe.
    private static bool MotorRodando(string nome)
    {
        var (_, _, pid) = ConsultarInstancia(nome);
        return ProcessoVivo(pid);
    }

    // Remove os arquivos de dados órfãos do banco, em TODOS os lugares onde ele pode ter
    // ficado: a pasta de instalação e o perfil do usuário.
    //
    // POR QUE as duas: até a instalação gravar o banco na pasta de instalação, o LocalDB deixava
    // o .mdf em C:\Users\<usuário>. Agora o .mdf nasce em <instalação>, mas uma instalação feita
    // com a versão anterior deixou o arquivo no perfil, e apagar a instância continua não
    // apagando nenhum dos dois. Sem varrer os dois lugares, a recriação falha com
    // "Cannot create file ... because it already exists".
    //
    // POR QUE agora falha em vez de só avisar: quando o arquivo está preso pelo motor antigo, a
    // etapa 6 "passava" e a falha só aparecia na etapa 9, com um CREATE DATABASE que não
    // apontava a origem. Falhar aqui, com o nome do arquivo e a instrução, é muito mais honesto.
    private static async Task RemoverArquivosBancoOrfaosAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        var nomes = new[] { ctx.NomeBanco + ".mdf", ctx.NomeBanco + "_log.ldf", ctx.NomeBanco + ".ldf" };

        var pastas = new List<string> { ctx.PastaBancoEfetiva };
        var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(perfil) && !pastas.Contains(perfil, StringComparer.OrdinalIgnoreCase))
            pastas.Add(perfil);

        for (int tentativa = 1; tentativa <= 3; tentativa++)
        {
            var restantes = new List<string>();
            var removidos = 0;

            foreach (var pasta in pastas)
            foreach (var nome in nomes)
            {
                var caminho = Path.Combine(pasta, nome);
                if (!File.Exists(caminho))
                    continue;

                try
                {
                    File.Delete(caminho);
                    onLog("     arquivo de dado antigo removido: " + caminho);
                    removidos++;
                }
                catch
                {
                    restantes.Add(caminho);
                }
            }

            if (restantes.Count == 0)
            {
                if (removidos == 0)
                    onLog("     nenhum arquivo de dado antigo do banco foi encontrado.");
                return;
            }

            ct.ThrowIfCancellationRequested();
            if (tentativa == 3)
                throw new Exception(
                    "Não foi possível apagar o arquivo de banco antigo e ele vai fazer o CREATE " +
                    "falhar na etapa seguinte. Feche o SQL Server Management Studio e qualquer outro " +
                    "programa que possa estar usando o banco e execute o instalador de novo. " +
                    "Arquivo(s) travado(s): " + string.Join(", ", restantes));

            // Arquivo preso quase sempre significa motor do LocalDB ainda vivo.
            onLog("     arquivo ainda em uso; o motor do LocalDB provavelmente continua aberto. Tentando liberar...");
            EncerrarMotoresLocalDb(onLog);
            await Task.Delay(2000, ct);
        }
    }

    // Condensa a saída do SqlLocalDB em uma linha legível para a mensagem de erro, já que ela
    // vem com as quebras de linha do console. É apenas para diagnóstico: o texto vem traduzido
    // conforme o idioma do Windows e por isso NÃO decide se a operação deu certo.
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

    // Cria o banco com os arquivos .mdf/.ldf dentro da pasta de instalação.
    //
    // POR QUE não deixa o 'dotnet ef database update' criar: o LocalDB guarda os bancos de
    // usuário no PERFIL do Windows, e o EF usa o caminho padrão do servidor ao criar. O
    // resultado era C:\Users\<usuário>\<banco>.mdf, espalhado fora da instalação e perdido a
    // cada troca de usuário do Windows. Declarando FILENAME, o banco nasce onde se espera:
    // junto do executável.
    //
    // O passo seguinte ('dotnet ef database update') continua funcionando: ele detecta que o
    // banco já existe e apenas aplica as migrações.
    private static async Task CriarBancoNaPastaDeInstalacaoAsync(SetupContext ctx, Action<string> onLog, CancellationToken ct)
    {
        var pasta = ctx.PastaBancoEfetiva;
        var nome = ctx.NomeBanco;

        // O nome entra dentro de um comando SQL e de nomes de arquivo, então é validado aqui.
        // A tela do instalador já restringe a letras/números/'_', mas esta função não depende
        // disso: é chamada pelo motor, e o motor não valida.
        if (string.IsNullOrWhiteSpace(nome) || nome.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
            throw new Exception(
                "Nome de banco inválido: \"" + nome + "\". Use apenas letras, números e '_'.");

        try
        {
            Directory.CreateDirectory(pasta);
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Não foi possível criar a pasta do banco em \"" + pasta + "\": " + ex.Message +
                ". O banco precisa ficar numa pasta onde o usuário atual possa escrever.");
        }

        var mdf = Path.Combine(pasta, nome + ".mdf");
        var ldf = Path.Combine(pasta, nome + "_log.ldf");

        // Aspa simples fecharia o literal SQL. Num caminho padrão isso nunca acontece, mas um
        // usuário que aponte a pasta para um caminho com apóstrofo merece erro claro, não SQL
        // malformado.
        if (mdf.Contains('\'') || ldf.Contains('\''))
            throw new Exception("A pasta do banco não pode conter apóstrofo: \"" + pasta + "\".");

        // A conexão vai para o master: conectar direto no banco que ainda não existe falharia,
        // que é exatamente o que se quer evitar aqui.
        var conexao = new SqlConnectionStringBuilder(ctx.ConnectionString)
        {
            InitialCatalog = "master",
        }.ConnectionString;

        const string sql = @"
IF DB_ID(@nome) IS NULL
BEGIN
    DECLARE @cmd nvarchar(max) =
        N'CREATE DATABASE ' + QUOTENAME(@nome) +
        N' ON PRIMARY (NAME = N''' + @nome + N''', FILENAME = N''' + @mdf + N''', SIZE = 32MB, FILEGROWTH = 16MB)' +
        N' LOG ON (NAME = N''' + @nome + N'_log'', FILENAME = N''' + @ldf + N''', SIZE = 8MB, FILEGROWTH = 8MB)';
    EXEC sp_executesql @cmd;
END";

        try
        {
            await using var cn = new SqlConnection(conexao);
            await cn.OpenAsync(ct);

            await using var cmd = cn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@nome", nome);
            cmd.Parameters.AddWithValue("@mdf", mdf);
            cmd.Parameters.AddWithValue("@ldf", ldf);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Não foi possível criar o banco \"" + nome + "\" com os arquivos em \"" + pasta + "\": "
                + ex.Message +
                " Verifique se o usuário tem permissão de escrita nessa pasta e se há espaço livre.");
        }

        if (File.Exists(mdf))
            onLog("  Banco criado com os arquivos em: " + pasta);
        else
            onLog("  Banco já existia; arquivos mantidos em: " + pasta);
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
