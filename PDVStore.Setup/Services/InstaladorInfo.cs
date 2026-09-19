using System;
using System.IO;

namespace PDVStore.Setup.Services;

/// <summary>
/// Informações centralizadas do instalador: URLs oficiais de download e caminhos usados em todo o processo.
/// </summary>
public static class InstaladorInfo
{
    public const string DotNetInstallScriptUrl = "https://dot.net/v1/dotnet-install.ps1";
    public const string DotNetChannel = "8.0";
    public const string SqlLocalDbMsiUrl =
        "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi";

    // Caminho da pasta temporária usada pelo instalador para baixar scripts/MSIs intermediários.
    // POR QUE centralizar em uma propriedade computada: evita repetir Path.Combine(GetTempPath)
    // por todo o código e mantém um único lugar para mudar a pasta caso necessário.
    // Dependências: Path.GetTempPath() do System.IO (pasta de %TEMP% do Windows).
    public static string DirTemporario
        => Path.Combine(Path.GetTempPath(), "PDVStoreSetup");

    // Pasta onde o SDK .NET é instalado (instalação "por usuário", sem precisar de admin).
    // POR QUE usar LocalApplicationData: o script oficial dotnet-install coloca o runtime no
    // perfil do usuário quando executado sem elevação, evitando pedidos de UAC.
    // Dependências: Environment.SpecialFolder.LocalApplicationData (pasta %LOCALAPPDATA%).
    public static string DirDotNet
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "dotnet");

    // Pasta padrão das ferramentas globais do .NET instaladas para o usuário (ex.: dotnet-ef).
    // POR QUE: 'dotnet tool install --global' grava os executáveis em ~/.dotnet/tools, e esse
    // diretório também deve entrar no PATH para os comandos serem encontrados.
    // Dependências: Environment.SpecialFolder.UserProfile (a pasta do usuário atual).
    public static string DirFerramentasDotNet
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dotnet", "tools");

    // Caminho completo do executável do .NET (dotnet.exe) dentro da instalação por usuário.
    // POR QUE: o verificador e o preparador precisam saber exatamente onde está o dotnet para
    // chamar comandos como --list-sdks e dotnet publish sem depender do PATH do sistema.
    // Dependências: soma a propriedade computada DirDotNet + o nome do executável.
    public static string ExeDotNet
        => Path.Combine(DirDotNet, "dotnet.exe");

    // Caminho completo da ferramenta global dotnet-ef usada para aplicar migrações do EF Core.
    // POR QUE: as migrações do banco são executadas por 'dotnet ef database update', e localizar
    // o .exe direto evita ambiguidade caso exista outra versão instalada em outro lugar.
    // Dependências: soma a propriedade computada DirFerramentasDotNet + o nome do executável.
    public static string ExeDotNetEf
        => Path.Combine(DirFerramentasDotNet, "dotnet-ef.exe");

    // Monta a connection string padrão do LocalDB para um banco com o nome informado.
    // POR QUE a forma (localdb)\MSSQLLocalDB: essa é a instância LocalDB padrão criada pelo
    // instalador do SQL Server Express, acessível com autenticação integrada do Windows.
    // Dependências: apenas o parâmetro nomeBanco, interpolado na string de conexão.
    public static string ConnectionStringPadrao(string nomeBanco)
        => $"Server=(localdb)\\MSSQLLocalDB;Database={nomeBanco};Trusted_Connection=True;";

    // Descobre o executável do .NET, priorizando a instalação por usuário e caindo para o
    // comando 'where dotnet.exe' (que procura no PATH).
    // POR QUE a busca em duas etapas: o instalador pode ter acabado de baixar o SDK localmente,
    // e ainda assim o dotnet pode existir no PATH do usuário vindo de outra instalação.
    // Dependências: System.IO (File.Exists) e System.Diagnostics (Process para o 'where'),
    // com timeout de 8 segundos; retorna "" quando não encontrar nada.
    public static string LocalizarDotnet()
    {
        if (File.Exists(ExeDotNet)) return ExeDotNet;
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("where", "dotnet.exe")
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
            // ignorado -> retorna null (não instalado)
        }
        return "";
    }
}