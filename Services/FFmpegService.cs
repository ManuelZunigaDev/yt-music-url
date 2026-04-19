using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaDownloader.Helpers;

namespace MediaDownloader.Services;

public interface IFFmpegService
{
    Task ConvertToMp3Async(string inputPath, string outputPath, CancellationToken ct);
    Task TrimAsync(string inputPath, string outputPath,
                   string start, string end, CancellationToken ct);
    Task TrimAndConvertAsync(string inputPath, string outputPath,
                             string start, string end, CancellationToken ct);
}

public class FFmpegService : IFFmpegService
{
    private readonly string _ffmpegPath;

    public FFmpegService()
    {
        _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffmpeg.exe");
    }

    public async Task ConvertToMp3Async(string inputPath, string outputPath, CancellationToken ct)
    {
        string args = $"-i \"{inputPath}\" -q:a 0 -map a \"{outputPath}\" -y";
        await RunFFmpegAsync(args, ct);
    }

    public async Task TrimAsync(string inputPath, string outputPath,
                               string start, string end, CancellationToken ct)
    {
        string args = $"-i \"{inputPath}\" -ss {start} -to {end} -c copy \"{outputPath}\" -y";
        await RunFFmpegAsync(args, ct);
    }

    public async Task TrimAndConvertAsync(string inputPath, string outputPath,
                                         string start, string end, CancellationToken ct)
    {
        string args = $"-i \"{inputPath}\" -ss {start} -to {end} -q:a 0 \"{outputPath}\" -y";
        await RunFFmpegAsync(args, ct);
    }

    private async Task RunFFmpegAsync(string args, CancellationToken ct)
    {
        int exitCode = await ProcessHelper.RunAsync(
            _ffmpegPath,
            args,
            line => { /* No mostramos salida de ffmpeg en progreso por ahora */ },
            ct);

        if (exitCode != 0)
            throw new Exception("Error en la operación de FFmpeg.");
    }
}
