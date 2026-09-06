using System.Collections.Generic;
using Godot;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Save;
using TowerDefense.UI;

namespace TowerDefense.Towers;

public partial class Tower : Node2D
{
    [Export] public TowerData Data { get; set; }

    private readonly List<Enemy> _enemiesInRange = new();
    private float _cooldown;
    private int _bonusDamage;
    private float _fireRateMultiplier = 1f;

    public float EffectiveDamage => Data.Damage + _bonusDamage;
    public float EffectiveFireRate => Data.FireRate * _fireRateMultiplier;

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
        _cooldown = 1f / EffectiveFireRate;
    }

    private void FireAt(Enemy target)
    {
        var damage = EffectiveDamage;

        if (Data.ProjectileScene == null)
        {
            target.TakeDamage(damage);
            DamageTracker.Report(Data.DisplayName, damage);
            DamageNumberSpawner.Spawn(this, target.GlobalPosition, damage);
            return;
        }

        var projectile = Data.ProjectileScene.Instantiate<Projectile>();
        // Fontos: AddChild ELŐBB, utána GlobalPosition — ha egy még szülő
        // nélküli node-on állítjuk be a GlobalPosition-t, azt csak lokálisként
        // tárolja, és a tényleges szülő (aminek van saját eltolása/skálája)
        // alá kerülve rossz, "duplán eltolt" helyre kerülne.
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
        projectile.Target = target;
        projectile.Damage = damage;
        projectile.TowerName = Data.DisplayName;
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
