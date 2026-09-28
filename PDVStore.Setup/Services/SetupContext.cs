using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PDVStore.Setup.Services;

public enum StatusRequisito
{
    Instalado,
    Ausente,
    EmAndamento,
    Falha
}

// Um pré-requisito verificado na etapa de diagnóstico. Opcional = o instalador funciona sem
// ele (o Git, por exemplo, só é o plano B quando o download do ZIP falha), então a ausência
// de um opcional não bloqueia o avanço.
public sealed class Requisito
{
    public string Nome { get; init; } = "";
    public string Detalhe { get; set; } = "";
    public StatusRequisito Status { get; set; } = StatusRequisito.Ausente;
    public bool Opcional { get; init; }
}

/// <summary>
/// Estado compartilhado de toda a instalação: onde instalar, de onde baixar o código, o que
/// já existe na máquina e o resultado das verificações.
/// </summary>
public sealed class SetupContext
{
    // ---- Destino ----
    // C:\PDVStore por padrão, criado pelo instalador. O código-fonte baixado do GitHub vai
    // para a subpasta "source", de modo que app e código fiquem juntos e rastreáveis.
    public string DirInstalacao { get; set; } = InstaladorInfo.PastaPadraoInstalacao;
    public string NomeBanco { get; set; } = InstaladorInfo.BancoPadrao;

    // Quando verdadeiro, a instância LocalDB é parada, excluída e recriada — o que descarta
    // o banco existente e todas as vendas cadastradas. Marcado por padrão para que a
    // instalação seja sempre limpa e previsível; o usuário pode desmarcar para reaproveitar
    // o banco que já está lá.
    public bool RecriarBanco { get; set; } = true;

    // ---- Código-fonte ----
    public string Branch { get; set; } = InstaladorInfo.BranchPadrao;
    public string PastaFonte { get; set; } = "";
    public string PastaClone => DirFonte + "-git";

    public string? CaminhoProjeto { get; set; }
    public string? CaminhoExeApp { get; set; }

    // ---- Estado da máquina ----
    public bool SdkDotNetInstalado { get; set; }
    public bool LocalDbInstalado { get; set; }
    public bool DotNetEfInstalado { get; set; }
    public bool GitInstalado { get; set; }
    public bool InstanciaExiste { get; set; }
    public string CaminhoSqlLocalDb { get; set; } = "";

    public List<Requisito> Requisitos { get; } = new();

    // Subpasta de trabalho dentro do diretório de instalação, onde o ZIP é extraído.
    public string DirFonte => Path.Combine(DirInstalacao, InstaladorInfo.PastaFonteRelativa);

    // Executável publicado dentro do diretório de instalação.
    public string CaminhoExe => Path.Combine(DirInstalacao, "PDVStore.exe");

    public string ConnectionString
        => InstaladorInfo.ConexaoPadrao(NomeBanco);

    // Confere se as escolhas da tela de boas-vindas são utilizáveis: caminho absoluto, com
    // volume válido (C:\...) e gravável. O instalador cria a pasta depois, em uma etapa própria.
    public bool TryValidarDestino(out string erro)
    {
        erro = "";

        if (string.IsNullOrWhiteSpace(DirInstalacao))
        {
            erro = "Informe a pasta de instalação.";
            return false;
        }

        if (!Path.IsPathRooted(DirInstalacao))
        {
            erro = "A pasta de instalação precisa ser um caminho absoluto (ex.: C:\\PDVStore).";
            return false;
        }

        try
        {
            var raiz = Path.GetPathRoot(DirInstalacao);
            if (string.IsNullOrEmpty(raiz) || !Directory.Exists(raiz))
            {
                erro = $"Unidade inexistente: \"{raiz}\".";
                return false;
            }

            // IsEmpty só lança quando a pasta existe, então a criação é tentada antes.
            if (!Directory.Exists(DirInstalacao))
                Directory.CreateDirectory(DirInstalacao);

            if (!IsGravavel(DirInstalacao))
            {
                erro = $"Sem permissão de escrita em \"{DirInstalacao}\". Escolha outra pasta (ou execute como administrador).";
                return false;
            }
        }
        catch (Exception ex)
        {
            erro = "Não foi possível usar a pasta informada: " + ex.Message;
            return false;
        }

        if (string.IsNullOrWhiteSpace(NomeBanco)
            || NomeBanco.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
        {
            erro = "O nome do banco só pode conter letras, números e underscore.";
            return false;
        }

        return true;
    }

    // Testa a gravação criando e removendo um arquivo temporário. Existe porque ACLs podem
    // negar escrita mesmo com a pasta já criada (por exemplo, C:\Program Files sem elevação).
    private static bool IsGravavel(string pasta)
    {
        var teste = Path.Combine(pasta, ".pdvstore-teste-" + System.Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(teste, "ok");
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { if (File.Exists(teste)) File.Delete(teste); } catch { }
        }
    }

    // Localiza o PDVStore.csproj. Primeiro no código recém-baixado; depois, subindo a partir
    // do executável, para o cenário de desenvolvimento em que o instalador roda de bin\Debug
    // dentro do próprio repositório. O limite de 8 níveis evita percorrer o disco inteiro.
    public string LocalizarProjeto()
    {
        if (!string.IsNullOrEmpty(CaminhoProjeto) && File.Exists(CaminhoProjeto))
            return CaminhoProjeto!;

        var baixado = Downloader.LocalizarProjeto(PastaFonte, InstaladorInfo.CsprojPrincipal);
        if (!string.IsNullOrEmpty(baixado))
            return baixado;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++)
        {
            var csproj = Path.Combine(dir.FullName, InstaladorInfo.CsprojPrincipal);
            if (File.Exists(csproj))
                return csproj;
            dir = dir.Parent;
        }
        return "";
    }
}
