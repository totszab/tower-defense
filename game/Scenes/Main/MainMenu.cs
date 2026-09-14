using System.Collections.Generic;
using Godot;
using TowerDefense.Data;
using TowerDefense.Levels;
using TowerDefense.Save;
using TowerDefense.UI;

namespace TowerDefense.MainMenu;

// Bootstrap skill fa: hub + 4 irány (0-5 szintes upgrade node-ok) + 2 diagonális
// egyszeri torony-unlock node, lásd GAMEPLAY.md "Skill fa".
// Node id -> irány: towers=hub, hp=fel, fireRate=le, currency=jobb, dmg=bal,
// unlockSplash=jobb-fent, unlockSniper=jobb-lent.
public partial class MainMenu : Node2D
{
    private const int MaxLevel = 5;
    private const float NodeDiameter = 110f;

    // Minden pálya mind a 10 körének van tartalma (lásd GAMEPLAY.md "Pályák").
    // (LevelBuild.MaxPlayableRound ugyanezt a tényt tükrözi, szándékosan duplikált.)
    private const int TotalRoundsPerLevel = 10;

    // Sorrend, amiben a pályák egymást feloldják (lásd LevelBuild.LevelOrder —
    // szándékosan duplikált, mindkettő ugyanazt a tényt tükrözi).
    private static readonly string[] LevelIds = { "Level1", "Level2", "Level3" };
    private static readonly Dictionary<string, string> LevelDisplayNames = new()
    {
        ["Level1"] = "Level 1",
        ["Level2"] = "Level 2",
        ["Level3"] = "Level 3",
    };

    private static readonly int[] DmgHpCosts = { 5, 10, 20, 35, 50 };
    private static readonly int[] CurrencyCosts = { 50, 150, 300, 500, 1000 };

    // Index 0 sosem kerül lekérdezésre — a "towers" mindig >=1 szinten van
    // (PlayerProgress.GetSkillLevel baseline). 1->2, 2->3, 3->4, 4->5 árak.
    private static readonly int[] TowerCosts = { 0, 100, 250, 500, 750 };

    // TBD: a tűzgyorsaság node árát nem adta meg a design — egyelőre a
    // sebzés/élet görbét használjuk placeholderként.
    private static readonly int[] FireRateCosts = DmgHpCosts;

    // Egyszeri (1 szintes, nem 0-5) unlock node-ok — a torony TÍPUSOK a skill
    // fából nyílnak, nem a pálya-progressztől függenek (lásd LevelBuild.TowerUnlocks).
    private static readonly int[] UnlockSplashCosts = { 150 };
    private static readonly int[] UnlockSniperCosts = { 400 };

    // Kézzel karbantartott lista — nincs központi "minden ellenség" registry,
    // amikor új ellenségtípus készül, ide is fel kell venni.
    private static readonly string[] CodexEnemyPaths =
    {
        "res://Data/Enemies/enemy_basic.tres",
        "res://Data/Enemies/blue_slime.tres",
        "res://Data/Enemies/purple_slime.tres",
        "res://Data/Enemies/blue_slime_boss.tres",
        "res://Data/Enemies/green_triangle.tres",
        "res://Data/Enemies/blue_triangle.tres",
        "res://Data/Enemies/purple_triangle.tres",
        "res://Data/Enemies/purple_slime_boss.tres",
        "res://Data/Enemies/red_slime.tres",
        "res://Data/Enemies/orange_slime.tres",
        "res://Data/Enemies/red_triangle.tres",
        "res://Data/Enemies/orange_triangle.tres",
        "res://Data/Enemies/red_slime_boss.tres",
        "res://Data/Enemies/orange_slime_boss.tres",
        "res://Data/Enemies/cyan_slime.tres",
        "res://Data/Enemies/magenta_slime.tres",
        "res://Data/Enemies/cyan_triangle.tres",
        "res://Data/Enemies/magenta_triangle.tres",
        "res://Data/Enemies/cyan_slime_boss.tres",
        "res://Data/Enemies/magenta_slime_boss.tres",
    };

