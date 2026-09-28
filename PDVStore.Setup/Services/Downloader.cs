using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PDVStore.Setup.Services;

/// <summary>
/// Baixa arquivos da internet e extrai pacotes ZIP, reportando progresso.
/// </summary>
public static class Downloader
{
    // User-Agent padrão. O GitHub rejeita requisições sem User-Agent (HTTP 403), então ele é
    // obrigatório tanto para o zip do repositório quanto para a API que descobre a branch padrão.
    private const string UserAgent = "PDVStore.Setup/1.0";

    public static HttpClient CriarCliente()
        => new HttpClient { Timeout = TimeSpan.FromMinutes(30) };

    public static void ConfigurarCabecalho(HttpClient client)
        => client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

    // Baixa um arquivo da internet, salvando-o em filePath, e reporta o progresso via callback.
    // POR QUE esvaziar o conteúdo em blocos: o download pode ser de dezenas de MB (SDK/LocalDB);
    // usar ResponseHeadersRead + leitura por buffer evita carregar tudo em memória e permite
    // informar o andamento ao usuário linha a linha no log.
    public static async Task BaixarAsync(
        string url,
        string filePath,
        Action<long, long>? onProgress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var client = CriarCliente();
        ConfigurarCabecalho(client);

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        long recebido = 0;
        var buffer = new byte[81920];

        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var destino = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        int lidos;
        while ((lidos = await source.ReadAsync(buffer, ct)) > 0)
        {
            await destino.WriteAsync(buffer.AsMemory(0, lidos), ct);
            recebido += lidos;
            onProgress?.Invoke(recebido, total);
        }
    }

    // Testa se uma URL responde OK. Usado para descobrir qual branch existe no repositório
    // quando a API do GitHub não pode ser consultada (sem rede, por exemplo).
    public static async Task<bool> UrlExisteAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var client = CriarCliente();
            client.Timeout = TimeSpan.FromSeconds(30);
            ConfigurarCabecalho(client);
            using var r = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            return r.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Extrai todas as entradas de um ZIP em pastaDestino e devolve quantos arquivos foram gravados.
    // POR QUE não usar ZipFile.ExtractToDirectory direto: antes de criar cada arquivo o código
    // valida que o caminho final continua dentro de pastaDestino. Um zip de origem externa pode
    // conter entradas como "../../Windows/System32/..." (ataque Zip Slip); a checagem impede que
    // a extração escape da pasta de destino e sobrescreva arquivos do sistema.
    public static int ExtrairZip(string caminhoZip, string pastaDestino, CancellationToken ct = default)
    {
        Directory.CreateDirectory(pastaDestino);

        var raiz = Path.GetFullPath(pastaDestino);
        var prefixo = raiz.EndsWith(Path.DirectorySeparatorChar)
            ? raiz
            : raiz + Path.DirectorySeparatorChar;

        using var zip = ZipFile.OpenRead(caminhoZip);

        int arquivos = 0;
        foreach (var entrada in zip.Entries)
        {
            ct.ThrowIfCancellationRequested();

            // Entrada sem nome é apenas a pasta raiz do zip (o GitHub sempre a inclui).
            if (string.IsNullOrEmpty(entrada.Name))
                continue;

            var destino = Path.GetFullPath(Path.Combine(raiz, entrada.FullName));
            if (!destino.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
                throw new IOException(
                    "Entrada do ZIP fora da pasta de destino (Zip Slip): " + entrada.FullName);

            var pasta = Path.GetDirectoryName(destino);
            if (!string.IsNullOrEmpty(pasta))
                Directory.CreateDirectory(pasta);

            entrada.ExtractToFile(destino, overwrite: true);
            arquivos++;
        }

        return arquivos;
    }

    // Procura o arquivo de projeto (PDVStore.csproj) dentro da pasta extraída do zip.
    // POR QUE profundidade limitada a 3: o zip do GitHub embrulha tudo em uma única pasta
    // "<repo>-<branch>"; a busca recursiva rasa cobre esse caso e casos de publicação
    // aninhados, sem varrer a árvore inteira do disco.
    public static string LocalizarProjeto(string pastaRaiz, string nomeCsproj, int profundidade = 3)
    {
        if (string.IsNullOrWhiteSpace(pastaRaiz) || !Directory.Exists(pastaRaiz))
            return "";

        var direto = Path.Combine(pastaRaiz, nomeCsproj);
        if (File.Exists(direto))
            return direto;

        if (profundidade <= 0)
            return "";

        foreach (var sub in Directory.EnumerateDirectories(pastaRaiz))
        {
            var achado = LocalizarProjeto(sub, nomeCsproj, profundidade - 1);
            if (!string.IsNullOrEmpty(achado))
                return achado;
        }

        return "";
    }
}
