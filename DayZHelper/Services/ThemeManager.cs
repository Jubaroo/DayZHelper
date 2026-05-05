using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace DayZHelper.Services;

public enum AppTheme
{
    System,
    Dark,
    Light
}

public static class ThemeManager
{
    public static event Action? ThemeChanged;

    public static AppTheme Mode { get; private set; } = AppTheme.System;
    public static bool IsDark { get; private set; } = true;
    public static Color Accent { get; private set; } = Color.FromRgb(0xE4, 0x58, 0x26);

    private static ResourceDictionary? _accentDict;
    private static ResourceDictionary? _themeDict;

    public static readonly (string Name, Color Color)[] AccentPresets =
    {
        ("Orange", Color.FromRgb(0xE4, 0x58, 0x26)),
        ("Crimson", Color.FromRgb(0xE0, 0x48, 0x48)),
        ("Amber", Color.FromRgb(0xE6, 0xB4, 0x50)),
        ("Emerald", Color.FromRgb(0x42, 0xC9, 0x7A)),
        ("Cyan", Color.FromRgb(0x3D, 0xC2, 0xCC)),
        ("Blue", Color.FromRgb(0x4D, 0x8E, 0xFF)),
        ("Violet", Color.FromRgb(0x9B, 0x6B, 0xFF)),
        ("Pink", Color.FromRgb(0xE2, 0x68, 0xB3)),
    };

    public static void Apply(AppTheme mode, Color? accent = null)
    {
        Mode = mode;
        if (accent.HasValue) Accent = accent.Value;
        IsDark = ResolveIsDark(mode);

        SwapTheme(IsDark);
        SwapAccent(Accent);
        ThemeChanged?.Invoke();
    }

    public static void Refresh()
    {
        // Re-evaluate "System" without changing user preference.
        IsDark = ResolveIsDark(Mode);
        SwapTheme(IsDark);
        SwapAccent(Accent);
        ThemeChanged?.Invoke();
    }

    private static bool ResolveIsDark(AppTheme mode) => mode switch
    {
        AppTheme.Dark => true,
        AppTheme.Light => false,
        _ => IsSystemDark()
    };

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int v) return v == 0;
        }
        catch
        {
            /* ignore */
        }

        return true;
    }

    private static void SwapTheme(bool dark)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var newSrc = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml",
            UriKind.Relative);
        var newDict = new ResourceDictionary { Source = newSrc };

        if (_themeDict != null && dicts.Contains(_themeDict))
        {
            // Replace in place so the new dict keeps its priority slot.
            int idx = dicts.IndexOf(_themeDict);
            dicts.RemoveAt(idx);
            dicts.Insert(idx, newDict);
        }
        else
        {
            // First call (or dict was lost): try to replace any source-based theme dict.
            int found = -1;
            for (int i = 0; i < dicts.Count; i++)
            {
                if (dicts[i].Source is { } src &&
                    (src.OriginalString.EndsWith("Dark.xaml") ||
                     src.OriginalString.EndsWith("Light.xaml")))
                {
                    found = i;
                    break;
                }
            }

            if (found >= 0)
            {
                dicts.RemoveAt(found);
                dicts.Insert(found, newDict);
            }
            else
            {
                dicts.Insert(0, newDict);
            }
        }

        _themeDict = newDict;
    }

    private static void SwapAccent(Color accent)
    {
        var hover = Lighten(accent, 0.12);
        var pressed = Darken(accent, 0.18);
        var muted = Color.FromArgb(0x33, accent.R, accent.G, accent.B);

        var rd = new ResourceDictionary();
        rd["AccentColor"] = accent;
        rd["AccentHoverColor"] = hover;
        rd["AccentPressedColor"] = pressed;
        rd["AccentMutedColor"] = muted;
        rd["AccentBrush"] = new SolidColorBrush(accent);
        rd["AccentHoverBrush"] = new SolidColorBrush(hover);
        rd["AccentPressedBrush"] = new SolidColorBrush(pressed);
        rd["AccentMutedBrush"] = new SolidColorBrush(muted);

        var grad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        grad.GradientStops.Add(new GradientStop(hover, 0));
        grad.GradientStops.Add(new GradientStop(accent, 1));
        rd["AccentGradientBrush"] = grad;

        var dicts = Application.Current.Resources.MergedDictionaries;

        if (_accentDict != null && dicts.Contains(_accentDict))
        {
            int idx = dicts.IndexOf(_accentDict);
            dicts.RemoveAt(idx);
            dicts.Insert(idx, rd);
        }
        else
        {
            int found = -1;
            for (int i = 0; i < dicts.Count; i++)
            {
                if (dicts[i].Source is { } src &&
                    src.OriginalString.EndsWith("Accent.xaml"))
                {
                    found = i;
                    break;
                }
            }

            if (found >= 0)
            {
                dicts.RemoveAt(found);
                dicts.Insert(found, rd);
            }
            else
            {
                // Append to end so it has highest priority (last-merged wins).
                dicts.Add(rd);
            }
        }

        _accentDict = rd;
    }

    private static Color Lighten(Color c, double pct) => Color.FromRgb(
        (byte)Math.Min(255, c.R + 255 * pct),
        (byte)Math.Min(255, c.G + 255 * pct),
        (byte)Math.Min(255, c.B + 255 * pct));

    private static Color Darken(Color c, double pct) => Color.FromRgb(
        (byte)Math.Max(0, c.R - 255 * pct),
        (byte)Math.Max(0, c.G - 255 * pct),
        (byte)Math.Max(0, c.B - 255 * pct));
}