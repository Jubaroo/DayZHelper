using System.IO;
using Microsoft.Win32;

namespace DayZHelper.Services;

public static class PathResolver
{
    private static readonly string[] SteamCandidates =
    {
        @"C:\Program Files (x86)\Steam\Steam.exe",
        @"C:\Program Files\Steam\Steam.exe"
    };

    public static string AutoDetectDayzFolder()
    {
        var docs = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Documents", "DayZ");
        if (Directory.Exists(docs)) return docs;

        var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
        if (!string.IsNullOrEmpty(oneDrive))
        {
            var alt = Path.Combine(oneDrive, "Documents", "DayZ");
            if (Directory.Exists(alt)) return alt;
        }
        return docs;
    }

    public static string? ResolveSteamExe(string? saved)
    {
        if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
            return saved;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamExe") is string regPath && File.Exists(regPath))
                return regPath;
        }
        catch
        {
            // ignore registry errors
        }

        foreach (var candidate in SteamCandidates)
            if (File.Exists(candidate)) return candidate;

        return null;
    }
}