    private static readonly Dictionary<string, Vector2> NodePositions = new()
    {
        ["towers"] = new Vector2(640, 340),
        ["hp"] = new Vector2(640, 200),
        ["fireRate"] = new Vector2(640, 480),
        ["currency"] = new Vector2(820, 340),
        ["dmg"] = new Vector2(460, 340),
        ["unlockSplash"] = new Vector2(790, 220),
        ["unlockSniper"] = new Vector2(790, 460),
    };

    // Tooltip (hover) szöveg: az egy szintnyi (marginális) hatás, angolul.
    private static readonly Dictionary<string, string> NodePerLevelText = new()
    {
        ["towers"] = "+1 tower",
        ["hp"] = "+2 health",
        ["currency"] = "+10% gold",
        ["dmg"] = "+1 damage",
        ["fireRate"] = "+5% attack speed",
        ["unlockSplash"] = "Unlocks the Splash Tower (area damage)",
        ["unlockSniper"] = "Unlocks the Sniper Tower (long range, high damage)",
    };

    private readonly Dictionary<string, Button> _buttons = new();
    private readonly List<Button> _roundButtons = new();
    private readonly Dictionary<string, Button> _levelTabButtons = new();
    private PlayerProgress _progress;
    private Label _goldLabel;
    private CanvasLayer _canvasLayer;
    private Panel _codexPopup;
    private Panel _levelSelectPopup;
    private GridContainer _roundGrid;
    private string _selectedLevelId;

    public override void _Ready()
    {
        _progress = new LocalFileSaveProvider().Load();
        _canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
        _goldLabel = GetNode<Label>("CanvasLayer/GoldLabel");
        BuildPlaySystem();
        GetNode<Button>("CanvasLayer/AddGoldButton").Pressed += OnAddGoldPressed;
        GetNode<Button>("CanvasLayer/ResetButton").Pressed += OnResetPressed;

        // Placeholder arany-ikon a "Gold" felirat ELÉ.
        var coin = UiHelpers.MakeCircle(new Vector2(20, 20), Colors.Gold);
        coin.Position = new Vector2(20, 22);
        _canvasLayer.AddChild(coin);

        BuildSkillNodes();
        BuildCodex();
        RefreshUi();
        QueueRedraw();

        // A skill-fa node-ok (és minden más futásidőben hozzáadott elem) a
        // popupok UTÁN kerül a fába, tehát alapból FÖLÉJÜK rajzolódna ki —
        // ezért a popupokat a végén a gyerek-lista végére toljuk.
        _canvasLayer.MoveChild(_codexPopup, _canvasLayer.GetChildCount() - 1);
        _canvasLayer.MoveChild(_levelSelectPopup, _canvasLayer.GetChildCount() - 1);
    }

    private void BuildCodex()
    {
        _codexPopup = GetNode<Panel>("CanvasLayer/CodexPopup");
        GetNode<Button>("CanvasLayer/CodexButton").Pressed += () => _codexPopup.Visible = true;
        GetNode<Button>("CanvasLayer/CodexPopup/CloseButton").Pressed += () => _codexPopup.Visible = false;

        var cardStyle = UiHelpers.MakeOpaquePanelStyle(new Color(0.14f, 0.15f, 0.18f), new Color(0.35f, 0.37f, 0.42f));

        var grid = GetNode<GridContainer>("CanvasLayer/CodexPopup/IconGrid");
        foreach (var path in CodexEnemyPaths)
        {
            var data = GD.Load<EnemyData>(path);

            var card = new PanelContainer { CustomMinimumSize = new Vector2(80, 80) };
            card.AddThemeStyleboxOverride("panel", cardStyle);
            card.TooltipText = $"{data.DisplayName}\nHP: {data.Hp:0}\nDmg: {data.Dmg:0}\nGold: {data.Value}\nSpeed: {data.Speed:0.#} tiles/sec";

            var icon = UiHelpers.MakeEnemyIcon(data);
            card.AddChild(icon);
            grid.AddChild(card);
        }
    }

    private void OnAddGoldPressed()
    {
        _progress.MetaCurrency += 1000;
        new LocalFileSaveProvider().Save(_progress);
        RefreshUi();
    }

