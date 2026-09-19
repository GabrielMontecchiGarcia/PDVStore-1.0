using System.Collections.Generic;
using System.IO;

namespace PDVStore.Setup.Services;

public enum StatusRequisito
{
    Instalado,
    Ausente,
    EmAndamento,
    Falha
}

public sealed class Requisito
{
    public string Nome { get; init; } = "";
    public string Detalhe { get; set; } = "";
    public StatusRequisito Status { get; set; } = StatusRequisito.Ausente;
}

/// <summary>
/// Estado compartilhado de toda a instalação (caminhos escolhidos, resultados das verificações).
/// </summary>
public sealed class SetupContext
{
    public string DirInstalacao { get; set; } = AppContext.BaseDirectory;
    public string NomeBanco { get; set; } = "PDV_StoreDB";

    public string? CaminhoProjeto { get; set; }
    public string? CaminhoExeApp { get; set; }

    public bool SdkDotNetInstalado { get; set; }
    public bool LocalDbInstalado { get; set; }
    public bool DotNetEfInstalado { get; set; }
    public string CaminhoSqlLocalDb { get; set; } = "";

    public List<Requisito> Requisitos { get; } = new();

    public string ConnectionString
        => InstaladorInfo.ConnectionStringPadrao(NomeBanco);

    public string LocalizarProjeto()
    {
        if (File.Exists(CaminhoProjeto))
            return CaminhoProjeto!;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++)
        {
            var csproj = Path.Combine(dir.FullName, "PDVStore.csproj");
            if (File.Exists(csproj))
                return csproj;
            dir = dir.Parent;
        }
        return "";
    }
}