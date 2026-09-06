using System;
using System.Collections.Generic;
using Godot;
using TowerDefense.Core;
using TowerDefense.Enemies;
using TowerDefense.Save;
using TowerDefense.Towers;
using TowerDefense.UI;

namespace TowerDefense.Levels;

// Bootstrap smoke test only: hardcoded grid size, single hardcoded wave,
// no full RunState wiring yet (that arrives ROADMAP Fázis 4). Skill fa
// hatások (hp/towers/currency/fireRate/dmg) a PlayerProgress-ből olvasva.
public partial class LevelBuild : Node2D
{
    private const int Columns = 13;
    private const int PathRow = 1;
    private static readonly int[] BuildableRows = { 0, 2 };

    // Bootstrap values only — real per-level numbers belong in a Resource
    // once real levels exist (GAMEPLAY.md "Pályák" TBD).
    private const int BaseStartingHp = 3;
    private const int BaseEnemiesPerWave = 10;
    private const float SpawnInterval = 2.0f;

    [Export] public PackedScene[] AvailableTowers { get; set; } = Array.Empty<PackedScene>();
    [Export] public PackedScene EnemyScene { get; set; }

    private readonly HashSet<Vector2I> _occupiedTiles = new();
    private Node2D _towers;
    private Node2D _enemies;
    private VBoxContainer _towerPalette;
    private TextureButton _selectedButton;
    private PackedScene _selectedTowerScene;
    private bool _buildingEnabled = true;

    private const float HpBarMaxWidth = 194f;

    private ColorRect _hpBarFill;
    private Label _hpBarLabel;
    private int _maxHp;
    private Label _totalGoldLabel;
    private Label _enemyCountLabel;
    private Label _towerCountLabel;
    private Button _startRoundButton;
    private Timer _spawnTimer;

    private Panel _statsPopup;
    private Label _statsTitleLabel;
    private Label _statsGoldLabel;
    private VBoxContainer _damageListContainer;

    private PlayerProgress _progress;
    private int _maxTowers;
    private float _goldMultiplier;
    private int _enemiesThisWave;

    private readonly Dictionary<string, float> _damageByTower = new();
    private ulong _roundStartMsec;

    private int _hp;
    private int _goldCollected;
    private int _enemiesSpawned;
    private int _enemiesResolved;
    private bool _roundActive;

    public override void _Ready()
    {
        _towers = GetNode<Node2D>("Towers");
        _enemies = GetNode<Node2D>("Enemies");
        _hpBarFill = GetNode<ColorRect>("CanvasLayer/HpBarBg/HpBarFill");
        _hpBarLabel = GetNode<Label>("CanvasLayer/HpBarBg/HpBarLabel");
        _totalGoldLabel = GetNode<Label>("CanvasLayer/TotalGoldLabel");
        _enemyCountLabel = GetNode<Label>("CanvasLayer/EnemyPanel/EnemyCountLabel");
        _towerCountLabel = GetNode<Label>("CanvasLayer/RightPanel/TowerCountLabel");
        _startRoundButton = GetNode<Button>("CanvasLayer/RightPanel/StartRoundButton");
        _spawnTimer = GetNode<Timer>("SpawnTimer");

        _statsPopup = GetNode<Panel>("CanvasLayer/StatsPopup");
        _statsTitleLabel = GetNode<Label>("CanvasLayer/StatsPopup/TitleLabel");
        _statsGoldLabel = GetNode<Label>("CanvasLayer/StatsPopup/RoundGoldLabel");
        _damageListContainer = GetNode<VBoxContainer>("CanvasLayer/StatsPopup/DamageList");

        _startRoundButton.Pressed += OnStartRoundPressed;
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        GetNode<Area2D>("GoalArea").AreaEntered += OnGoalEntered;
        GetNode<Button>("CanvasLayer/RightPanel/BackButton").Pressed += OnBackPressed;
        GetNode<Button>("CanvasLayer/StatsPopup/CloseButton").Pressed += OnCloseStatsPressed;
        var dmgToggle = GetNode<Button>("CanvasLayer/RightPanel/DamageToggleButton");
        dmgToggle.Pressed += () => OnDamageTogglePressed(dmgToggle);
        UpdateDamageToggleText(dmgToggle);
        DamageTracker.DamageDealt += OnDamageDealt;

        BuildTowerPalette();

        _progress = new LocalFileSaveProvider().Load();
        // "towers" node legalább 1-en indul (lásd PlayerProgress.GetSkillLevel), így
        // egy friss mentésnél is lerakható az első torony.
        _maxTowers = _progress.GetSkillLevel("towers");
        _goldMultiplier = 1f + _progress.GetSkillLevel("currency") * 0.10f;
        _enemiesThisWave = BaseEnemiesPerWave;

        _maxHp = BaseStartingHp + _progress.GetSkillLevel("hp") * 2;
        _hp = _maxHp;
        UpdateHpBar();
        _totalGoldLabel.Text = $"Total Gold: {_progress.MetaCurrency}";
        _enemyCountLabel.Text = $"Enemies this round: {_enemiesThisWave}";
        _towerCountLabel.Text = $"Towers: 0/{_maxTowers}";
        _statsPopup.Visible = false;

        // Placeholder arany-ikon a "Total Gold" felirat ELÉ.
        var coin = UiHelpers.MakeCircle(new Vector2(20, 20), Colors.Gold);
        coin.Position = new Vector2(10, 49);
        GetNode<CanvasLayer>("CanvasLayer").AddChild(coin);

        // Placeholder ikon a soron következő ellenségtípusról az Enemy panelen.
        var enemyPreview = EnemyScene.Instantiate<Enemy>();
        var enemyTexture = enemyPreview.GetNode<Sprite2D>("Sprite2D").Texture;
        enemyPreview.QueueFree();

        var enemyIcon = new TextureRect
        {
            Texture = enemyTexture,
            Position = new Vector2(220, 4),
            Size = new Vector2(28, 28),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
        };
        GetNode<Control>("CanvasLayer/EnemyPanel").AddChild(enemyIcon);

        QueueRedraw();
    }

