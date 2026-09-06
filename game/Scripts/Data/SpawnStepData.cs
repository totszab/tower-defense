using Godot;

namespace TowerDefense.Data;

// Egy "tick": ennyi darab EnemyData spawnol egyszerre, a WaveData SpawnInterval-jén.
[GlobalClass]
public partial class SpawnStepData : Resource
{
    [Export] public EnemyData Enemy { get; set; }
    [Export] public int Count { get; set; } = 1;
}
