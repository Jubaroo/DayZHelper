using System.Windows;
using System.Windows.Media;
using DayZHelper.Services;

namespace DayZHelper;

public partial class App : Application
{
    public static SingleInstance? SingleInstance { get; private set; }
    public static AppConfig Config { get; private set; } = new();

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        SingleInstance = new SingleInstance();
        if (!SingleInstance.IsFirstInstance)
        {
            SingleInstance.NotifyExisting();
            Shutdown();
            return;
        }

        Config = AppConfig.Load();

        var mode = Config.ThemeMode switch
        {
            "Dark" => AppTheme.Dark,
            "Light" => AppTheme.Light,
            _ => AppTheme.System
        };
        var accent = TryParseHex(Config.AccentHex) ?? Color.FromRgb(0xE4, 0x58, 0x26);
        ThemeManager.Apply(mode, accent);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        SingleInstance.StartListener(() =>
        {
            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SingleInstance?.Dispose();
        base.OnExit(e);
    }

    public static Color? TryParseHex(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return null;
        }
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}