using System.IO;
using System.Text.Json;
using PocketLaunch.Models;

namespace PocketLaunch.Services;

public class BackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void Export(ShortcutData data, string filePath)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public ShortcutData Import(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ShortcutData>(json, JsonOptions)
            ?? throw new InvalidDataException("The selected file is not a valid PocketLaunch backup.");
    }
}
