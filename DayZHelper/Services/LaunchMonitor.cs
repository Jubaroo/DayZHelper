using System.Diagnostics;

namespace DayZHelper.Services;

public static class LaunchMonitor
{
    private static readonly string[] ProcessNames = { "DayZ_x64", "DayZ_BE", "DayZDiag_x64" };

    public static bool IsDayzRunning()
    {
        foreach (var name in ProcessNames)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0) return true;
            }
            catch
            {
                /* ignore */
            }
        }

        return false;
    }

    /// <summary>Returns true if DayZ was detected within timeout.</summary>
    public static async Task<bool> WaitForLaunchAsync(int timeoutMs = 30000,
        CancellationToken ct = default)
    {
        var deadline = Environment.TickCount + timeoutMs;
        while (Environment.TickCount < deadline && !ct.IsCancellationRequested)
        {
            if (IsDayzRunning()) return true;
            await Task.Delay(750, ct);
        }

        return false;
    }
}