    private void OnResetPressed()
    {
        _progress = new PlayerProgress();
        new LocalFileSaveProvider().Save(_progress);
        RefreshUi();
    }

    private void BuildSkillNodes()
    {
        var normalStyle = MakeNodeStyle(new Color(0.10f, 0.16f, 0.32f), new Color(0.35f, 0.55f, 0.95f));
        var hoverStyle = MakeNodeStyle(new Color(0.16f, 0.24f, 0.46f), new Color(0.5f, 0.7f, 1f));
        var pressedStyle = MakeNodeStyle(new Color(0.08f, 0.13f, 0.26f), new Color(0.35f, 0.55f, 0.95f));

        foreach (var entry in NodePositions)
        {
            var nodeId = entry.Key;
            var center = entry.Value;

            var button = new Button
            {
                Position = center - new Vector2(NodeDiameter, NodeDiameter) / 2f,
                CustomMinimumSize = new Vector2(NodeDiameter, NodeDiameter),
                Size = new Vector2(NodeDiameter, NodeDiameter),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ClipText = true,
            };
            button.AddThemeStyleboxOverride("normal", normalStyle);
            button.AddThemeStyleboxOverride("hover", hoverStyle);
            button.AddThemeStyleboxOverride("pressed", pressedStyle);
            button.AddThemeColorOverride("font_color", Colors.White);
            button.AddThemeColorOverride("font_hover_color", Colors.White);
            button.Pressed += () => OnNodePressed(nodeId);

            _canvasLayer.AddChild(button);
            _buttons[nodeId] = button;
        }
    }

