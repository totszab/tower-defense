using System;
using System.Collections.Generic;
using Godot;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Enemies;
using TowerDefense.Save;
using TowerDefense.Towers;
using TowerDefense.UI;

namespace TowerDefense.Levels;

// Level 1 kör-lejátszó. Egyelőre csak Level 1 létezik (mind a 10 köre kész
// tartalommal) — több pálya esetén ez a "melyik pálya" dimenzióval bővül majd
// (GAMEPLAY.md "Pályák", PlayerProgress.HighestUnlockedRound megjegyzése).
public partial class LevelBuild : Node2D
{
    // MainMenu ezt állítja be ChangeSceneToFile előtt (nincs egyszerűbb módja
    // paramétert átadni egy scene-váltásnak Godotban).
    public static int RequestedRoundNumber = 1;

    private const int TotalRoundsPerLevel = 10;

    // Mind a 10 körnek van WaveData tartalma (lásd MainMenu.PlayableRounds —
    // a két konstans szándékosan duplikált, mindkettő ugyanazt a tényt tükrözi).
    private const int MaxPlayableRound = 10;
    private const int Columns = 13;
    private const int PathRow = 1;
    private static readonly int[] BuildableRows = { 0, 2 };

    // Bootstrap value only — real per-level numbers belong in a Resource
    // once real levels exist (GAMEPLAY.md "Pályák" TBD).
    private const int BaseStartingHp = 3;

    [Export] public PackedScene[] AvailableTowers { get; set; } = Array.Empty<PackedScene>();
    [Export] public PackedScene EnemyScene { get; set; }

    private readonly Dictionary<Vector2I, Tower> _towersByTile = new();
    private Tower _selectedInfoTower;
    private Panel _towerInfoPopup;
    private Label _towerInfoLabel;
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
    private Label _roundLabel;
    private VBoxContainer _enemyBreakdown;
    private Label _towerCountLabel;
    private Button _startRoundButton;
    private Button _backButton;
    private Button _resetTowersButton;
    private Timer _spawnTimer;

    private Panel _statsPopup;
    private Label _statsTitleLabel;
    private Label _statsGoldLabel;
    private VBoxContainer _damageListContainer;
    private Button _nextRoundButton;
    private Button _savePresetButton;

    private PlayerProgress _progress;
    private int _roundNumber;
    private WaveData _wave;
    private int _currentStepIndex;
    private int _totalEnemiesThisWave;
    private int _maxTowers;
    private float _goldMultiplier;

    private readonly Dictionary<string, float> _damageByTower = new();
    private ulong _roundStartMsec;

    private int _hp;
    private int _goldCollected;
    private int _enemiesResolved;
    private bool _roundActive;

