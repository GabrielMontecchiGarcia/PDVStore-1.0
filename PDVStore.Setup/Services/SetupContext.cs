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

    // Propriedade computada (sem armazenamento): devolve a connection string do modo de instalação,
    // já derivada do nome do banco escolhido pelo usuário.
    // POR QUE centralizar: todo o código (resumo da tela, migrações, aplicativo) usa a mesma
    // regra de conexão, evitando que strings soltas se desatualizem em pontos diferentes.
    // Dependências: InstaladorInfo.ConnectionStringPadrao(NomeBanco) e a propriedade NomeBanco.
    public string ConnectionString
        => InstaladorInfo.ConnectionStringPadrao(NomeBanco);

    // Localiza o arquivo PDVStore.csproj, verificando primeiro o caminho já conhecido e, se
    // necessário, subindo até 8 níveis de pastas a partir do diretório do executável.
    // POR QUE subir na árvore: o instalador roda a partir de bin/Debug quando não publicado,
    // então o .csproj do projeto costuma estar alguns níveis acima; o limite de 8 evita
    // percorrer o disco inteiro de forma desnecessária.
    // Dependências: System.IO (File.Exists, DirectoryInfo, Path) e a propriedade CaminhoProjeto;
    // retorna "" quando o projeto não é encontrado.
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