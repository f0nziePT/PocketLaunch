using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using PocketLaunch.Models;
using PocketLaunch.Services;
using PocketLaunch.Views;
using MessageBox = System.Windows.MessageBox;
using Shortcut = PocketLaunch.Models.Shortcut;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PocketLaunch;

public partial class MainWindow : Window
{
    private readonly ShortcutRepository _repository = new();
    private readonly BackupService _backupService = new();
    private readonly ObservableCollection<Shortcut> _shortcuts = new();
    private ShortcutData _data = new();

    public MainWindow()
    {
        InitializeComponent();
        ShortcutsList.ItemsSource = _shortcuts;
        LoadShortcuts();
    }

    private void LoadShortcuts()
    {
        _data = _repository.Load();
        _shortcuts.Clear();
        foreach (var shortcut in _data.Shortcuts.OrderBy(s => s.Order))
        {
            _shortcuts.Add(shortcut);
        }

        UpdateEmptyState();
        LoadIconsAsync();
    }

    private void LoadIconsAsync()
    {
        var targets = _shortcuts.ToList();
        _ = Task.Run(() =>
        {
            // SHGetFileInfo is not safe to call concurrently from multiple threads,
            // so icons are fetched one at a time on this single background thread.
            foreach (var target in targets)
            {
                var icon = ShellIconService.GetIcon(target.Path);
                if (icon is null) continue;
                Dispatcher.Invoke(() => target.Icon = icon);
            }
        });
    }

    private void UpdateEmptyState()
    {
        EmptyStateText.Visibility = _shortcuts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveShortcuts()
    {
        _data.Shortcuts = _shortcuts.ToList();
        for (int i = 0; i < _data.Shortcuts.Count; i++)
        {
            _data.Shortcuts[i].Order = i;
        }
        _repository.Save(_data);
    }

    public void ShowNearTray()
    {
        var workArea = System.Windows.SystemParameters.WorkArea;
        Left = workArea.Right - Width - 12;
        Top = workArea.Bottom - Height - 12;

        Show();
        Activate();
        Topmost = true;
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Hide();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddShortcutDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is not null)
        {
            _shortcuts.Add(dialog.Result);
            SaveShortcuts();
            UpdateEmptyState();

            var icon = ShellIconService.GetIcon(dialog.Result.Path);
            if (icon is not null) dialog.Result.Icon = icon;
        }
    }

    private void RemoveShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: Shortcut shortcut })
        {
            _shortcuts.Remove(shortcut);
            SaveShortcuts();
            UpdateEmptyState();
        }
    }

    private void ShortcutCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Border { Tag: Shortcut shortcut })
        {
            Launch(shortcut);
        }
    }

    private void Launch(Shortcut shortcut)
    {
        try
        {
            var psi = new ProcessStartInfo(shortcut.Path) { UseShellExecute = true };
            Process.Start(psi);
            Hide();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open '{shortcut.Name}':\n{ex.Message}",
                "PocketLaunch", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export PocketLaunch backup",
            Filter = "PocketLaunch backup (*.json)|*.json",
            FileName = "pocketlaunch-backup.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                SaveShortcuts();
                _backupService.Export(_data, dialog.FileName);
                MessageBox.Show(this, "Backup exported successfully.", "PocketLaunch",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "PocketLaunch",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import PocketLaunch backup",
            Filter = "PocketLaunch backup (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var imported = _backupService.Import(dialog.FileName);

            var choice = MessageBox.Show(this,
                "Replace the current list with the imported backup?\n\nYes = Replace\nNo = Merge (append new items)",
                "Import backup", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (choice == MessageBoxResult.Cancel) return;

            if (choice == MessageBoxResult.Yes)
            {
                _shortcuts.Clear();
                foreach (var s in imported.Shortcuts.OrderBy(s => s.Order)) _shortcuts.Add(s);
            }
            else
            {
                var existingPaths = _shortcuts.Select(s => s.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var s in imported.Shortcuts.Where(s => !existingPaths.Contains(s.Path)))
                {
                    _shortcuts.Add(s);
                }
            }

            SaveShortcuts();
            UpdateEmptyState();
            LoadIconsAsync();

            MessageBox.Show(this, "Backup imported successfully.", "PocketLaunch",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Import failed:\n{ex.Message}", "PocketLaunch",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
