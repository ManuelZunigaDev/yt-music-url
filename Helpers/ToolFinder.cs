using System;
using System.IO;

namespace MediaDownloader.Helpers;

public static class ToolFinder
{
    public static string FindTool(string toolName)
    {
        // 1. Check Tools/ folder in execution directory
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", toolName);
        if (File.Exists(path)) return path;

        // 2. Check execution directory directly
        path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, toolName);
        if (File.Exists(path)) return path;

        // 3. Check some common parent folders during development (bin/Debug/net8.0/../../../)
        string? current = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 4; i++)
        {
            if (current == null) break;
            string parentPath = Path.Combine(current, toolName);
            if (File.Exists(parentPath)) return parentPath;
            current = Path.GetDirectoryName(current);
        }

        // 4. Fallback to just the tool name
        return toolName;
    }
}
