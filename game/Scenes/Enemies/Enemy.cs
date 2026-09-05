using Godot;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.Enemies;

public partial class Enemy : Area2D
{
    [Signal]
    public delegate void DiedEventHandler();

    [Export] public EnemyData Data { get; set; }

    private float _currentHp;

    public override void _Ready()
    {
        _currentHp = Data.Hp;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Bootstrap smoke test only: straight-line movement. Real path-following
        // (level waypoints) replaces this once Level scenes exist (ROADMAP Fázis 3).
        Position += Vector2.Right * Data.Speed * GridConstants.TileSize * (float)delta;
    }

    public void TakeDamage(float amount)
    {
        _currentHp -= amount;
        if (_currentHp <= 0f)
        {
            EmitSignal(SignalName.Died);
            QueueFree();
        }
    }
}
