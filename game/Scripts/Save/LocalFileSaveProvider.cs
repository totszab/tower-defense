using System.IO;
using System.Text.Json;
using Godot;

namespace TowerDefense.Save;

public class LocalFileSaveProvider : ISaveProvider
{
    private static string SavePath => ProjectSettings.GlobalizePath("user://save.json");

    public PlayerProgress Load()
    {
        if (!File.Exists(SavePath))
        {
            return new PlayerProgress();
        }

        var json = File.ReadAllText(SavePath);
        return JsonSerializer.Deserialize<PlayerProgress>(json) ?? new PlayerProgress();
    }

    public void Save(PlayerProgress progress)
    {
        File.WriteAllText(SavePath, JsonSerializer.Serialize(progress));
    }
}
