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

    public static string DirTemporario
        => Path.Combine(Path.GetTempPath(), "PDVStoreSetup");

    public static string DirDotNet
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "dotnet");

    public static string DirFerramentasDotNet
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".dotnet", "tools");

    public static string ExeDotNet
        => Path.Combine(DirDotNet, "dotnet.exe");

    public static string ExeDotNetEf
        => Path.Combine(DirFerramentasDotNet, "dotnet-ef.exe");

    public static string ConnectionStringPadrao(string nomeBanco)
        => $"Server=(localdb)\\MSSQLLocalDB;Database={nomeBanco};Trusted_Connection=True;";

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