    public override void _ExitTree()
    {
        DamageTracker.DamageDealt -= OnDamageDealt;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            TryPlaceTower(GetLocalMousePosition());
        }
    }

    private void BuildTowerPalette()
    {
        var rightPanel = GetNode<Control>("CanvasLayer/RightPanel");
        _towerPalette = new VBoxContainer { Position = new Vector2(20, 105) };
        rightPanel.AddChild(_towerPalette);

        foreach (var towerScene in AvailableTowers)
        {
            // Instantiate off-tree just to read the default sprite for the icon, then discard it.
            var preview = towerScene.Instantiate<Node2D>();
            var icon = preview.GetNode<Sprite2D>("Sprite2D").Texture;
            preview.QueueFree();

            var button = new TextureButton
            {
                TextureNormal = icon,
                CustomMinimumSize = new Vector2(64, 64),
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
                IgnoreTextureSize = true,
                Modulate = Colors.White,
            };
            button.Pressed += () => OnTowerButtonPressed(button, towerScene);
            _towerPalette.AddChild(button);
        }
    }

    private void OnTowerButtonPressed(TextureButton button, PackedScene towerScene)
    {
        if (_selectedButton == button)
        {
            // Clicking the already-selected tower again deselects it.
            button.Modulate = Colors.White;
            _selectedButton = null;
            _selectedTowerScene = null;
            return;
        }

        if (_selectedButton != null)
        {
            _selectedButton.Modulate = Colors.White;
        }

        button.Modulate = new Color(1f, 0.95f, 0.4f);
        _selectedButton = button;
        _selectedTowerScene = towerScene;
    }

    private void TryPlaceTower(Vector2 localPos)
    {
        if (!_buildingEnabled) return;
        if (_selectedTowerScene == null) return;
        if (_occupiedTiles.Count >= _maxTowers) return;

        var tile = new Vector2I(
            Mathf.FloorToInt(localPos.X / GridConstants.TileSize),
            Mathf.FloorToInt(localPos.Y / GridConstants.TileSize));

        if (tile.X < 0 || tile.X >= Columns) return;
        if (Array.IndexOf(BuildableRows, tile.Y) < 0) return;
        if (_occupiedTiles.Contains(tile)) return;

        var tower = _selectedTowerScene.Instantiate<Tower>();
        tower.Position = new Vector2(
            (tile.X + 0.5f) * GridConstants.TileSize,
            (tile.Y + 0.5f) * GridConstants.TileSize);
        _towers.AddChild(tower);
        _occupiedTiles.Add(tile);
        _towerCountLabel.Text = $"Towers: {_occupiedTiles.Count}/{_maxTowers}";
    }

    private void SetBuildingEnabled(bool enabled)
    {
        _buildingEnabled = enabled;
        foreach (var child in _towerPalette.GetChildren())
        {
            if (child is BaseButton button)
            {
                button.Disabled = !enabled;
            }
        }
    }

    private void OnStartRoundPressed()
    {
        if (_roundActive) return;

        _roundActive = true;
        _goldCollected = 0;
        _enemiesSpawned = 0;
        _enemiesResolved = 0;
        _damageByTower.Clear();
        _roundStartMsec = Time.GetTicksMsec();
        SetBuildingEnabled(false);
        _startRoundButton.Disabled = true;

        SpawnEnemy();
        _enemiesSpawned = 1;
        _spawnTimer.WaitTime = SpawnInterval;
        _spawnTimer.Start();
    }

    private void OnSpawnTimerTimeout()
    {
        SpawnEnemy();
        _enemiesSpawned++;
        if (_enemiesSpawned >= _enemiesThisWave)
        {
            _spawnTimer.Stop();
        }
    }

    private void SpawnEnemy()
    {
        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Position = new Vector2(0, (PathRow + 0.5f) * GridConstants.TileSize);
        enemy.Died += () => OnEnemyKilled(enemy);
        _enemies.AddChild(enemy);
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        _goldCollected += Mathf.RoundToInt(enemy.Data.Value * _goldMultiplier);
        _totalGoldLabel.Text = $"Total Gold: {_progress.MetaCurrency + _goldCollected}";
        ResolveEnemy();
    }

    private void OnGoalEntered(Area2D area)
    {
        if (area is Enemy enemy)
        {
            // TBD (ROADMAP Fázis 4): ez a helyi _hp majd a RunState autoloadba
            // költözik, amikor a teljes statisztika/skill fa kör megépül.
            _hp = Mathf.Max(0, _hp - Mathf.CeilToInt(enemy.Data.Dmg));
            UpdateHpBar();
            enemy.QueueFree();

            if (_hp <= 0)
            {
                EndRound(won: false);
                return;
            }

            ResolveEnemy();
        }
    }

    private void OnDamageDealt(string towerName, float amount)
    {
        if (!_roundActive) return;
        _damageByTower[towerName] = _damageByTower.GetValueOrDefault(towerName) + amount;
    }

    private void ResolveEnemy()
    {
        _enemiesResolved++;
        if (_roundActive && _enemiesSpawned >= _enemiesThisWave && _enemiesResolved >= _enemiesThisWave)
        {
            EndRound(won: true);
        }
    }

    private void EndRound(bool won)
    {
        if (!_roundActive) return;

        _roundActive = false;
        _spawnTimer.Stop();
        SetBuildingEnabled(true);

        foreach (var enemy in _enemies.GetChildren())
        {
            enemy.QueueFree();
        }

        ShowStatsPopup(won);

        _progress.MetaCurrency += _goldCollected;
        new LocalFileSaveProvider().Save(_progress);
    }

    private void ShowStatsPopup(bool won)
    {
        _statsTitleLabel.Text = won ? "Success!" : "Defeat!";
        _statsGoldLabel.Text = $"Gold collected this round: {_goldCollected}";

        foreach (var child in _damageListContainer.GetChildren())
        {
            child.QueueFree();
        }

        var elapsedSeconds = Mathf.Max(0.001f, (Time.GetTicksMsec() - _roundStartMsec) / 1000f);
        foreach (var entry in _damageByTower)
        {
            var dps = entry.Value / elapsedSeconds;
            _damageListContainer.AddChild(new Label
            {
                Text = $"{entry.Key} — Total: {entry.Value:0} dmg, {dps:0.0} dmg/sec",
            });
        }

        _statsPopup.Visible = true;
        _startRoundButton.Disabled = true;
    }

    private void OnCloseStatsPressed()
    {
        _statsPopup.Visible = false;
        _startRoundButton.Disabled = false;
    }

    private void OnDamageTogglePressed(Button button)
    {
        DamageTracker.ShowDamageNumbers = !DamageTracker.ShowDamageNumbers;
        UpdateDamageToggleText(button);
    }

    private static void UpdateDamageToggleText(Button button)
    {
        button.Text = DamageTracker.ShowDamageNumbers ? "Dmg Numbers: ON" : "Dmg Numbers: OFF";
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Main/MainMenu.tscn");
    }

    private void UpdateHpBar()
    {
        var pct = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
        var size = _hpBarFill.Size;
        size.X = HpBarMaxWidth * pct;
        _hpBarFill.Size = size;
        _hpBarLabel.Text = $"{_hp}/{_maxHp}";
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