    public override void _Ready()
    {
        _towers = GetNode<Node2D>("Towers");
        _enemies = GetNode<Node2D>("Enemies");
        _hpBarFill = GetNode<ColorRect>("CanvasLayer/HpBarBg/HpBarFill");
        _hpBarLabel = GetNode<Label>("CanvasLayer/HpBarBg/HpBarLabel");
        _totalGoldLabel = GetNode<Label>("CanvasLayer/TotalGoldLabel");
        _roundLabel = GetNode<Label>("CanvasLayer/EnemyPanel/RoundLabel");
        _enemyBreakdown = GetNode<VBoxContainer>("CanvasLayer/EnemyPanel/Breakdown");
        _towerCountLabel = GetNode<Label>("CanvasLayer/RightPanel/TowerCountLabel");
        _startRoundButton = GetNode<Button>("CanvasLayer/RightPanel/StartRoundButton");
        _backButton = GetNode<Button>("CanvasLayer/RightPanel/BackButton");
        _resetTowersButton = GetNode<Button>("CanvasLayer/RightPanel/ResetTowersButton");
        _spawnTimer = GetNode<Timer>("SpawnTimer");

        _statsPopup = GetNode<Panel>("CanvasLayer/StatsPopup");
        _statsTitleLabel = GetNode<Label>("CanvasLayer/StatsPopup/TitleLabel");
        _statsGoldLabel = GetNode<Label>("CanvasLayer/StatsPopup/RoundGoldLabel");
        _damageListContainer = GetNode<VBoxContainer>("CanvasLayer/StatsPopup/DamageList");
        _nextRoundButton = GetNode<Button>("CanvasLayer/StatsPopup/NextRoundButton");
        _savePresetButton = GetNode<Button>("CanvasLayer/StatsPopup/SavePresetButton");

        _towerInfoPopup = GetNode<Panel>("CanvasLayer/TowerInfoPopup");
        _towerInfoLabel = GetNode<Label>("CanvasLayer/TowerInfoPopup/InfoLabel");
        GetNode<Button>("CanvasLayer/TowerInfoPopup/CloseButton").Pressed += HideTowerInfo;
        _towerInfoPopup.Visible = false;

        _startRoundButton.Pressed += OnStartOrRetreatPressed;
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        GetNode<Area2D>("GoalArea").AreaEntered += OnGoalEntered;
        _backButton.Pressed += OnBackPressed;
        GetNode<Button>("CanvasLayer/StatsPopup/CloseButton").Pressed += OnCloseStatsPressed;
        _nextRoundButton.Pressed += OnNextRoundPressed;
        _savePresetButton.Pressed += SavePreset;
        _resetTowersButton.Pressed += OnResetTowersPressed;
        var dmgToggle = GetNode<Button>("CanvasLayer/DamageTogglePanel/DamageToggleButton");
        dmgToggle.Pressed += () => OnDamageTogglePressed(dmgToggle);
        UpdateDamageToggleText(dmgToggle);
        DamageTracker.DamageDealt += OnDamageDealt;

        BuildTowerPalette();

        _roundNumber = RequestedRoundNumber;
        _wave = GD.Load<WaveData>($"res://Data/Waves/Level1/Round{_roundNumber}.tres");
        _totalEnemiesThisWave = _wave.TotalEnemyCount();

        _progress = new LocalFileSaveProvider().Load();
        // "towers" node legalább 1-en indul (lásd PlayerProgress.GetSkillLevel), így
        // egy friss mentésnél is lerakható az első torony.
        _maxTowers = _progress.GetSkillLevel("towers");
        _goldMultiplier = 1f + _progress.GetSkillLevel("currency") * 0.10f;

        _maxHp = BaseStartingHp + _progress.GetSkillLevel("hp") * 2;
        _hp = _maxHp;
        UpdateHpBar();
        _totalGoldLabel.Text = $"Total Gold: {_progress.MetaCurrency}";
        _roundLabel.Text = $"Round {_roundNumber}";
        _towerCountLabel.Text = $"Towers: 0/{_maxTowers}";
        _statsPopup.Visible = false;

        // Placeholder arany-ikon a "Total Gold" felirat ELÉ.
        var coin = UiHelpers.MakeCircle(new Vector2(20, 20), Colors.Gold);
        coin.Position = new Vector2(10, 49);
        GetNode<CanvasLayer>("CanvasLayer").AddChild(coin);

        BuildEnemyBreakdown();
        ApplyPreset();

        QueueRedraw();
    }

    // Típusonként külön sor (ikon + "Nx Name"), ne egy összesített szám —
    // Round 3-nál pl. "10x Green Slime" ÉS "5x Blue Slime" külön sorban.
    private void BuildEnemyBreakdown()
    {
        var counts = new Dictionary<EnemyData, int>();
        var order = new List<EnemyData>();

        foreach (var step in _wave.Steps)
        {
            if (!counts.ContainsKey(step.Enemy))
            {
                counts[step.Enemy] = 0;
                order.Add(step.Enemy);
            }

            counts[step.Enemy] += step.Count;
        }

        foreach (var data in order)
        {
            var row = new HBoxContainer();

            var icon = UiHelpers.MakeEnemyIcon(data);
            icon.CustomMinimumSize = new Vector2(22, 22);
            var label = new Label { Text = $"{counts[data]}x {data.DisplayName}" };

            row.AddChild(icon);
            row.AddChild(label);
            _enemyBreakdown.AddChild(row);
        }
    }

