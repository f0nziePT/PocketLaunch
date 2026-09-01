using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PocketLaunch.Models;
using PocketLaunch.Services;
using PocketLaunch.Views;
using Border = System.Windows.Controls.Border;
using MessageBox = System.Windows.MessageBox;
using Shortcut = PocketLaunch.Models.Shortcut;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Size = System.Windows.Size;

namespace PocketLaunch;

public partial class MainWindow : Window
{
    private readonly ShortcutRepository _repository = new();
    private readonly BackupService _backupService = new();
    private readonly ObservableCollection<Shortcut> _shortcuts = new();
    private ShortcutData _data = new();

    private Point _dragStartPoint;
    private Shortcut? _dragCandidate;
    private Border? _dragSourceBorder;
    private Point _dragGrabOffset;

    private bool _isDragging;
    private bool _justDragged;
    private DragVisualAdorner? _dragAdorner;
    private AdornerLayer? _dragAdornerLayer;
    private Shortcut? _dropTarget;
    private bool _dropBefore;

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
        if (sender is FrameworkElement { Tag: Shortcut shortcut })
        {
            _shortcuts.Remove(shortcut);
            SaveShortcuts();
            UpdateEmptyState();
        }
    }

    private void EditShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Shortcut shortcut }) return;

        var dialog = new AddShortcutDialog(shortcut) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            SaveShortcuts();
            var icon = ShellIconService.GetIcon(shortcut.Path);
            if (icon is not null) shortcut.Icon = icon;
        }
    }

    private void ShortcutCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (_justDragged)
        {
            _justDragged = false;
            return;
        }

        if (sender is Border { Tag: Shortcut shortcut })
        {
            Launch(shortcut);
        }
    }

    private void ShortcutCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;

        _dragStartPoint = e.GetPosition(this);
        _dragGrabOffset = e.GetPosition(border);
        _dragCandidate = border.Tag as Shortcut;
        _dragSourceBorder = border;
    }

    private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragCandidate is null) return;

        var current = e.GetPosition(this);

        if (!_isDragging)
        {
            if (Math.Abs(current.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            StartDrag();
        }

        UpdateDrag(current);
    }

    private void MainWindow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            FinishDrag();
            e.Handled = true;
        }

        _dragCandidate = null;
        _dragSourceBorder = null;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_isDragging && e.Key == Key.Escape)
        {
            CancelDrag();
            e.Handled = true;
        }
    }

    private void StartDrag()
    {
        if (_dragSourceBorder is null || _dragCandidate is null) return;

        _isDragging = true;
        CaptureMouse();

        _dragSourceBorder.Opacity = 0.25;

        _dragAdornerLayer = AdornerLayer.GetAdornerLayer(RootBorder);
        var size = new Size(_dragSourceBorder.ActualWidth, _dragSourceBorder.ActualHeight);
        _dragAdorner = new DragVisualAdorner(RootBorder, _dragSourceBorder, size);
        _dragAdornerLayer?.Add(_dragAdorner);
    }

    private void UpdateDrag(Point currentPositionInWindow)
    {
        if (_dragAdorner is null || _dragSourceBorder is null) return;

        var posInRoot = TranslatePoint(currentPositionInWindow, RootBorder);
        _dragAdorner.UpdatePosition(new Point(posInRoot.X - _dragGrabOffset.X, posInRoot.Y - _dragGrabOffset.Y));

        UpdateDropIndicator(currentPositionInWindow);
    }

    private void UpdateDropIndicator(Point currentPositionInWindow)
    {
        ClearDropIndicator();

        var posInList = TranslatePoint(currentPositionInWindow, ShortcutsList);
        var hit = VisualTreeHelper.HitTest(ShortcutsList, posInList)?.VisualHit;

        Border? hoveredBorder = null;
        while (hit is not null)
        {
            if (hit is Border { Tag: Shortcut } b)
            {
                hoveredBorder = b;
                break;
            }
            hit = VisualTreeHelper.GetParent(hit);
        }

        if (hoveredBorder is null || hoveredBorder.Tag is not Shortcut target || ReferenceEquals(target, _dragCandidate))
        {
            return;
        }

        var posInCard = TranslatePoint(currentPositionInWindow, hoveredBorder);
        var before = posInCard.Y < hoveredBorder.ActualHeight / 2;

        _dropTarget = target;
        _dropBefore = before;

        if (before) target.IsDropTargetAbove = true;
        else target.IsDropTargetBelow = true;
    }

    private void ClearDropIndicator()
    {
        if (_dropTarget is null) return;
        _dropTarget.IsDropTargetAbove = false;
        _dropTarget.IsDropTargetBelow = false;
        _dropTarget = null;
    }

    private void FinishDrag()
    {
        var dragged = _dragCandidate;
        var target = _dropTarget;
        var before = _dropBefore;

        EndDragVisuals();

        if (dragged is not null && target is not null && !ReferenceEquals(dragged, target))
        {
            var oldIndex = _shortcuts.IndexOf(dragged);
            var targetIndex = _shortcuts.IndexOf(target);
            if (oldIndex >= 0 && targetIndex >= 0)
            {
                var newIndex = before ? targetIndex : targetIndex + 1;
                if (newIndex > oldIndex) newIndex--;
                _shortcuts.Move(oldIndex, Math.Clamp(newIndex, 0, _shortcuts.Count - 1));
                SaveShortcuts();
            }
        }

        _justDragged = true;
    }

    private void CancelDrag()
    {
        EndDragVisuals();
        ReleaseMouseCapture();
    }

    private void EndDragVisuals()
    {
        ClearDropIndicator();

        if (_dragAdornerLayer is not null && _dragAdorner is not null)
        {
            _dragAdornerLayer.Remove(_dragAdorner);
        }
        _dragAdorner = null;
        _dragAdornerLayer = null;

        if (_dragSourceBorder is not null) _dragSourceBorder.Opacity = 1;

        ReleaseMouseCapture();
        _isDragging = false;
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
