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
    public int Order { get; set; }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    private string _path = string.Empty;
    public string Path
    {
        get => _path;
        set => SetField(ref _path, value);
    }

    private ShortcutType _type = ShortcutType.Folder;
    public ShortcutType Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    private ImageSource? _icon;
    [System.Text.Json.Serialization.JsonIgnore]
    public ImageSource? Icon
    {
        get => _icon;
        set => SetField(ref _icon, value);
    }

    private bool _isDropTargetAbove;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDropTargetAbove
    {
        get => _isDropTargetAbove;
        set => SetField(ref _isDropTargetAbove, value);
    }

    private bool _isDropTargetBelow;
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDropTargetBelow
    {
        get => _isDropTargetBelow;
        set => SetField(ref _isDropTargetBelow, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
