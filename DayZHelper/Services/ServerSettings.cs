using System.IO;
using System.Text.RegularExpressions;

namespace DayZHelper.Services;

public sealed record LastServer(string Name, string Ip, int Port);

public static class ServerSettings
{
    private static readonly Regex ServerRx =
        new(@"lastMPServer=""([^"":]+):(\d+)""", RegexOptions.Compiled);

    private static readonly Regex NameRx =
        new(@"lastMPServerName=""([^""]+)""", RegexOptions.Compiled);

    public static string GetSettingsFile(string dayzFolder) =>
        Path.Combine(dayzFolder, $"{Environment.UserName}_settings.DayZProfile");

    public static LastServer? TryRead(string dayzFolder, out string? error)
    {
        error = null;
        var file = GetSettingsFile(dayzFolder);
        if (!File.Exists(file))
        {
            error = $"Settings file not found:\n{file}";
            return null;
        }

        string content;
        try
        {
            content = File.ReadAllText(file);
        }
        catch (Exception ex)
        {
            error = $"Failed to read settings file:\n{ex.Message}";
            return null;
        }

        var srv = ServerRx.Match(content);
        if (!srv.Success)
        {
            error = "Server details not found in settings file.";
            return null;
        }

        var ip = srv.Groups[1].Value;
        if (!int.TryParse(srv.Groups[2].Value, out var rawPort))
        {
            error = "Could not parse port.";
            return null;
        }

        // DayZ sometimes encodes the port in the high 16 bits.
        if (rawPort > 65535) rawPort >>= 16;
        if (rawPort is < 1 or > 65535)
        {
            error = "Port out of range.";
            return null;
        }

        var name = NameRx.Match(content) is { Success: true } n
            ? n.Groups[1].Value
            : "(Unknown)";

        return new LastServer(name, ip, rawPort);
    }
}
