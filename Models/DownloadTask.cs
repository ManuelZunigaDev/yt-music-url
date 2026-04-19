using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaDownloader.Models;

/// <summary>
/// Representa una tarea de descarga en el historial.
/// </summary>
public class DownloadTask : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _status = "Pendiente";
    private double _progress;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Status
    {
        get => _status; // "Pendiente", "Descargando", "Completado", "Error"
        set => SetField(ref _status, value);
    }

    public DateTime Date { get; set; }

    public double Progress
    {
        get => _progress; // 0.0 - 100.0
        set => SetField(ref _progress, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
