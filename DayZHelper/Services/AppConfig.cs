using System.IO;
using System.Text.Json;

namespace DayZHelper.Services;

public sealed class AppConfig
{
    public string? DayzFolder { get; set; }
    public string? SteamExe { get; set; }
    public string? WorkshopFolder { get; set; }
    public string? LastDirectIp { get; set; }
    public string? LastDirectPort { get; set; }
    public bool DzsalPromptDeclined { get; set; }
    public bool MonitorLaunch { get; set; } = true;
    public string ThemeMode { get; set; } = "System"; // System | Dark | Light
    public string AccentHex { get; set; } = "#E45826";
    public List<Favorite> Favorites { get; set; } = new();

    private static readonly string AppName = "DayZHelper";

    private static string ConfigDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName);

    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new AppConfig();
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // best-effort
        }
    }
}