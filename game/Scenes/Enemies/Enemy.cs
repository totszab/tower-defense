using Godot;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.Enemies;

public partial class Enemy : Area2D
{
    [Signal]
    public delegate void DiedEventHandler();

    private const float BarWidth = 50f;
    private const float BarHeight = 6f;
    private const float BarYOffset = -50f;

    [Export] public EnemyData Data { get; set; }

    private float _currentHp;

    public override void _Ready()
    {
        _currentHp = Data.Hp;
        QueueRedraw();
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
        QueueRedraw();

        if (_currentHp <= 0f)
        {
            EmitSignal(SignalName.Died);
            QueueFree();
        }
    }

    // Piros életerő-sáv az ellenség fölött, szám nélkül.
    public override void _Draw()
    {
        var pct = Data.Hp > 0f ? Mathf.Clamp(_currentHp / Data.Hp, 0f, 1f) : 0f;
        var topLeft = new Vector2(-BarWidth / 2f, BarYOffset);

        DrawRect(new Rect2(topLeft, new Vector2(BarWidth, BarHeight)), new Color(0.15f, 0.03f, 0.03f, 0.9f));
        DrawRect(new Rect2(topLeft, new Vector2(BarWidth * pct, BarHeight)), new Color(0.85f, 0.15f, 0.15f, 1f));
    }
}
