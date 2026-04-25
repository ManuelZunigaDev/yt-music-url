using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MediaDownloader.Models;
using MediaDownloader.Services;

namespace MediaDownloader.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDownloadService _downloadService;
    private readonly IFFmpegService _ffmpegService;
    private CancellationTokenSource? _cts;

    // Propiedades de la UI
    private string _url = string.Empty;
    private MediaInfo? _mediaInfo;
    private string _format = "MP4"; // "MP4" o "MP3"
    private string _startTime = "";
    private string _endTime = "";
    private string _outputFolder = "";
    private double _totalProgress;
    private bool _isBusy;
    private string _statusMessage = "Bienvenido a XDisk - Listo para descargar";
    private bool _isDownloadComplete;
    private bool _hasError;

    public MainWindowViewModel()
    {
        _downloadService = new DownloadService();
        _ffmpegService = new FFmpegService();
        History = new ObservableCollection<DownloadTask>();
        _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        GetInfoCommand = new AsyncRelayCommand(GetInfoAsync, () => !string.IsNullOrWhiteSpace(Url));
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, () => MediaInfo != null && !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        SelectFolderCommand = new AsyncRelayCommand(SelectFolderAsync);

        CheckTools();
    }

    private void CheckTools()
    {
        string ytDlp = Helpers.ToolFinder.FindTool("yt-dlp.exe");
        string ffmpeg = Helpers.ToolFinder.FindTool("ffmpeg.exe");

        bool ytDlpExists = File.Exists(ytDlp);
        bool ffmpegExists = File.Exists(ffmpeg);

        if (!ytDlpExists)
        {
            StatusMessage = "ERROR: No se encontró yt-dlp.exe. Por favor, colócalo en la carpeta del programa.";
            HasError = true;
        }
        else if (!ffmpegExists)
        {
            StatusMessage = "ADVERTENCIA: No se encontró ffmpeg.exe. Las conversiones a MP3 y los recortes podrían fallar.";
        }
    }

    #region Properties
    public string Url
    {
        get => _url;
        set 
        { 
            if (SetField(ref _url, value?.Trim() ?? string.Empty)) 
            {
                MediaInfo = null;
                ((AsyncRelayCommand)GetInfoCommand).RaiseCanExecuteChanged(); 
            }
        }
    }

    public MediaInfo? MediaInfo
    {
        get => _mediaInfo;
        set { if (SetField(ref _mediaInfo, value)) ((AsyncRelayCommand)DownloadCommand).RaiseCanExecuteChanged(); }
    }

    public string Format
    {
        get => _format;
        set => SetField(ref _format, value);
    }

    public string StartTime
    {
        get => _startTime;
        set => SetField(ref _startTime, value);
    }

    public string EndTime
    {
        get => _endTime;
        set => SetField(ref _endTime, value);
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set => SetField(ref _outputFolder, value);
    }

    public double TotalProgress
    {
        get => _totalProgress;
        set => SetField(ref _totalProgress, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set 
        { 
            if (SetField(ref _isBusy, value))
            {
                ((AsyncRelayCommand)DownloadCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsDownloadComplete
    {
        get => _isDownloadComplete;
        set => SetField(ref _isDownloadComplete, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetField(ref _hasError, value);
    }

    public ObservableCollection<DownloadTask> History { get; }
    #endregion

    #region Commands
    public ICommand GetInfoCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectFolderCommand { get; }
    #endregion

    private async Task GetInfoAsync()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
        {
            StatusMessage = "URL no válida";
            return;
        }

        try
        {
            IsBusy = true;
            MediaInfo = null; // Limpiar información previa
            StatusMessage = "Analizando contenido...";
            
            MediaInfo = await _downloadService.GetMediaInfoAsync(Url, CancellationToken.None);
            StatusMessage = "Análisis completado";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DownloadAsync()
    {
        if (MediaInfo == null) return;

        _cts = new CancellationTokenSource();
        var task = new DownloadTask
        {
            Name = MediaInfo.Title,
            Date = DateTime.Now,
            Status = "Descargando",
            Progress = 0
        };

        History.Insert(0, task);
        IsBusy = true;
        TotalProgress = 0;
        IsDownloadComplete = false;
        HasError = false;

        try
        {
            string ext = Format == "MP3" ? "mp3" : "mp4";
            string safeTitle = string.Join("_", MediaInfo.Title.Split(Path.GetInvalidFileNameChars()));
            string tempPath = Path.Combine(OutputFolder, $"{safeTitle}_temp.{ext}");
            string finalPath = Path.Combine(OutputFolder, $"{safeTitle}.{ext}");

            // Si hay recorte, descargamos a temp y luego procesamos
            bool needsTrim = !string.IsNullOrWhiteSpace(StartTime) || !string.IsNullOrWhiteSpace(EndTime);
            string downloadPath = needsTrim ? tempPath : finalPath;

            StatusMessage = "Descargando...";
            var progressHandler = new Progress<double>(p => 
            {
                TotalProgress = p;
                task.Progress = p;
            });

            await _downloadService.DownloadAsync(Url, Format, downloadPath, progressHandler, _cts.Token);

            if (needsTrim)
            {
                StatusMessage = "Procesando recorte/conversión...";
                task.Status = "Procesando";
                
                string start = string.IsNullOrWhiteSpace(StartTime) ? "00:00:00" : StartTime;
                string end = string.IsNullOrWhiteSpace(EndTime) ? "" : EndTime;

                if (Format == "MP3")
                    await _ffmpegService.TrimAndConvertAsync(downloadPath, finalPath, start, end, _cts.Token);
                else
                    await _ffmpegService.TrimAsync(downloadPath, finalPath, start, end, _cts.Token);

                if (File.Exists(tempPath)) File.Delete(tempPath);
            }

            task.Status = "Completado";
            task.Progress = 100;
            StatusMessage = "¡Completado!";
            IsDownloadComplete = true;
        }
        catch (OperationCanceledException)
        {
            task.Status = "Cancelado";
            StatusMessage = "Operación cancelada";
        }
        catch (Exception ex)
        {
            task.Status = "Error";
            StatusMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
            _cts = null;
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
    }

    private async Task SelectFolderAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel != null)
            {
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Seleccionar carpeta de destino",
                    AllowMultiple = false
                });

                if (folders.Count > 0)
                {
                    OutputFolder = folders[0].Path.LocalPath;
                }
            }
        }
    }
}
