using Godot;

namespace TowerDefense.Data;

// Egy kör ellenség-menetrendje: a Steps sorban, SpawnInterval másodpercenként
// pörögnek le (minden Step egyszerre Count db Enemy-t indít). Ez az egy modell
// fedi az eddigi mintázatokat: egyenletes hullám (1 típus, Count=1 vagy N),
// kevert/interleaved sorrend (több Step, váltakozó Enemy), és a záró boss
// (egy utolsó Step, Count=1, nagyobb EnemyData-val).
[GlobalClass]
public partial class WaveData : Resource
{
    [Export] public float SpawnInterval { get; set; } = 2f;
    [Export] public SpawnStepData[] Steps { get; set; } = System.Array.Empty<SpawnStepData>();

    public int TotalEnemyCount()
    {
        var total = 0;
        foreach (var step in Steps)
        {
            total += step.Count;
        }
        return total;
    }
}
