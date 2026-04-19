# MediaDownloader

Aplicación de escritorio moderna para descargar y convertir contenido multimedia desde URLs públicas.

## Características
- **Descarga Inteligente**: Obtiene metadatos (título, duración) antes de descargar.
- **Formatos**: Soporte para MP4 (Video) y MP3 (Solo Audio).
- **Recorte**: Permite especificar tiempos de inicio y fin para recortes precisos.
- **Historial**: Seguimiento de descargas realizadas en la sesión actual.
- **Arquitectura**: Construido con .NET 8, Avalonia UI y MVVM estricto.

## Requisitos
- **.NET 8 SDK**
- **Herramientas**: `yt-dlp.exe` y `ffmpeg.exe` deben estar en la carpeta `Tools/` del directorio de ejecución.

## Estructura del Proyecto
- `Models/`: Entidades de datos.
- `ViewModels/`: Lógica de presentación.
- `Views/`: Interfaz de usuario.
- `Services/`: Servicios para interactuar con herramientas externas.
- `Helpers/`: Utilidades comunes.

## Ejecución
```bash
dotnet run
```
