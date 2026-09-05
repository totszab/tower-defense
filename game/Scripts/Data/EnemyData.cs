using Godot;

namespace TowerDefense.Data;

[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public float Hp { get; set; } = 5f;
    [Export] public float Dmg { get; set; } = 1f;
    [Export] public int Value { get; set; } = 1;

    // Tiles per second (see GridConstants.TileSize).
    [Export] public float Speed { get; set; } = 1f;

    [Export] public Texture2D Sprite { get; set; }
}
