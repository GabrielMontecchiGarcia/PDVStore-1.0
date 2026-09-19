using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PDVStore.Setup.Services;

/// <summary>
/// Executa processos externos capturando a saída em tempo real (linha a linha).
/// </summary>
public static class ProcessUtil
{
    /// <summary>
    /// Executa o processo informado e retorna (código de saída, saída completa consolidada).
    /// Linhas de saída/erro são repassadas para <paramref name="onLine"/> quando informado.
    /// </summary>
    public static async Task<(int ExitCode, string Output)> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        System.Action<string>? onLine = null,
        CancellationToken ct = default,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        foreach (var a in arguments)
        {
            if (!string.IsNullOrWhiteSpace(a))
                psi.ArgumentList.Add(a);
        }

        if (environment != null)
        {
            foreach (var kv in environment)
                psi.Environment[kv.Key] = kv.Value;
        }

        using var processo = new Process { StartInfo = psi };
        var sb = new StringBuilder();

        processo.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            lock (sb) sb.AppendLine(e.Data);
            onLine?.Invoke(e.Data);
        };
        processo.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            lock (sb) sb.AppendLine(e.Data);
            onLine?.Invoke(e.Data);
        };

        if (!processo.Start())
            return (-1, "Falha ao iniciar o processo: " + fileName);

        processo.BeginOutputReadLine();
        processo.BeginErrorReadLine();

        await processo.WaitForExitAsync(ct).ConfigureAwait(false);

        return (processo.ExitCode, sb.ToString());
    }
}