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

        var sprite = GetNode<Sprite2D>("Sprite2D");
        sprite.Modulate = Data.Tint;
        sprite.Scale = new Vector2(Data.SpriteScale, Data.SpriteScale);

        // A CollisionShape2D alap shape-je meg van osztva minden Enemy.tscn
        // példány között — duplikálni kell, különben egy boss megnagyobbított
        // hitboxa minden más ellenségre is átterjedne.
        var collision = GetNode<CollisionShape2D>("CollisionShape2D");
        var shape = (CircleShape2D)((CircleShape2D)collision.Shape).Duplicate();
        shape.Radius = Data.HitRadius;
        collision.Shape = shape;

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
        var barWidth = BarWidth * Data.SpriteScale;
        var topLeft = new Vector2(-barWidth / 2f, BarYOffset * Data.SpriteScale);

        DrawRect(new Rect2(topLeft, new Vector2(barWidth, BarHeight)), new Color(0.15f, 0.03f, 0.03f, 0.9f));
        DrawRect(new Rect2(topLeft, new Vector2(barWidth * pct, BarHeight)), new Color(0.85f, 0.15f, 0.15f, 1f));
    }
}