    public override void _ExitTree()
    {
        DamageTracker.DamageDealt -= OnDamageDealt;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            OnBoardClicked(GetLocalMousePosition());
        }
    }

    // _Input (nem _UnhandledInput) fut le MINDEN kattintásra, még azokra is,
    // amiket egy GUI Control (gomb, panel) elnyel — enélkül a torony-infó
    // popup csak akkor tűnt el, ha pont a pályára kattintottunk. Csak akkor
    // csinál bármit, ha a kattintás a pályán KÍVÜL esik, hogy a
    // _UnhandledInput-beli toggle-logikát (ugyanarra a toronyra kattintva
    // becsukja) ne írja felül.
    public override void _Input(InputEvent @event)
    {
        if (_selectedInfoTower == null) return;
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton) return;

        var boardRect = new Rect2(GlobalPosition, new Vector2(Columns * GridConstants.TileSize, 3 * GridConstants.TileSize));
        if (!boardRect.HasPoint(mouseButton.Position))
        {
            HideTowerInfo();
        }
    }

    private void OnBoardClicked(Vector2 localPos)
    {
        var tile = new Vector2I(
            Mathf.FloorToInt(localPos.X / GridConstants.TileSize),
            Mathf.FloorToInt(localPos.Y / GridConstants.TileSize));

        if (_towersByTile.TryGetValue(tile, out var existingTower))
        {
            ToggleTowerInfo(existingTower);
            return;
        }

        HideTowerInfo();
        TryPlaceTower(tile);
    }

    private void BuildTowerPalette()
    {
        var rightPanel = GetNode<Control>("CanvasLayer/RightPanel");
        _towerPalette = new VBoxContainer { Position = new Vector2(20, 70) };
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

    private void TryPlaceTower(Vector2I tile)
    {
        if (!_buildingEnabled) return;
        if (_selectedTowerScene == null) return;
        if (_towersByTile.Count >= _maxTowers) return;

        if (tile.X < 0 || tile.X >= Columns) return;
        if (Array.IndexOf(BuildableRows, tile.Y) < 0) return;
        if (_towersByTile.ContainsKey(tile)) return;

        var tower = _selectedTowerScene.Instantiate<Tower>();
        tower.Position = new Vector2(
            (tile.X + 0.5f) * GridConstants.TileSize,
            (tile.Y + 0.5f) * GridConstants.TileSize);
        _towers.AddChild(tower);
        _towersByTile[tile] = tower;
        _towerCountLabel.Text = $"Towers: {_towersByTile.Count}/{_maxTowers}";
    }

    private void ToggleTowerInfo(Tower tower)
    {
        if (_selectedInfoTower == tower)
        {
            HideTowerInfo();
            return;
        }

        _selectedInfoTower = tower;
        _towerInfoPopup.Visible = true;
        _towerInfoLabel.Text =
            $"{tower.Data.DisplayName}\n" +
            $"Damage: {tower.EffectiveDamage:0.#}\n" +
            $"Range: {tower.Data.Range:0.#} tiles\n" +
            $"Fire Rate: {tower.EffectiveFireRate:0.##}/sec";
        _towerInfoPopup.Position = tower.GlobalPosition + new Vector2(40, -90);
        QueueRedraw();
    }

    private void HideTowerInfo()
    {
        if (_selectedInfoTower == null) return;
        _selectedInfoTower = null;
        _towerInfoPopup.Visible = false;
        QueueRedraw();
    }

    private void SetBuildingEnabled(bool enabled)
    {
        _buildingEnabled = enabled;
        _resetTowersButton.Disabled = !enabled;
        foreach (var child in _towerPalette.GetChildren())
        {
            if (child is BaseButton button)
            {
                button.Disabled = !enabled;
            }
        }
    }

    private void OnResetTowersPressed()
    {
        if (!_buildingEnabled) return;

        foreach (var tower in _towersByTile.Values)
        {
            tower.QueueFree();
        }

        _towersByTile.Clear();
        _towerCountLabel.Text = $"Towers: 0/{_maxTowers}";
        HideTowerInfo();
    }

    // Ha van mentett preset ehhez a pályához (Level 1 bármelyik köréhez közös),
    // automatikusan lerakja induláskor — a _maxTowers/_towersByTile-nak már
    // készen kell állnia, mielőtt ez lefut.
    private void ApplyPreset()
    {
        foreach (var entry in _progress.Level1Preset)
        {
            if (_towersByTile.Count >= _maxTowers) break;

            var tile = new Vector2I(entry.TileX, entry.TileY);
            if (_towersByTile.ContainsKey(tile)) continue;

            PackedScene scene = null;
            foreach (var candidate in AvailableTowers)
            {
                if (candidate.ResourcePath == entry.TowerScenePath)
                {
                    scene = candidate;
                    break;
                }
            }
            if (scene == null) continue;

            var tower = scene.Instantiate<Tower>();
            tower.Position = new Vector2(
                (tile.X + 0.5f) * GridConstants.TileSize,
                (tile.Y + 0.5f) * GridConstants.TileSize);
            _towers.AddChild(tower);
            _towersByTile[tile] = tower;
        }

        _towerCountLabel.Text = $"Towers: {_towersByTile.Count}/{_maxTowers}";
    }

    private void SavePreset()
    {
        var preset = new List<PresetTowerEntry>();
        foreach (var entry in _towersByTile)
        {
            preset.Add(new PresetTowerEntry
            {
                TileX = entry.Key.X,
                TileY = entry.Key.Y,
                TowerScenePath = entry.Value.SceneFilePath,
            });
        }

        _progress.Level1Preset = preset;
        new LocalFileSaveProvider().Save(_progress);
    }

    private void OnStartOrRetreatPressed()
    {
        if (_roundActive)
        {
            // Retreat: a kör azonnal véget ér, de az addig gyűjtött arany/statisztika
            // megmarad — nem büntetjük a kilépést, csak korábban zárja le a kört.
            EndRound("Retreated", success: false);
        }
        else
        {
            StartRound();
        }
    }

    private void StartRound()
    {
        _roundActive = true;
        _goldCollected = 0;
        _currentStepIndex = 0;
        _enemiesResolved = 0;
        _damageByTower.Clear();
        _roundStartMsec = Time.GetTicksMsec();
        SetBuildingEnabled(false);
        _startRoundButton.Text = "Retreat";
        _backButton.Disabled = true;

        SpawnStep();
        _currentStepIndex = 1;
        _spawnTimer.WaitTime = _wave.SpawnInterval;
        _spawnTimer.Start();
    }

    private void OnSpawnTimerTimeout()
    {
        if (_currentStepIndex >= _wave.Steps.Length)
        {
            _spawnTimer.Stop();
            return;
        }

        SpawnStep();
        _currentStepIndex++;
        if (_currentStepIndex >= _wave.Steps.Length)
        {
            _spawnTimer.Stop();
        }
    }

    private void SpawnStep()
    {
        var step = _wave.Steps[_currentStepIndex];
        for (var i = 0; i < step.Count; i++)
        {
            SpawnEnemy(step.Enemy, i);
        }
    }

    private void SpawnEnemy(EnemyData data, int spawnIndexInStep = 0)
    {
        var enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Data = data;
        // Egy tick-en belül a batch tagjai ne fedjék teljesen egymást — egy
        // tile-nyi hézaggal "mögé" spawnolnak, hogy látszódjon, hányan jönnek.
        var xOffset = -spawnIndexInStep * GridConstants.TileSize;
        enemy.Position = new Vector2(xOffset, (PathRow + 0.5f) * GridConstants.TileSize);
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
        // Ha egy torony pont abban a frame-ben öli meg az ellenséget, amikor
        // az a célba ér (a QueueFree() csak a frame végén törli a node-ot),
        // ez a signal még lefuthatna egy már halott ellenségre — dupla
        // kör-teljesítést és jogtalan HP-levonást okozva.
        if (area is Enemy enemy && !enemy.IsDead)
        {
            // TBD (ROADMAP Fázis 4): ez a helyi _hp majd a RunState autoloadba
            // költözik, amikor a teljes statisztika/skill fa kör megépül.
            _hp = Mathf.Max(0, _hp - Mathf.CeilToInt(enemy.Data.Dmg));
            UpdateHpBar();
            enemy.QueueFree();

            if (_hp <= 0)
            {
                EndRound("Defeat!", success: false);
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
        if (_roundActive && _enemiesResolved >= _totalEnemiesThisWave)
        {
            EndRound("Success!", success: true);
        }
    }

    private void EndRound(string outcomeTitle, bool success)
    {
        if (!_roundActive) return;

        _roundActive = false;
        _spawnTimer.Stop();
        SetBuildingEnabled(true);
        _backButton.Disabled = false;
        _startRoundButton.Text = "Start Round";

        foreach (var enemy in _enemies.GetChildren())
        {
            enemy.QueueFree();
        }

        ShowStatsPopup(outcomeTitle, success);

        _progress.MetaCurrency += _goldCollected;

        if (success && _roundNumber >= _progress.HighestUnlockedRound)
        {
            _progress.HighestUnlockedRound = Math.Min(_roundNumber + 1, TotalRoundsPerLevel);
        }

        new LocalFileSaveProvider().Save(_progress);
    }

    private void ShowStatsPopup(string outcomeTitle, bool success)
    {
        _statsTitleLabel.Text = outcomeTitle;
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

        _nextRoundButton.Visible = success && _roundNumber < MaxPlayableRound;
        _statsPopup.Visible = true;
        _startRoundButton.Disabled = true;
    }

    private void OnCloseStatsPressed()
    {
        _statsPopup.Visible = false;
        _startRoundButton.Disabled = false;
    }

    private void OnNextRoundPressed()
    {
        RequestedRoundNumber = _roundNumber + 1;
        GetTree().ChangeSceneToFile("res://Scenes/Levels/Level01Test.tscn");
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

        if (_selectedInfoTower != null && IsInstanceValid(_selectedInfoTower))
        {
            var radius = _selectedInfoTower.Data.Range * GridConstants.TileSize;
            DrawCircle(_selectedInfoTower.Position, radius, new Color(1f, 1f, 0.3f, 0.15f));
            DrawArc(_selectedInfoTower.Position, radius, 0f, Mathf.Tau, 48, new Color(1f, 1f, 0.3f, 0.7f), 2f);
        }
    }
}
