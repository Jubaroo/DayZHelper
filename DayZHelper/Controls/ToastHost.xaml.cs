using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DayZHelper.Controls;

public enum ToastKind { Info, Success, Warning, Error }

public partial class ToastHost : UserControl
{
    public ToastHost() => InitializeComponent();

    public void Show(string message, ToastKind kind = ToastKind.Info, int durationMs = 3500)
    {
        var border = BuildToast(message, kind);
        Items.Items.Add(border);

        // Slide in
        var slide = new DoubleAnimation
        {
            From = 30, To = 0,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(220)
        };
        ((TranslateTransform)border.RenderTransform).BeginAnimation(
            TranslateTransform.YProperty, slide);
        border.BeginAnimation(OpacityProperty, fade);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            DismissToast(border);
        };
        timer.Start();

        border.MouseLeftButtonDown += (_, _) =>
        {
            timer.Stop();
            DismissToast(border);
        };
    }

    private void DismissToast(Border border)
    {
        var fade = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromMilliseconds(180)
        };
        fade.Completed += (_, _) => Items.Items.Remove(border);
        border.BeginAnimation(OpacityProperty, fade);
    }

    private Border BuildToast(string message, ToastKind kind)
    {
        var (bgKey, fgKey, glyph) = kind switch
        {
            ToastKind.Success => ("SuccessBrush",  "TextBrush", "\uE73E"),
            ToastKind.Warning => ("WarningBrush",  "TextBrush", "\uE7BA"),
            ToastKind.Error   => ("DangerBrush",   "TextBrush", "\uEA39"),
            _                 => ("AccentBrush",   "TextBrush", "\uE946")
        };

        var iconStripe = new Border
        {
            Width = 4,
            Background = (Brush)Application.Current.Resources[bgKey],
            CornerRadius = new CornerRadius(2, 0, 0, 2)
        };

        var glyphText = new TextBlock
        {
            FontFamily = (FontFamily)Application.Current.Resources["IconFont"],
            FontSize = 16,
            Text = glyph,
            Margin = new Thickness(12, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources[bgKey]
        };

        var msg = new TextBlock
        {
            Text = message,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 360,
            Margin = new Thickness(0, 8, 14, 8),
            Foreground = (Brush)Application.Current.Resources[fgKey]
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(iconStripe, 0);
        Grid.SetColumn(glyphText, 1);
        Grid.SetColumn(msg, 2);
        grid.Children.Add(iconStripe);
        grid.Children.Add(glyphText);
        grid.Children.Add(msg);

        var card = new Border
        {
            Background = (Brush)Application.Current.Resources["SurfaceBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BorderBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            MinWidth = 260,
            Margin = new Thickness(0, 8, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Opacity = 0,
            RenderTransform = new TranslateTransform(0, 30),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 4,
                Opacity = 0.35,
                Color = Colors.Black
            },
            Child = grid
        };
        return card;
    }
}
