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
    private float _critChance;
    private float _critDamageMultiplier = 1f;
    private int _extraProjectiles;
    private float _fireTwiceChance;

    public float EffectiveDamage => Data.Damage + _bonusDamage;
    public float EffectiveFireRate => Data.FireRate * _fireRateMultiplier;

    public override void _Ready()
    {
        var progress = new LocalFileSaveProvider().Load();
        // TBD: ma minden toronyra hat (globális), amikor torony-specifikus
        // upgrade-ág is lesz, ezt szűkíteni kell (GAMEPLAY.md "Skill fa").
        _bonusDamage = progress.GetSkillLevel("dmg") + progress.GetSkillLevel("dmg2") * 2;
        _fireRateMultiplier = 1f + (progress.GetSkillLevel("fireRate") + progress.GetSkillLevel("fireRate2")) * 0.05f;
        _critChance = (progress.GetSkillLevel("critChance") + progress.GetSkillLevel("critChance2")) * 0.01f;
        _critDamageMultiplier = 1f + progress.GetSkillLevel("critDamage") * 0.05f;
        _extraProjectiles = progress.GetSkillLevel("projectileCount");
        _fireTwiceChance = progress.GetSkillLevel("fireTwiceChance") * 0.02f;

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
        // Damage ág, L4: esély, hogy a normál lövés UTÁN azonnal még egyszer
        // tüzeljen (nem várja meg a cooldown-t) — a FireAt saját maga
        // (újra)sorsolja a kritikus találatot/extra lövedékeket is.
        if (_fireTwiceChance > 0f && GD.Randf() < _fireTwiceChance && _enemiesInRange.Count > 0)
        {
            FireAt(_enemiesInRange[0]);
        }

        _cooldown = 1f / EffectiveFireRate;
    }

    private void FireAt(Enemy target)
    {
        var isCrit = _critChance > 0f && GD.Randf() < _critChance;
        var damage = EffectiveDamage * (isCrit ? _critDamageMultiplier : 1f);

        // Damage ág, L4: +1 lövedék/szint — mindegyik ugyanarra a célpontra
        // csapódik be (a torony egyetlen célpontot fókuszál, lásd
        // GAMEPLAY.md a "front-focus" mechanikáról), tehát gyakorlatilag a
        // kifejtett sebzést sokszorozza az aktuális célponton.
        var shotCount = 1 + _extraProjectiles;
        for (var i = 0; i < shotCount; i++)
        {
            FireProjectile(target, damage);
        }
    }

    private void FireProjectile(Enemy target, float damage)
    {
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
        projectile.SplashRadius = Data.SplashRadius;
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
