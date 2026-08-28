using System.IO;
using System.Text.Json;
using PocketLaunch.Models;

namespace PocketLaunch.Services;

public class ShortcutRepository
{
    private static readonly string AppDataDir = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PocketLaunch");

    private static readonly string StoreFile = System.IO.Path.Combine(AppDataDir, "shortcuts.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ShortcutData Load()
    {
        if (!File.Exists(StoreFile))
        {
            return new ShortcutData();
        }

        try
        {
            var json = File.ReadAllText(StoreFile);
            return JsonSerializer.Deserialize<ShortcutData>(json, JsonOptions) ?? new ShortcutData();
        }
        catch
        {
            return new ShortcutData();
        }
    }

    public void Save(ShortcutData data)
    {
        Directory.CreateDirectory(AppDataDir);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(StoreFile, json);
    }
}
