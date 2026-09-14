using System.Collections.Generic;

namespace TowerDefense.Save;

// Bootstrap subset only.
public class PlayerProgress
{
    public int MetaCurrency { get; set; }

    // Skill fa node id -> jelenlegi szint (0-5). Node id-k: "towers" (hub),
    // "hp", "currency", "dmg", "fireRate" (lásd MainMenu.cs).
    public Dictionary<string, int> SkillLevels { get; set; } = new();

    // Pálya-kulcsos (pl. "Level1") map: hányas kör a legmagasabb feloldott
    // az adott pályán belül. Hiányzó kulcs = a pálya maga sincs feloldva
    // (lásd IsLevelUnlocked). Level1 alapból 1-en indul, a többi pálya az
    // előző pálya 10. körének teljesítésekor nyílik meg (LevelBuild.EndRound).
    public Dictionary<string, int> HighestUnlockedRoundByLevel { get; set; } = new() { ["Level1"] = 1 };

    // Pályánként EGY mentett torony-elrendezés, amit az adott pálya
    // BÁRMELYIK körének indításakor alapból alkalmazunk.
    public Dictionary<string, List<PresetTowerEntry>> PresetByLevel { get; set; } = new();

    // A Főmenü szint-választó popupja ezt nyitja meg alapból.
    public string LastPlayedLevelId { get; set; } = "Level1";

    public int GetSkillLevel(string nodeId)
    {
        var level = SkillLevels.TryGetValue(nodeId, out var value) ? value : 0;

        // Baseline: a "towers" node mindig legalább 1-en indul, hogy egy
        // teljesen friss (0 aranyas) mentésnél is lerakható legyen 1 torony.
        if (nodeId == "towers" && level < 1)
        {
            level = 1;
        }

        return level;
    }

    public int GetHighestUnlockedRound(string levelId) =>
        HighestUnlockedRoundByLevel.TryGetValue(levelId, out var round) ? round : 0;

    public bool IsLevelUnlocked(string levelId) => GetHighestUnlockedRound(levelId) >= 1;

    public List<PresetTowerEntry> GetPreset(string levelId) =>
        PresetByLevel.TryGetValue(levelId, out var preset) ? preset : new List<PresetTowerEntry>();

    public void SetPreset(string levelId, List<PresetTowerEntry> preset)
    {
        PresetByLevel[levelId] = preset;
    }
}
