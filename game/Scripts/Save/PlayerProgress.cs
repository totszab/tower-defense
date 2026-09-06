using System.Collections.Generic;

namespace TowerDefense.Save;

// Bootstrap subset only.
public class PlayerProgress
{
    public int MetaCurrency { get; set; }

    // Skill fa node id -> jelenlegi szint (0-5). Node id-k: "towers" (hub),
    // "hp", "currency", "dmg", "fireRate" (lásd MainMenu.cs).
    public Dictionary<string, int> SkillLevels { get; set; } = new();

    // Egyszerűsített, egy-pályás verzió: hányas kör a legmagasabb feloldott
    // (Level 1-en belül). Ha több pálya lesz, ez pálya-kulcsos map-re bővül.
    public int HighestUnlockedRound { get; set; } = 1;

    // Pályánként (jelenleg csak Level 1) EGY mentett torony-elrendezés,
    // amit a pálya BÁRMELYIK körének indításakor alapból alkalmazunk.
    // Ha több pálya lesz, ez is pálya-kulcsos map-re bővül.
    public List<PresetTowerEntry> Level1Preset { get; set; } = new();

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
