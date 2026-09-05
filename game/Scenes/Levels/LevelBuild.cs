using System;
using System.Collections.Generic;
using Godot;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Towers;

namespace TowerDefense.Levels;

// Bootstrap smoke test only: hardcoded grid size, no slot-limit/skill-tree
// gating yet (that arrives with SkillTreeManager, ROADMAP Fázis 4).
public partial class LevelBuild : Node2D
{
    private const int Columns = 10;
    private const int PathRow = 1;
    private static readonly int[] BuildableRows = { 0, 2 };

    [Export] public PackedScene TowerScene { get; set; }
    [Export] public PackedScene EnemyScene { get; set; }

    private readonly HashSet<Vector2I> _occupiedTiles = new();
    private Node2D _towers;
    private Node2D _enemies;

    public override void _Ready()
    {
        _towers = GetNode<Node2D>("Towers");
        _enemies = GetNode<Node2D>("Enemies");
        GetNode<Button>("CanvasLayer/SpawnButton").Pressed += OnSpawnPressed;
        GetNode<Area2D>("GoalArea").AreaEntered += OnGoalEntered;
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            TryPlaceTower(GetLocalMousePosition());
        }
    }

    private void TryPlaceTower(Vector2 localPos)
    {
        var tile = new Vector2I(
            Mathf.FloorToInt(localPos.X / GridConstants.TileSize),
            Mathf.FloorToInt(localPos.Y / GridConstants.TileSize));

        if (tile.X < 0 || tile.X >= Columns) return;
        if (Array.IndexOf(BuildableRows, tile.Y) < 0) return;
        if (_occupiedTiles.Contains(tile)) return;

        var tower = TowerScene.Instantiate<Tower>();
        tower.Position = new Vector2(
            (tile.X + 0.5f) * GridConstants.TileSize,
            (tile.Y + 0.5f) * GridConstants.TileSize);
        _towers.AddChild(tower);
        _occupiedTiles.Add(tile);
    }

    private void OnSpawnPressed()
    {
        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Position = new Vector2(0, (PathRow + 0.5f) * GridConstants.TileSize);
        _enemies.AddChild(enemy);
    }

    private void OnGoalEntered(Area2D area)
    {
        if (area is Enemy enemy)
        {
            // TBD (ROADMAP Fázis 4): levonni Data.Dmg-et a játékos életéből RunState-en keresztül.
            enemy.QueueFree();
        }
    }

    public override void _Draw()
    {
        for (var x = 0; x < Columns; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                var color = y == PathRow
                    ? new Color(0.62f, 0.49f, 0.31f)
                    : new Color(0.35f, 0.6f, 0.35f);
                var rect = new Rect2(
                    x * GridConstants.TileSize,
                    y * GridConstants.TileSize,
                    GridConstants.TileSize,
                    GridConstants.TileSize);
                DrawRect(rect, color, true);
                DrawRect(rect, new Color(0f, 0f, 0f, 0.15f), false, 1f);
            }
        }
    }
}
