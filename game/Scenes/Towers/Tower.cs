using System.Collections.Generic;
using Godot;
using TowerDefense.Data;
using TowerDefense.Enemies;

namespace TowerDefense.Towers;

public partial class Tower : Node2D
{
    [Export] public TowerData Data { get; set; }

    private readonly List<Enemy> _enemiesInRange = new();
    private float _cooldown;

    public override void _Ready()
    {
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
        _cooldown = 1f / Data.FireRate;
    }

    private void FireAt(Enemy target)
    {
        if (Data.ProjectileScene == null)
        {
            target.TakeDamage(Data.Damage);
            return;
        }

        var projectile = Data.ProjectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Target = target;
        projectile.Damage = Data.Damage;
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
