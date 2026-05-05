using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DayZHelper.Controls;
using DayZHelper.Services;
using DayZHelper.Views;
using Microsoft.Win32;
using Shape = System.Windows.Shapes.Shape;

namespace DayZHelper;

public partial class MainWindow : Window
{
    // ----- View-models ----------------------------------------------------

    public sealed class FavoriteVm : INotifyPropertyChanged
    {
        public Favorite Model { get; }
        public string Name => Model.Name;
        public string Endpoint => $"{Model.Ip}:{Model.Port}";

        private string _pingText = "—";

        public string PingText
        {
            get => _pingText;
            set
            {
                _pingText = value;
                OnChanged();
            }
        }

        private Brush _pingBrush = Brushes.Gray;

        public Brush PingBrush
        {
            get => _pingBrush;
            set
            {
                _pingBrush = value;
                OnChanged();
            }
        }

        // -1 = unknown, -2 = offline, otherwise ms.
        public int PingMs { get; set; } = -1;

        public FavoriteVm(Favorite m) => Model = m;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public sealed class AccentVm
    {
        public string Name { get; }
        public Color Color { get; }
        public Brush Brush { get; }

        public AccentVm(string name, Color c)
        {
            Name = name;
            Color = c;
            Brush = new SolidColorBrush(c);
        }
    }

    private readonly ObservableCollection<FavoriteVm> _favorites = new();

    // ----- State -----------------------------------------------------------

    private readonly AppConfig _config;
    private string _dayzFolder;
    private string? _steamExe;
    private string _workshopFolder;
    private LastServer? _lastServer;
    private bool _suppressThemeChange = true;

    public MainWindow()
    {
        InitializeComponent();

        _config = App.Config;
        _dayzFolder = ResolveInitialDayzFolder();
        _steamExe = PathResolver.ResolveSteamExe(_config.SteamExe);
        _workshopFolder = !string.IsNullOrEmpty(_config.WorkshopFolder)
            ? _config.WorkshopFolder!
            : Cleanup.GuessWorkshopFolder();

        IpBox.Text = _config.LastDirectIp ?? "";
        PortBox.Text = _config.LastDirectPort ?? "";
        MonitorLaunchBox.IsChecked = _config.MonitorLaunch;

        FavoritesList.ItemsSource = _favorites;
        foreach (var f in _config.Favorites) _favorites.Add(new FavoriteVm(f));
        UpdateFavoritesEmptyState();

        AccentSwatches.ItemsSource = ThemeManager.AccentPresets
            .Select(p => new AccentVm(p.Name, p.Color)).ToList();

        ThemeCombo.SelectedIndex = _config.ThemeMode switch
        {
            "Dark" => 1,
            "Light" => 2,
            _ => 0
        };
        _suppressThemeChange = false;

        ThemeManager.ThemeChanged += OnThemeChanged;
        Loaded += MainWindow_Loaded;
        KeyDown += MainWindow_KeyDown;
        Closed += (_, _) => ThemeManager.ThemeChanged -= OnThemeChanged;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        ApplyWindowEffects();
    }

    private void OnThemeChanged()
    {
        ApplyWindowEffects();
        RefreshStatus();
        RefreshPingColors();
    }

    private void RefreshPingColors()
    {
        // Last-server ping dot
        if (_lastServer is null)
        {
            PingDot.SetResourceReference(Shape.FillProperty, "TextMutedBrush");
        }

        // Otherwise PingDot was set to a derived brush by PingLastServerAsync — re-derive on next ping.
        // Re-color favorites from their stored ms.
        foreach (var vm in _favorites)
        {
            vm.PingBrush = vm.PingMs switch
            {
                -1 => (Brush)FindResource("TextMutedBrush"),
                -2 => (Brush)FindResource("DangerBrush"),
                _ => PingBrush(vm.PingMs)
            };
        }
    }

    private void ApplyWindowEffects()
    {
        var bg = ((SolidColorBrush)FindResource("BgBrush")).Color;
        WindowEffects.Apply(this, ThemeManager.IsDark, bg);
    }

    private string ResolveInitialDayzFolder()
    {
        if (!string.IsNullOrEmpty(_config.DayzFolder) && Directory.Exists(_config.DayzFolder))
            return _config.DayzFolder!;
        return PathResolver.AutoDetectDayzFolder();
    }

