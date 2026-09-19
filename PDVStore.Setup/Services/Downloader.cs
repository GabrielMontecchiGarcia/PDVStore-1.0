using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PDVStore.Setup.Services;

/// <summary>
/// Baixa arquivos da internet reportando o progresso (bytes recebidos / total quando conhecido).
/// </summary>
public static class Downloader
{
    public static async Task DownloadAsync(
        string url,
        string filePath,
        Action<long, long>? onProgress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PDVStore.Setup/1.0");

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
}