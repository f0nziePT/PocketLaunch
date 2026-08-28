using System.ComponentModel;
using System.Windows.Media;

namespace PocketLaunch.Models;

public enum ShortcutType
{
    Folder,
    Application
}

public class Shortcut : INotifyPropertyChanged
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ShortcutType Type { get; set; } = ShortcutType.Folder;
    public int Order { get; set; }

    private ImageSource? _icon;
    [System.Text.Json.Serialization.JsonIgnore]
    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
