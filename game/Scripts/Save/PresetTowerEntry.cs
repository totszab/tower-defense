namespace TowerDefense.Save;

// Egy lerakott torony egy mentett preset-ben. A TowerScenePath a Tower.tscn
// (vagy jövőbeli más torony-scene) elérési útja — Node.SceneFilePath adja
// vissza placement-kor, ez alapján tudjuk visszakeresni AvailableTowers-ből.
public class PresetTowerEntry
{
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string TowerScenePath { get; set; }
}
