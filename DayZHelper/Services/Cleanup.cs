using System.IO;

namespace DayZHelper.Services;

public sealed record CleanupPlan(IReadOnlyList<FileInfo> Files, long TotalBytes);

public sealed record CleanupResult(int Deleted, long Bytes, IReadOnlyList<string> Failures);

public static class Cleanup
{
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".log", ".rpt", ".mdmp" };

    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DayZHelper");

    private static readonly string LogPath = Path.Combine(LogDir, "cleanup.log");

    public static CleanupPlan Scan(params string[] folders)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<FileInfo>();
        foreach (var folder in folders.Where(f => !string.IsNullOrEmpty(f) && Directory.Exists(f)))
        {
            try
            {
                foreach (var f in new DirectoryInfo(folder)
                             .EnumerateFiles("*", SearchOption.AllDirectories)
                             .Where(f => Extensions.Contains(f.Extension)))
                {
                    if (seen.Add(f.FullName)) files.Add(f);
                }
            }
            catch
            {
                // Some workshop subfolders may be locked; skip them.
            }
        }

        long total = 0;
        foreach (var f in files)
        {
            try
            {
                total += f.Length;
            }
            catch
            {
                /* file vanished */
            }
        }

        return new CleanupPlan(files, total);
    }

    public static CleanupResult Delete(IEnumerable<FileInfo> files, bool writeLog = true)
    {
        var failures = new List<string>();
        var deletedNames = new List<string>();
        long bytes = 0;
        int count = 0;
        foreach (var f in files)
        {
            try
            {
                long size = f.Exists ? f.Length : 0;
                f.Delete();
                bytes += size;
                count++;
                deletedNames.Add($"{f.FullName}\t{size}");
            }
            catch (Exception ex)
            {
                failures.Add($"{f.Name}: {ex.Message}");
            }
        }

        if (writeLog && (count > 0 || failures.Count > 0))
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                using var sw = new StreamWriter(LogPath, append: true);
                sw.WriteLine(
                    $"--- {DateTime.Now:yyyy-MM-dd HH:mm:ss}  deleted={count} bytes={bytes} failed={failures.Count} ---");
                foreach (var d in deletedNames) sw.WriteLine("DEL\t" + d);
                foreach (var f in failures) sw.WriteLine("ERR\t" + f);
            }
            catch
            {
                /* logging is best-effort */
            }
        }

        return new CleanupResult(count, bytes, failures);
    }

    public static string LogFilePath => LogPath;

    public static string FormatSize(long size) => size switch
    {
        >= 1073741824L => $"{size / 1073741824.0:0.00} GB",
        >= 1048576L => $"{size / 1048576.0:0.00} MB",
        >= 1024L => $"{size / 1024.0:0.00} KB",
        _ => $"{size} bytes"
    };

    public static string GuessWorkshopFolder()
    {
        // Common Steam library workshop path; user can still configure manually.
        var candidates = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\workshop\content\221100",
            @"C:\Program Files\Steam\steamapps\workshop\content\221100",
            @"D:\SteamLibrary\steamapps\workshop\content\221100",
            @"E:\SteamLibrary\steamapps\workshop\content\221100"
        };
        return candidates.FirstOrDefault(Directory.Exists) ?? "";
    }
}