using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Shortcut = PocketLaunch.Models.Shortcut;
using ShortcutType = PocketLaunch.Models.ShortcutType;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PocketLaunch.Views;

public partial class AddShortcutDialog : Window
{
    public Shortcut? Result { get; private set; }

    public AddShortcutDialog()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (FolderRadio.IsChecked == true)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathBox.Text = dialog.SelectedPath;
                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    NameBox.Text = new DirectoryInfo(dialog.SelectedPath).Name;
                }
            }
        }
        else
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select an application",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                PathBox.Text = dialog.FileName;
                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    NameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text.Trim();
        var name = NameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(path) || (!Directory.Exists(path) && !File.Exists(path)))
        {
            MessageBox.Show(this, "Please choose a valid folder or application path.",
                "PocketLaunch", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Please enter a name.",
                "PocketLaunch", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new Shortcut
        {
            Name = name,
            Description = DescriptionBox.Text.Trim(),
            Path = path,
            Type = FolderRadio.IsChecked == true ? ShortcutType.Folder : ShortcutType.Application
        };

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
