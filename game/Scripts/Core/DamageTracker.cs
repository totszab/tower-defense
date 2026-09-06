using System;

namespace TowerDefense.Core;

// Egyszerű statikus event-bus, hogy a Tower/Projectile ne kelljen a LevelBuild-re
// hivatkozzon közvetlenül a sebzés-statisztikához. A feliratkozó (LevelBuild)
// felelős a le- és feliratkozásért (_ExitTree-ben), mert ez statikus event.
public static class DamageTracker
{
    public static event Action<string, float> DamageDealt;

    // In-game gombbal kapcsolható (lásd LevelBuild "Dmg Numbers" gomb).
    public static bool ShowDamageNumbers = true;

    public static void Report(string towerName, float amount)
    {
        DamageDealt?.Invoke(towerName, amount);
    }
}
