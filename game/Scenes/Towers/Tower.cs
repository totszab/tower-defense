using System.Collections.Generic;
using Godot;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Save;

namespace TowerDefense.Towers;

public partial class Tower : Node2D
{
    [Export] public TowerData Data { get; set; }

    private readonly List<Enemy> _enemiesInRange = new();
    private float _cooldown;
    private int _bonusDamage;
    private float _fireRateMultiplier = 1f;

    public override void _Ready()
    {
        var progress = new LocalFileSaveProvider().Load();
        _bonusDamage = progress.GetSkillLevel("dmg");
        // TBD: ma minden toronyra hat (csak 1 típus van), amikor több torony
        // típus lesz, ezt torony-specifikusra kell szűkíteni (GAMEPLAY.md).
        _fireRateMultiplier = 1f + progress.GetSkillLevel("fireRate") * 0.05f;

        var rangeArea = GetNode<Area2D>("RangeArea");
        rangeArea.AreaEntered += OnAreaEntered;
        rangeArea.AreaExited += OnAreaExited;
    }

    public override void _Process(double delta)
    {
        _enemiesInRange.RemoveAll(enemy => !IsInstanceValid(enemy));

        if (_enemiesInRange.Count == 0)
        {
            return;
        }

        _cooldown -= (float)delta;
        if (_cooldown > 0f)
        {
            return;
        }

        FireAt(_enemiesInRange[0]);
        _cooldown = 1f / (Data.FireRate * _fireRateMultiplier);
    }

    private void FireAt(Enemy target)
    {
        var damage = Data.Damage + _bonusDamage;

        if (Data.ProjectileScene == null)
        {
            target.TakeDamage(damage);
            return;
        }

        var projectile = Data.ProjectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Target = target;
        projectile.Damage = damage;
        GetTree().CurrentScene.AddChild(projectile);
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is Enemy enemy)
        {
            _enemiesInRange.Add(enemy);
        }
    }

    private void OnAreaExited(Area2D area)
    {
        if (area is Enemy enemy)
        {
            _enemiesInRange.Remove(enemy);
        }
    }
}
