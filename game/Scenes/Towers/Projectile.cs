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

    // Tiles (see GridConstants.TileSize). 0 = nincs area sebzés.
    public float SplashRadius { get; set; } = 0f;

    // Towers ág, "splash damage %" — csak a splash-találatra hat, a normál
    // (nem-splash) becsapódásra nem. 1 = nincs bónusz.
    public float SplashDamageMultiplier { get; set; } = 1f;

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

            if (SplashRadius > 0f)
            {
                HitSplash(hitPosition);
            }
            else
            {
                Target.TakeDamage(Damage);
                DamageTracker.Report(TowerName, Damage);
                DamageNumberSpawner.Spawn(this, hitPosition, Damage);
            }

            QueueFree();
            return;
        }

        GlobalPosition += toTarget.Normalized() * step;
    }

    // Mindenkit sebez a becsapódási pont körüli SplashRadius-on belül (a
    // célpontot is), nem csak a lezárt Target-et — az "Enemies" konténer
    // gyerekei közül szűrünk, mert nincs Area2D-alapú overlap-lekérdezés
    // idekötve. Az Enemy.IsDead (lásd Enemy.cs) kiszűri, ha egy szomszédos
    // ellenség UGYANEBBEN a frame-ben már meghalt egy másik találattól —
    // enélkül a TakeDamage() saját _dead védelme ezt már csendben elnyelné,
    // de itt korábban kiszűrve elkerüljük a felesleges Report/Spawn hívást is.
    private void HitSplash(Vector2 hitPosition)
    {
        var splashRadiusPx = SplashRadius * GridConstants.TileSize;
        var splashDamage = Damage * SplashDamageMultiplier;
        var enemies = GetTree().CurrentScene.GetNode<Node2D>("Enemies");

        foreach (var child in enemies.GetChildren())
        {
            if (child is not Enemy enemy || !IsInstanceValid(enemy) || enemy.IsDead) continue;
            if (enemy.GlobalPosition.DistanceTo(hitPosition) > splashRadiusPx) continue;

            enemy.TakeDamage(splashDamage);
            DamageTracker.Report(TowerName, splashDamage);
            DamageNumberSpawner.Spawn(this, enemy.GlobalPosition, splashDamage);
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 8f, Colors.Yellow);
    }
}