    // ----- Loaded ----------------------------------------------------------

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AnimateContentIn();
        TryLoadLastServer(silent: true);
        RefreshStatus();

        if (_lastServer != null) _ = PingLastServerAsync();
        if (_favorites.Count > 0) _ = PingAllFavoritesAsync();

        PromptForDzsalIfNeeded();
    }

    private void AnimateContentIn()
    {
        var fade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(280),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slide = new DoubleAnimation
        {
            From = 12, To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ContentRoot.BeginAnimation(OpacityProperty, fade);
        ((TranslateTransform)ContentRoot.RenderTransform).BeginAnimation(
            TranslateTransform.YProperty, slide);
    }

    private void PromptForDzsalIfNeeded()
    {
        if (DzsalProtocol.IsRegistered()) return;
        if (_config.DzsalPromptDeclined) return;

        var result = MessageBox.Show(
            this,
            "The dzsal:// protocol is not registered.\nWould you like to register it now?",
            "Register dzsal Protocol",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            DoRegisterDzsal();
        }
        else
        {
            _config.DzsalPromptDeclined = true;
            _config.Save();
        }
    }

    // ----- Hotkeys ---------------------------------------------------------

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        if (ctrl && e.Key == Key.L)
        {
            CleanFilesButton_Click(sender, e);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.R)
        {
            ConnectOfficialButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            RefreshServerButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && MenuPopup.IsOpen)
        {
            MenuPopup.IsOpen = false;
            e.Handled = true;
        }
    }

    // ----- Status ----------------------------------------------------------

    private void RefreshStatus()
    {
        var steamTxt = !string.IsNullOrEmpty(_steamExe) ? "Steam OK" : "Steam not found";
        StatusText.Text = $"DayZ: {_dayzFolder}    |    {steamTxt}";

        if (string.IsNullOrEmpty(_steamExe))
        {
            StatusDot.SetResourceReference(Shape.FillProperty, "DangerBrush");
            StatusBadge.Text = "Steam missing";
        }
        else if (!Directory.Exists(_dayzFolder))
        {
            StatusDot.SetResourceReference(Shape.FillProperty, "DangerBrush");
            StatusBadge.Text = "DayZ folder missing";
        }
        else
        {
            StatusDot.SetResourceReference(Shape.FillProperty, "SuccessBrush");
            StatusBadge.Text = "Ready";
        }
    }

    private void UpdateLastServerUi()
    {
        if (_lastServer is null)
        {
            LastServerName.Text = "Not loaded yet";
            LastServerIp.Text = "—";
            LastServerPort.Text = "—";
            PingText.Text = "—";
            PingDot.SetResourceReference(Shape.FillProperty, "TextMutedBrush");
            return;
        }

        LastServerName.Text = _lastServer.Name;
        LastServerIp.Text = $"IP {_lastServer.Ip}";
        LastServerPort.Text = $"Port {_lastServer.Port}";
    }

    private void TryLoadLastServer(bool silent)
    {
        if (!Directory.Exists(_dayzFolder))
        {
            if (!silent) Toasts.Show($"DayZ folder not found: {_dayzFolder}", ToastKind.Error);
            return;
        }

        var server = ServerSettings.TryRead(_dayzFolder, out var error);
        if (server is null)
        {
            if (!silent && error != null) Toasts.Show(error, ToastKind.Warning);
            return;
        }

        _lastServer = server;
        UpdateLastServerUi();
    }

    private async Task PingLastServerAsync()
    {
        if (_lastServer is null) return;
        var ms = await ServerPing.PingAsync(_lastServer.Ip, _lastServer.Port);
        Dispatcher.Invoke(() =>
        {
            if (ms.HasValue)
            {
                PingText.Text = $"{ms.Value} ms";
                PingDot.Fill = PingBrush(ms.Value);
            }
            else
            {
                PingText.Text = "offline";
                PingDot.SetResourceReference(Shape.FillProperty, "DangerBrush");
            }
        });
    }

    private Brush PingBrush(int ms) => ms switch
    {
        < 80 => (Brush)FindResource("SuccessBrush"),
        < 160 => (Brush)FindResource("WarningBrush"),
        _ => (Brush)FindResource("DangerBrush")
    };

