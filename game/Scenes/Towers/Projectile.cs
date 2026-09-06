using Godot;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.UI;

namespace TowerDefense.Towers;

public partial class Projectile : Node2D
{
    public Enemy Target { get; set; }
    public float Damage { get; set; }
    public float Speed { get; set; } = 400f;
    public string TowerName { get; set; } = "Tower";

    public override void _Ready()
    {
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(Target))
        {
            QueueFree();
            return;
        }

        var toTarget = Target.GlobalPosition - GlobalPosition;
        var step = Speed * (float)delta;

        if (toTarget.Length() <= step)
        {
            var hitPosition = Target.GlobalPosition;
            Target.TakeDamage(Damage);
            DamageTracker.Report(TowerName, Damage);
            DamageNumberSpawner.Spawn(this, hitPosition, Damage);
            QueueFree();
            return;
        }

        GlobalPosition += toTarget.Normalized() * step;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 8f, Colors.Yellow);
    }
}
