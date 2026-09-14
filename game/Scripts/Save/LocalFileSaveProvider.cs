using System.Collections.Generic;
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
        var progress = JsonSerializer.Deserialize<PlayerProgress>(json) ?? new PlayerProgress();

        MigrateLegacySingleLevelFields(json, progress);

        return progress;
    }

    // Régi (egy-pályás) mentésformátum: "HighestUnlockedRound"/"Level1Preset" lapos
    // mezők, mielőtt a PlayerProgress pálya-kulcsos map-re váltott (több pálya
    // támogatásához). Ezek a JSON kulcsok az új osztályon nem léteznek, ezért a
    // JsonSerializer csendben eldobná őket — enélkül a migrálás nélkül egy már
    // meglévő mentés elveszítené a Level 1-es kör-progresszt és a preset-et.
    private static void MigrateLegacySingleLevelFields(string json, PlayerProgress progress)
    {
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("HighestUnlockedRound", out var legacyRound))
        {
            var round = legacyRound.GetInt32();
            progress.HighestUnlockedRoundByLevel["Level1"] = round;

            // Ha a régi mentésben Level 1 teljesen kész volt (10. kör feloldva),
            // Level 2 is nyíljon meg — ugyanaz a szabály, mint LevelBuild.EndRound
            // "pálya teljesítve" ágában, csak itt egyszeri migrálásként.
            if (round >= 10 && !progress.HighestUnlockedRoundByLevel.ContainsKey("Level2"))
            {
                progress.HighestUnlockedRoundByLevel["Level2"] = 1;
            }
        }

        if (doc.RootElement.TryGetProperty("Level1Preset", out var legacyPreset))
        {
            var preset = JsonSerializer.Deserialize<List<PresetTowerEntry>>(legacyPreset.GetRawText());
            if (preset != null)
            {
                progress.PresetByLevel["Level1"] = preset;
            }
        }
    }

    public void Save(PlayerProgress progress)
    {
        File.WriteAllText(SavePath, JsonSerializer.Serialize(progress));
    }
}
