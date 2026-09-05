using System.Collections.Generic;

namespace TowerDefense.Save;

// Bootstrap subset only — HighestUnlockedLevelIndex és LevelPresets a
// szint-progresszió munkával kerülnek be (ROADMAP Fázis 4).
public class PlayerProgress
{
    public int MetaCurrency { get; set; }

    // Skill fa node id -> jelenlegi szint (0-5). Node id-k: "dmg", "hp",
    // "towers", "currency", "enemy" (lásd MainMenu.cs).
    public Dictionary<string, int> SkillLevels { get; set; } = new();

    public int GetSkillLevel(string nodeId) =>
        SkillLevels.TryGetValue(nodeId, out var level) ? level : 0;
}
