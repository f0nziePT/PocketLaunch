namespace PocketLaunch.Models;

public class ShortcutData
{
    public int Version { get; set; } = 1;
    public List<Shortcut> Shortcuts { get; set; } = new();
}
