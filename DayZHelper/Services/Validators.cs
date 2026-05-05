using System.Net;
using System.Text.RegularExpressions;

namespace DayZHelper.Services;

public static class Validators
{
    private static readonly Regex Label =
        new(@"^(?!-)[A-Za-z0-9-]{1,63}(?<!-)$", RegexOptions.Compiled);

    public static bool IsValidHostname(string host)
    {
        if (string.IsNullOrEmpty(host) || host.Length > 253) return false;
        var labels = host.TrimEnd('.').Split('.');
        return labels.All(l => Label.IsMatch(l));
    }

    public static (bool ok, string? error) ValidateIpPort(string ip, string port)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return (false, "Please enter an IP or hostname.");

        if (!IPAddress.TryParse(ip, out _) && !IsValidHostname(ip))
            return (false, $"'{ip}' is not a valid IP address or hostname.");

        if (!int.TryParse(port, out var p))
            return (false, "Port must be a number.");

        if (p is < 1 or > 65535)
            return (false, "Port must be between 1 and 65535.");

        return (true, null);
    }
}