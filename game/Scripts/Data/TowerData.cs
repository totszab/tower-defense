using Godot;

namespace TowerDefense.Data;

[GlobalClass]
public partial class TowerData : Resource
{
    [Export] public string DisplayName { get; set; } = "Tower";

    [Export] public float Damage { get; set; } = 1f;

    // Shots per second.
    [Export] public float FireRate { get; set; } = 1f;

    // In tiles (see GridConstants.TileSize).
    [Export] public float Range { get; set; } = 3f;

    [Export] public Texture2D Sprite { get; set; }
    [Export] public PackedScene ProjectileScene { get; set; }

    // Tiles (see GridConstants.TileSize). 0 = nincs area sebzés, a lövedék
    // csak a célpontot sebzi (ez az alap Rocket Tower viselkedése).
    [Export] public float SplashRadius { get; set; } = 0f;
}