    private static StyleBoxFlat MakeNodeStyle(Color fill, Color border)
    {
        var style = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthTop = 3,
            BorderWidthBottom = 3,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            CornerRadiusTopLeft = (int)(NodeDiameter / 2f),
            CornerRadiusTopRight = (int)(NodeDiameter / 2f),
            CornerRadiusBottomLeft = (int)(NodeDiameter / 2f),
            CornerRadiusBottomRight = (int)(NodeDiameter / 2f),
        };
        return style;
    }

    private static int[] CostsFor(string nodeId) => nodeId switch
    {
        "dmg" or "hp" => DmgHpCosts,
        "towers" => TowerCosts,
        "fireRate" => FireRateCosts,
        "unlockSplash" => UnlockSplashCosts,
        "unlockSniper" => UnlockSniperCosts,
        _ => CurrencyCosts,
    };

    // A legtöbb node 0-5 szintes, de az egyszeri torony-unlockok csak 0 vagy 1
    // lehetnek (lásd UnlockSplashCosts/UnlockSniperCosts, egy elemű tömbök).
    private static int MaxLevelFor(string nodeId) => nodeId switch
    {
        "unlockSplash" or "unlockSniper" => 1,
        _ => MaxLevel,
    };

    // A node kompakt (hover nélküli) szövege: a JELENLEGI kumulált hatás + szint.
    private static string CumulativeText(string nodeId, int level) => nodeId switch
    {
        "towers" => $"{level} towers",
        "hp" => $"+{level * 2} health",
        "currency" => $"+{level * 10}% gold",
        "dmg" => $"+{level} damage",
        "fireRate" => $"+{level * 5}% attack speed",
        "unlockSplash" or "unlockSniper" => level >= 1 ? "Unlocked" : "Locked",
        _ => "",
    };

    private void OnNodePressed(string nodeId)
    {
        var level = _progress.GetSkillLevel(nodeId);
        if (level >= MaxLevelFor(nodeId)) return;

        var cost = CostsFor(nodeId)[level];
        if (_progress.MetaCurrency < cost) return;

        _progress.MetaCurrency -= cost;
        _progress.SkillLevels[nodeId] = level + 1;
        new LocalFileSaveProvider().Save(_progress);

        RefreshUi();
    }

    private void RefreshUi()
    {
        _goldLabel.Text = $"Gold: {_progress.MetaCurrency}";

        // Újraépíti a szint-fülek és a kör-gombok disabled-állapotát a jelenlegi
        // progressz szerint (pl. Reset Progress vagy egy kör teljesítése után).
        SelectLevel(_progress.IsLevelUnlocked(_selectedLevelId) ? _selectedLevelId : "Level1");

        foreach (var entry in _buttons)
        {
            var nodeId = entry.Key;
            var button = entry.Value;
            var level = _progress.GetSkillLevel(nodeId);
            var costs = CostsFor(nodeId);
            var maxLevel = MaxLevelFor(nodeId);

            button.Text = $"{CumulativeText(nodeId, level)}\n{level}/{maxLevel}";
            button.TooltipText = level >= maxLevel
                ? $"{NodePerLevelText[nodeId]}\n{(maxLevel == 1 ? "Unlocked" : "MAX LEVEL")}"
                : $"{NodePerLevelText[nodeId]}\nCost: {costs[level]} gold";
        }
    }

    private void BuildPlaySystem()
    {
        _levelSelectPopup = GetNode<Panel>("CanvasLayer/LevelSelectPopup");
        GetNode<Button>("CanvasLayer/PlayButton").Pressed += () => _levelSelectPopup.Visible = true;
        GetNode<Button>("CanvasLayer/LevelSelectPopup/CloseButton").Pressed += () => _levelSelectPopup.Visible = false;

        _roundGrid = GetNode<GridContainer>("CanvasLayer/LevelSelectPopup/RoundGrid");
        var levelTabs = GetNode<HBoxContainer>("CanvasLayer/LevelSelectPopup/LevelTabs");
        foreach (var levelId in LevelIds)
        {
            var button = new Button { CustomMinimumSize = new Vector2(140, 37), Text = LevelDisplayNames[levelId] };
            button.Pressed += () => SelectLevel(levelId);
            levelTabs.AddChild(button);
            _levelTabButtons[levelId] = button;
        }

        // Automatikusan a legutóbb játszott pálya kerüljön kiválasztásra — hacsak
        // az közben (pl. Reset Progress után) nem vált fel nem oldottá.
        SelectLevel(_progress.IsLevelUnlocked(_progress.LastPlayedLevelId) ? _progress.LastPlayedLevelId : "Level1");
    }

    private void SelectLevel(string levelId)
    {
        if (!_progress.IsLevelUnlocked(levelId)) return;
        _selectedLevelId = levelId;

        foreach (var entry in _levelTabButtons)
        {
            var unlocked = _progress.IsLevelUnlocked(entry.Key);
            // A jelenleg kiválasztott fület sem lehet újra lenyomni — ez jelzi
            // vizuálisan is, melyik pálya köreit látjuk lent.
            entry.Value.Disabled = !unlocked || entry.Key == levelId;
            entry.Value.TooltipText = unlocked ? "" : "Locked — clear the previous level first";
        }

        foreach (var child in _roundGrid.GetChildren())
        {
            child.QueueFree();
        }
        _roundButtons.Clear();

        for (var round = 1; round <= TotalRoundsPerLevel; round++)
        {
            var roundNumber = round;
            var button = new Button { CustomMinimumSize = new Vector2(130, 60), Text = $"Round {round}" };
            button.Disabled = round > _progress.GetHighestUnlockedRound(levelId);
            button.Pressed += () => OnRoundPressed(levelId, roundNumber);
            _roundGrid.AddChild(button);
            _roundButtons.Add(button);
        }
    }

    private void OnRoundPressed(string levelId, int roundNumber)
    {
        _progress.LastPlayedLevelId = levelId;
        new LocalFileSaveProvider().Save(_progress);
        LevelBuild.RequestedLevelId = levelId;
        LevelBuild.RequestedRoundNumber = roundNumber;
        GetTree().ChangeSceneToFile("res://Scenes/Levels/Level01Test.tscn");
    }

    public override void _Draw()
    {
        var hub = NodePositions["towers"];
        foreach (var entry in NodePositions)
        {
            if (entry.Key == "towers") continue;
            var dir = (entry.Value - hub).Normalized();
            var start = hub + dir * (NodeDiameter / 2f);
            var end = entry.Value - dir * (NodeDiameter / 2f);
            DrawLine(start, end, new Color(0.5f, 0.7f, 1f, 0.6f), 4f);
        }
    }
}
