using System.Diagnostics;

namespace DayZHelper.Services;

public static class SteamLauncher
{
    public const string DayzAppId = "221100";

    public static void StartDayz()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"steam://rungameid/{DayzAppId}",
            UseShellExecute = true
        });
    }

    public static void StartServer(string steamExe, string ip, int port)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = steamExe,
            ArgumentList =
            {
                "-applaunch", DayzAppId,
                $"-connect={ip}",
                $"-port={port}"
            },
            UseShellExecute = false
        });
    }

    public static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    public static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