    // ----- Title bar -------------------------------------------------------

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        // Toggle maximize/restore geometry
        MaxButton.Content = WindowState == WindowState.Maximized
            ? FindResource("Geom.Restore")
            : FindResource("Geom.Max");
    }

    private void MinButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaxButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuButton_Click(object sender, RoutedEventArgs e) =>
        MenuPopup.IsOpen = !MenuPopup.IsOpen;

    // ----- Quick actions ---------------------------------------------------

    private async void StartDayzButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SteamLauncher.StartDayz();
            Toasts.Show("Launching DayZ via Steam…", ToastKind.Info);

            if (_config.MonitorLaunch)
            {
                StatusText.Text = "Waiting for DayZ to start…";
                var ok = await LaunchMonitor.WaitForLaunchAsync(20000);
                if (ok)
                {
                    Toasts.Show("DayZ is running. Bye!", ToastKind.Success, 2000);
                    await Task.Delay(800);
                    Close();
                }
                else
                {
                    Toasts.Show("DayZ didn't start in time. Check Steam.", ToastKind.Warning, 5000);
                    StatusText.Text = "Launch timeout.";
                }
            }
            else
            {
                await Task.Delay(500);
                Close();
            }
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not launch DayZ: {ex.Message}", ToastKind.Error);
        }
    }

    private void CleanFilesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(_dayzFolder))
        {
            Toasts.Show($"DayZ folder not found: {_dayzFolder}", ToastKind.Error);
            return;
        }

        var plan = Cleanup.Scan(_dayzFolder, _workshopFolder);
        if (plan.Files.Count == 0)
        {
            Toasts.Show("Nothing to clean.", ToastKind.Info);
            return;
        }

        var dlg = new CleanupWindow(plan.Files) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var result = Cleanup.Delete(dlg.SelectedForDeletion);
        var summary = $"Deleted {result.Deleted} file(s) — {Cleanup.FormatSize(result.Bytes)}";
        if (result.Failures.Count > 0)
            Toasts.Show($"{summary} ({result.Failures.Count} failed)",
                ToastKind.Warning, 5000);
        else
            Toasts.Show(summary, ToastKind.Success);
    }

    private void RefreshServerButton_Click(object sender, RoutedEventArgs e)
    {
        TryLoadLastServer(silent: false);
        if (_lastServer != null) _ = PingLastServerAsync();
    }

    // ----- Connect ---------------------------------------------------------

    private bool RequireSteam()
    {
        if (!string.IsNullOrEmpty(_steamExe) && File.Exists(_steamExe))
            return true;
        Toasts.Show("Steam.exe not found. Set it via the menu.", ToastKind.Error);
        return false;
    }

    private void ConnectOfficialButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastServer is null) TryLoadLastServer(silent: false);
        if (_lastServer is null) return;
        if (!RequireSteam()) return;

        try
        {
            SteamLauncher.StartServer(_steamExe!, _lastServer.Ip, _lastServer.Port);
            Toasts.Show($"Connecting to {_lastServer.Name}…", ToastKind.Info);
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not launch Steam: {ex.Message}", ToastKind.Error);
        }
    }

    private void ConnectDzsaButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastServer is null) TryLoadLastServer(silent: false);
        if (_lastServer is null) return;

        if (!DzsalProtocol.IsRegistered())
        {
            var ans = MessageBox.Show(this,
                "The dzsal:// protocol isn't registered. Register it now?",
                "dzsal:// not registered",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ans != MessageBoxResult.Yes) return;
            DoRegisterDzsal();
            if (!DzsalProtocol.IsRegistered()) return;
        }

        var url = $"dzsal://{_lastServer.Ip}:{_lastServer.Port}";
        try
        {
            SteamLauncher.OpenUrl(url);
            Toasts.Show("Opening in DZSA Launcher…", ToastKind.Info);
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not open dzsal:// link: {ex.Message}", ToastKind.Error);
        }
    }

    private void DirectConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var ip = IpBox.Text.Trim();
        var port = PortBox.Text.Trim();

        var (ok, error) = Validators.ValidateIpPort(ip, port);
        if (!ok)
        {
            Toasts.Show(error!, ToastKind.Error);
            return;
        }

        if (!RequireSteam()) return;

        _config.LastDirectIp = ip;
        _config.LastDirectPort = port;
        _config.Save();

        try
        {
            SteamLauncher.StartServer(_steamExe!, ip, int.Parse(port));
            Toasts.Show($"Connecting to {ip}:{port}…", ToastKind.Info);
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not launch Steam: {ex.Message}", ToastKind.Error);
        }
    }

    private void DirectInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) DirectConnectButton_Click(sender, e);
    }

    // ----- Favorites -------------------------------------------------------

    private void StarFavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastServer is null)
        {
            Toasts.Show("No server loaded.", ToastKind.Warning);
            return;
        }

        AddFavorite(_lastServer.Name, _lastServer.Ip, _lastServer.Port);
    }

    private void AddFavorite(string name, string ip, int port)
    {
        if (_favorites.Any(f => f.Model.Ip == ip && f.Model.Port == port))
        {
            Toasts.Show("Already in favorites.", ToastKind.Info);
            return;
        }

        var fav = new Favorite
        {
            Name = string.IsNullOrEmpty(name) ? $"{ip}:{port}" : name,
            Ip = ip, Port = port,
            LastPlayedUtc = DateTime.UtcNow.ToString("o")
        };
        _config.Favorites.Add(fav);
        _config.Save();
        var vm = new FavoriteVm(fav);
        _favorites.Add(vm);
        UpdateFavoritesEmptyState();
        _ = PingFavoriteAsync(vm);
        Toasts.Show($"Saved “{fav.Name}”", ToastKind.Success);
    }

    private void FavoriteRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FavoriteVm vm })
        {
            _favorites.Remove(vm);
            _config.Favorites.Remove(vm.Model);
            _config.Save();
            UpdateFavoritesEmptyState();
        }
    }

    private void FavoriteConnect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: FavoriteVm vm }) return;
        if (!RequireSteam()) return;
        try
        {
            SteamLauncher.StartServer(_steamExe!, vm.Model.Ip, vm.Model.Port);
            vm.Model.LastPlayedUtc = DateTime.UtcNow.ToString("o");
            _config.Save();
            Toasts.Show($"Connecting to {vm.Name}…", ToastKind.Info);
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not launch Steam: {ex.Message}", ToastKind.Error);
        }
    }

    private async void PingAllButton_Click(object sender, RoutedEventArgs e)
    {
        await PingAllFavoritesAsync();
    }

    private async Task PingAllFavoritesAsync()
    {
        var tasks = _favorites.Select(PingFavoriteAsync);
        await Task.WhenAll(tasks);
    }

    private async Task PingFavoriteAsync(FavoriteVm vm)
    {
        vm.PingText = "…";
        vm.PingMs = -1;
        vm.PingBrush = (Brush)FindResource("TextMutedBrush");
        var ms = await ServerPing.PingAsync(vm.Model.Ip, vm.Model.Port);
        if (ms.HasValue)
        {
            vm.PingMs = ms.Value;
            vm.PingText = $"{ms.Value} ms";
            vm.PingBrush = PingBrush(ms.Value);
        }
        else
        {
            vm.PingMs = -2;
            vm.PingText = "offline";
            vm.PingBrush = (Brush)FindResource("DangerBrush");
        }
    }

    private void UpdateFavoritesEmptyState()
    {
        NoFavoritesHint.Visibility = _favorites.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        FavoritesList.Visibility = _favorites.Count == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    // ----- Theme & accent --------------------------------------------------

    private void ThemeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressThemeChange) return;
        var mode = ThemeCombo.SelectedIndex switch
        {
            1 => AppTheme.Dark,
            2 => AppTheme.Light,
            _ => AppTheme.System
        };
        _config.ThemeMode = mode.ToString();
        _config.Save();
        ThemeManager.Apply(mode);
    }

    private void AccentSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AccentVm vm }) return;
        ThemeManager.Apply(ThemeManager.Mode, vm.Color);
        _config.AccentHex = App.ToHex(vm.Color);
        _config.Save();
    }

    private void MonitorLaunchBox_Changed(object sender, RoutedEventArgs e)
    {
        _config.MonitorLaunch = MonitorLaunchBox.IsChecked == true;
        _config.Save();
    }

    // ----- Menu actions ----------------------------------------------------

    private void MenuOpenDayzFolder_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (!Directory.Exists(_dayzFolder))
        {
            Toasts.Show($"DayZ folder not found: {_dayzFolder}", ToastKind.Error);
            return;
        }

        try
        {
            SteamLauncher.OpenPath(_dayzFolder);
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not open folder: {ex.Message}", ToastKind.Error);
        }
    }

    private void MenuOpenSettingsFile_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        var file = ServerSettings.GetSettingsFile(_dayzFolder);
        if (!File.Exists(file))
        {
            Toasts.Show($"Settings file not found: {file}", ToastKind.Error);
            return;
        }

        try
        {
            SteamLauncher.OpenPath(file);
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not open file: {ex.Message}", ToastKind.Error);
        }
    }

    private void MenuOpenCleanupLog_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        if (!File.Exists(Cleanup.LogFilePath))
        {
            Toasts.Show("No cleanup log yet.", ToastKind.Info);
            return;
        }

        try
        {
            SteamLauncher.OpenPath(Cleanup.LogFilePath);
        }
        catch (Exception ex)
        {
            Toasts.Show($"Could not open log: {ex.Message}", ToastKind.Error);
        }
    }

    private void MenuChangeDayzFolder_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        var dlg = new OpenFolderDialog
        {
            Title = "Select DayZ folder",
            InitialDirectory = Directory.Exists(_dayzFolder)
                ? _dayzFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog(this) != true) return;
        if (!Directory.Exists(dlg.FolderName))
        {
            Toasts.Show("Invalid folder.", ToastKind.Error);
            return;
        }

        _dayzFolder = dlg.FolderName;
        _config.DayzFolder = _dayzFolder;
        _config.Save();
        TryLoadLastServer(silent: true);
        RefreshStatus();
        Toasts.Show("DayZ folder updated.", ToastKind.Success);
    }

    private void MenuChangeSteamPath_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        var dlg = new OpenFileDialog
        {
            Title = "Select Steam.exe",
            Filter = "Steam executable|Steam.exe|Executables|*.exe",
            InitialDirectory = !string.IsNullOrEmpty(_steamExe)
                ? Path.GetDirectoryName(_steamExe)
                : @"C:\Program Files (x86)"
        };
        if (dlg.ShowDialog(this) != true) return;
        if (!string.Equals(Path.GetFileName(dlg.FileName), "Steam.exe",
                StringComparison.OrdinalIgnoreCase) || !File.Exists(dlg.FileName))
        {
            Toasts.Show("Please pick a valid Steam.exe.", ToastKind.Error);
            return;
        }

        _steamExe = dlg.FileName;
        _config.SteamExe = _steamExe;
        _config.Save();
        RefreshStatus();
        Toasts.Show("Steam path updated.", ToastKind.Success);
    }

    private void MenuChangeWorkshopFolder_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        var dlg = new OpenFolderDialog
        {
            Title = "Select Workshop folder (steamapps\\workshop\\content\\221100)",
            InitialDirectory = Directory.Exists(_workshopFolder)
                ? _workshopFolder
                : @"C:\Program Files (x86)\Steam\steamapps\workshop"
        };
        if (dlg.ShowDialog(this) != true) return;
        _workshopFolder = dlg.FolderName;
        _config.WorkshopFolder = _workshopFolder;
        _config.Save();
        Toasts.Show("Workshop folder updated.", ToastKind.Success);
    }

    private void MenuRegisterDzsal_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        DoRegisterDzsal();
    }

    private void DoRegisterDzsal()
    {
        var (ok, error) = DzsalProtocol.Register();
        if (ok) Toasts.Show("dzsal:// registered.", ToastKind.Success);
        else Toasts.Show(error ?? "Registration failed.", ToastKind.Error, 5000);
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        MenuPopup.IsOpen = false;
        MessageBox.Show(this,
            "DayZ Helper\nAuthor: Jarrod Schantz\nVersion: 1.1\n\nHotkeys:\n  Ctrl+L  Clean files\n  Ctrl+R  Reconnect last server\n  F5  Refresh server info",
            "About", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}