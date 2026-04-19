using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MediaDownloader.Helpers;

/// <summary>
/// Clase auxiliar para ejecutar procesos externos y capturar su salida.
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Ejecuta un proceso de forma asíncrona y lee stdout línea por línea.
    /// </summary>
    public static async Task<int> RunAsync(
        string executable,
        string arguments,
        Action<string> onOutputLine,
        CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        
        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                onOutputLine(e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                onOutputLine(e.Data);
        };

        if (!process.Start())
            throw new Exception($"No se pudo iniciar el proceso: {executable}");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(true);
            throw;
        }
    }
}
