using Godot;
using TowerDefense.Enemies;

namespace TowerDefense.Towers;

public partial class Projectile : Node2D
{
    public Enemy Target { get; set; }
    public float Damage { get; set; }
    public float Speed { get; set; } = 400f;

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
            Target.TakeDamage(Damage);
            QueueFree();
            return;
        }

        GlobalPosition += toTarget.Normalized() * step;
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 6f, Colors.Yellow);
    }
}
