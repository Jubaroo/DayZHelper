using System.IO;
using Microsoft.Win32;

namespace DayZHelper.Services;

public static class DzsalProtocol
{
    public static string DefaultLauncherPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "DZSALauncher", "DZSALauncher.exe");
    }

    public static bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\dzsal");
            if (key == null) return false;
            return key.GetValue("") != null && key.GetValue("URL Protocol") != null;
        }
        catch
        {
            return false;
        }
    }

    public static (bool ok, string? error) Register()
    {
        var launcher = DefaultLauncherPath();
        if (!File.Exists(launcher))
            return (false,
                $"DZSALauncher.exe not found at:\n{launcher}\n\nInstall DZSA Launcher first.");

        try
        {
            using (var protoKey = Registry.CurrentUser
                       .CreateSubKey(@"Software\Classes\dzsal", true))
            {
                protoKey!.SetValue("", "URL: DZSAL Protocol");
                protoKey.SetValue("URL Protocol", "");
            }

            using (var cmdKey = Registry.CurrentUser
                       .CreateSubKey(@"Software\Classes\dzsal\shell\open\command", true))
            {
                cmdKey!.SetValue("", $"\"{launcher}\" \"%1\"");
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Could not register dzsal:// protocol:\n{ex.Message}");
        }
    }
}
