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
    private float _damagePercentMultiplier = 1f;
    private float _rangeMultiplier = 1f;
    private float _splashRadiusMultiplier = 1f;
    private float _splashDamageMultiplier = 1f;
    private float _chanceToSplash;

    public float EffectiveDamage => (Data.Damage + _bonusDamage) * _damagePercentMultiplier;
    public float EffectiveFireRate => Data.FireRate * _fireRateMultiplier;
    public float EffectiveRange => Data.Range * _rangeMultiplier;

    public override void _Ready()
    {
        var progress = new LocalFileSaveProvider().Load();
        // TBD: ma minden toronyra hat (globális), amikor torony-specifikus
        // upgrade-ág is lesz, ezt szűkíteni kell (GAMEPLAY.md "Skill fa").
        _bonusDamage = progress.GetSkillLevel("dmg") + progress.GetSkillLevel("dmg2") * 2;
        _fireRateMultiplier = 1f
            + (progress.GetSkillLevel("fireRate") + progress.GetSkillLevel("fireRate2")) * 0.05f
            + progress.GetSkillLevel("turretAttackSpeed") * 0.02f;
        _critChance = (progress.GetSkillLevel("critChance") + progress.GetSkillLevel("critChance2")) * 0.01f;
        _critDamageMultiplier = 1f + progress.GetSkillLevel("critDamage") * 0.05f;
        _extraProjectiles = progress.GetSkillLevel("projectileCount");
        _fireTwiceChance = progress.GetSkillLevel("fireTwiceChance") * 0.02f;
        _damagePercentMultiplier = 1f + progress.GetSkillLevel("turretDamagePercent") * 0.03f;
        _rangeMultiplier = 1f + progress.GetSkillLevel("turretRadius") * 0.05f;
        _splashRadiusMultiplier = 1f + progress.GetSkillLevel("splashAreaPercent") * 0.02f;
        _splashDamageMultiplier = 1f + progress.GetSkillLevel("splashDamagePercent") * 0.05f;
        _chanceToSplash = progress.GetSkillLevel("chanceToSplash") * 0.02f;

        var rangeArea = GetNode<Area2D>("RangeArea");
        rangeArea.AreaEntered += OnAreaEntered;
        rangeArea.AreaExited += OnAreaExited;

        // Towers ág, "+base turret radius %" — a .tscn-ben rögzített
        // CircleShape2D sugarát Data.Range-ből (és a bónuszból) frissen
        // számoljuk újra, nem hagyatkozunk a .tscn-be beégetett értékre.
        // Duplikálni kell a shape-et, mert meg van osztva minden ugyanolyan
        // típusú torony-példány között (ugyanaz az elv, mint az Enemy
        // CollisionShape2D-jénél).
        if (_rangeMultiplier != 1f)
        {
            var rangeCollision = rangeArea.GetNode<CollisionShape2D>("CollisionShape2D");
            var rangeShape = (CircleShape2D)((CircleShape2D)rangeCollision.Shape).Duplicate();
            rangeShape.Radius = EffectiveRange * GridConstants.TileSize;
            rangeCollision.Shape = rangeShape;
        }
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
        projectile.SplashRadius = EffectiveSplashRadius();
        projectile.SplashDamageMultiplier = _splashDamageMultiplier;
    }

    // Towers ág: a "splash area %" a MEGLÉVŐ splash sugarat növeli; a "chance
    // to splash" pedig esélyt ad, hogy egy EGYÉBKÉNT nem-splash torony
    // (SplashRadius=0) lövése is területet sebezzen (kis, fix 1 tile-os
    // sugárral) — ugyanazt a Projectile.HitSplash logikát használva.
    private float EffectiveSplashRadius()
    {
        if (Data.SplashRadius > 0f)
        {
            return Data.SplashRadius * _splashRadiusMultiplier;
        }

        if (_chanceToSplash > 0f && GD.Randf() < _chanceToSplash)
        {
            return 1f;
        }

        return 0f;
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
