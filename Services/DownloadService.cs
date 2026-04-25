using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaDownloader.Helpers;
using MediaDownloader.Models;
using Newtonsoft.Json.Linq;

namespace MediaDownloader.Services;

public interface IDownloadService
{
    Task<MediaInfo> GetMediaInfoAsync(string url, CancellationToken ct);
    Task DownloadAsync(string url, string format, string outputPath,
                       IProgress<double> progress, CancellationToken ct);
}

public class DownloadService : IDownloadService
{
    private readonly string _ytDlpPath;

    public DownloadService()
    {
        _ytDlpPath = Helpers.ToolFinder.FindTool("yt-dlp.exe");
    }

    public async Task<MediaInfo> GetMediaInfoAsync(string url, CancellationToken ct)
    {
        string jsonOutput = "";
        int exitCode = await ProcessHelper.RunAsync(
            _ytDlpPath,
            $"--dump-json \"{url}\"",
            line => jsonOutput += line + Environment.NewLine,
            ct);

        if (exitCode != 0 && string.IsNullOrWhiteSpace(jsonOutput))
            throw new Exception($"yt-dlp falló con código {exitCode}. Verifica la URL.");

        try
        {
            // yt-dlp puede imprimir múltiples líneas, algunas pueden no ser JSON (advertencias, etc.)
            // Buscamos la primera línea que sea un objeto JSON válido.
            string[] lines = jsonOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            JObject? json = null;

            foreach (var line in lines)
            {
                try
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                    {
                        json = JObject.Parse(trimmed);
                        break;
                    }
                }
                catch { /* Continuar buscando */ }
            }

            if (json == null)
                throw new Exception($"No se encontró una respuesta JSON de yt-dlp. Salida obtenida: {(jsonOutput.Length > 100 ? jsonOutput.Substring(0, 100) : jsonOutput)}");
            
            return new MediaInfo
            {
                Title = json["title"]?.ToString() ?? "Desconocido",
                Duration = json["duration_string"]?.ToString() ?? "00:00",
                Formats = new List<string> { "MP4", "MP3" }
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al procesar la respuesta: {ex.Message}");
        }
    }

    public async Task DownloadAsync(string url, string format, string outputPath,
                                   IProgress<double> progress, CancellationToken ct)
    {
        string ffmpegPath = Helpers.ToolFinder.FindTool("ffmpeg.exe");
        string ffmpegDir = Path.GetDirectoryName(ffmpegPath);
        
        string ffmpegOption = !string.IsNullOrEmpty(ffmpegDir) && Directory.Exists(ffmpegDir) 
            ? $"--ffmpeg-location \"{ffmpegDir}\"" 
            : "";

        string args;
        if (format.ToUpper() == "MP3")
        {
            // Descargar solo audio y convertir a mp3 usando ffmpeg
            // --no-part: No crear archivos .part temporales
            // --restrict-filenames: Evitar caracteres raros en archivos temporales
            args = $"-x --audio-format mp3 {ffmpegOption} --newline --no-part --restrict-filenames -o \"{outputPath}\" \"{url}\"";
        }
        else
        {
            // Descargar video MP4 y unir streams usando ffmpeg
            args = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]\" {ffmpegOption} --newline --no-part --restrict-filenames -o \"{outputPath}\" \"{url}\"";
        }

        await ProcessHelper.RunAsync(
            _ytDlpPath,
            args,
            line =>
            {
                var match = Regex.Match(line, @"\[download\]\s+([\d.]+)%");
                if (match.Success && double.TryParse(match.Groups[1].Value, out var pct))
                {
                    progress.Report(pct);
                }
            },
            ct);
    }
}
