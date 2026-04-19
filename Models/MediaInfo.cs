using System.Collections.Generic;

namespace MediaDownloader.Models;

/// <summary>
/// Información básica del medio obtenida de la URL.
/// </summary>
public class MediaInfo
{
    public string Title { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public List<string> Formats { get; set; } = new();
}
