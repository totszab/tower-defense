using Godot;

namespace TowerDefense.Data;

[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string DisplayName { get; set; } = "Enemy";

    [Export] public float Hp { get; set; } = 5f;
    [Export] public float Dmg { get; set; } = 1f;
    [Export] public int Value { get; set; } = 1;

    // Tiles per second (see GridConstants.TileSize).
    [Export] public float Speed { get; set; } = 1f;

    [Export] public Texture2D Sprite { get; set; }

    // Placeholder-tier variánsok: ugyanazt a sprite-ot színezzük/skálázzuk,
    // amíg nincs egyedi art (pl. Blue Slime, mini/final boss).
    [Export] public Color Tint { get; set; } = Colors.White;
    [Export] public float SpriteScale { get; set; } = 1.25f;
    [Export] public float HitRadius { get; set; } = 35f;
}
