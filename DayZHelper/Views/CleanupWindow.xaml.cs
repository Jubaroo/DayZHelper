using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using DayZHelper.Services;

namespace DayZHelper.Views;

public partial class CleanupWindow : Window
{
    public sealed class Item : INotifyPropertyChanged
    {
        public FileInfo File { get; }
        public string Name => File.Name;
        public string Path => File.FullName;
        public long Size { get; }
        public string SizeText => Cleanup.FormatSize(Size);

        private bool _selected = true;
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                OnPropertyChanged();
                Owner?.UpdateSummary();
            }
        }

        internal CleanupWindow? Owner { get; set; }

        public Item(FileInfo file)
        {
            File = file;
            try { Size = file.Length; } catch { Size = 0; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public ObservableCollection<Item> Files { get; } = new();
    public IReadOnlyList<FileInfo> SelectedForDeletion { get; private set; } =
        Array.Empty<FileInfo>();

    public CleanupWindow(IEnumerable<FileInfo> files)
    {
        InitializeComponent();
        foreach (var f in files)
        {
            var item = new Item(f) { Owner = this };
            Files.Add(item);
        }
        FileList.ItemsSource = Files;
        UpdateSummary();
    }

    internal void UpdateSummary()
    {
        var selected = Files.Where(f => f.Selected).ToList();
        var bytes = selected.Sum(f => f.Size);
        SelectionSummary.Text =
            $"{selected.Count} of {Files.Count} selected — {Cleanup.FormatSize(bytes)}";
        TotalText.Text =
            $"Total: {Files.Count} files ({Cleanup.FormatSize(Files.Sum(f => f.Size))})";
        DeleteButton.IsEnabled = selected.Count > 0;

        // Sync select-all without retriggering
        SelectAllBox.IsChecked = selected.Count == Files.Count
            ? true
            : selected.Count == 0 ? false : (bool?)null;
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        var target = SelectAllBox.IsChecked == true;
        foreach (var f in Files) f.Selected = target;
        UpdateSummary();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        SelectedForDeletion = Files.Where(f => f.Selected)
            .Select(f => f.File).ToList();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
