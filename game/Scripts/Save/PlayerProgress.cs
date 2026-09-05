using System.Collections.Generic;

namespace TowerDefense.Save;

// Bootstrap subset only — HighestUnlockedLevelIndex és LevelPresets a
// szint-progresszió munkával kerülnek be (ROADMAP Fázis 4).
public class PlayerProgress
{
    public int MetaCurrency { get; set; }

    // Skill fa node id -> jelenlegi szint (0-5). Node id-k: "towers" (hub),
    // "hp", "currency", "dmg", "fireRate" (lásd MainMenu.cs).
    public Dictionary<string, int> SkillLevels { get; set; } = new();

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
}
