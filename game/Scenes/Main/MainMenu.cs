using System.Collections.Generic;
using Godot;
using TowerDefense.Data;
using TowerDefense.Levels;
using TowerDefense.Save;
using TowerDefense.UI;

namespace TowerDefense.MainMenu;

// Skill fa v2: hub + 4 fő ág (balra=Damage, fel=Defense, jobbra=Gold,
// le=Towers/Special), mindegyik 6 rétegen át elágazva (lásd BuildSkillTreeLayout).
// A legtöbb generált node egyelőre PLACEHOLDER (nincs tartalma, csak a forma
// látszik) — csak 6 node valódi/vásárolható, ugyanaz a 6 stat, ami korábban
// is megvolt, csak új pozícióban: dmg (Damage/L1), fireRate (Damage/L2),
// hp (Defense/L1), currency (Gold/L1), unlockSplash (Towers/L2), unlockSniper
// (Towers/L3). A csoportonkénti tartalom-feltöltés (mi legyen a többi node,
// milyen áron) külön kör, lásd GAMEPLAY.md "Skill fa".
public partial class MainMenu : Node2D
{
    private const int MaxLevel = 5;
    private const int MaxTowersLevel = 10;
    private const float NodeDiameter = 90f;
    private const float RealNodeDiameter = 72f;
    private const float PlaceholderNodeDiameter = 20f;
    private const float SkillSegment = 58f;
    private static readonly Vector2 SkillHubPosition = new(640, 340);

    private enum SkillBranch { Damage, Defense, Gold, Towers }

    private static readonly (SkillBranch Branch, Vector2 Dir)[] SkillBranchDirs =
    {
        (SkillBranch.Damage, new Vector2(-1, 0)),
        (SkillBranch.Defense, new Vector2(0, -1)),
        (SkillBranch.Gold, new Vector2(1, 0)),
        (SkillBranch.Towers, new Vector2(0, 1)),
    };

    // Fill/border pár minden ághoz — a placeholder node-ok ugyanezt a színt
    // kapják, csak elhalványítva (lásd BuildSkillNodes), hogy már üresen is
    // látszódjon, melyik ághoz tartoznak.
    private static readonly Dictionary<SkillBranch, (Color Fill, Color Border)> SkillBranchColors = new()
    {
        [SkillBranch.Damage] = (new Color(0.32f, 0.14f, 0.10f), new Color(0.85f, 0.40f, 0.25f)),
        [SkillBranch.Defense] = (new Color(0.10f, 0.16f, 0.32f), new Color(0.35f, 0.55f, 0.95f)),
        [SkillBranch.Gold] = (new Color(0.30f, 0.22f, 0.08f), new Color(0.85f, 0.65f, 0.25f)),
        [SkillBranch.Towers] = (new Color(0.20f, 0.14f, 0.32f), new Color(0.60f, 0.45f, 0.90f)),
    };
    private static readonly (Color Fill, Color Border) SkillHubColor = (new Color(0.16f, 0.16f, 0.18f), new Color(0.55f, 0.55f, 0.60f));

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
    // (PlayerProgress.GetSkillLevel baseline). 1->2, ..., 9->10 árak.
    // TBD, placeholder: a max szint 5->10 emelése (lásd MaxTowersLevel) friss,
    // a 6-10. szint ára még nincs véglegesítve — csoportonként nézzük át.
    private static readonly int[] TowerCosts = { 0, 100, 250, 500, 750, 1000, 1500, 2000, 3000, 4000 };

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

    // Melyik (ág, réteg) generált node kap valódi id-t — csak L1/L2/L3-on van
    // pontosan 1 node ágankánt (L4+ már 2-vel elágazik), ott fér el egyértelműen
    // a 6 meglévő stat. A többi generált node placeholder marad (Id = null).
    private static readonly Dictionary<(SkillBranch, int), string> SkillRealNodeIds = new()
    {
        [(SkillBranch.Damage, 1)] = "dmg",
        [(SkillBranch.Damage, 2)] = "fireRate",
        [(SkillBranch.Defense, 1)] = "hp",
        [(SkillBranch.Gold, 1)] = "currency",
        [(SkillBranch.Towers, 2)] = "unlockSplash",
        [(SkillBranch.Towers, 3)] = "unlockSniper",
    };

    private readonly List<(string Id, Vector2 Pos, SkillBranch Branch)> _skillNodes = new();
    private readonly List<(Vector2 From, Vector2 To, SkillBranch Branch)> _skillEdges = new();

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

    // A hub-ból 4 fő ág indul (Damage/Defense/Gold/Towers), mindegyik 6 rétegen
    // át — a pontos elágazási minta (mikor kanyarodik, mikor ágazik ketté) a
    // felhasználónak korábban bemutatott vizuális makett algoritmusát követi
    // 1:1-ben (lásd a session jegyzeteit): L1→L2 egyenes folytatás, utána a
    // PÁRATLAN rétegek (3, 5) 90°-ot fordulnak (egyetlen gyerek), a PÁROS
    // rétegek (4, 6) ±45°-ban kettéágaznak.
    private void BuildSkillTreeLayout()
    {
        _skillNodes.Clear();
        _skillEdges.Clear();

        foreach (var (branch, dir) in SkillBranchDirs)
        {
            var layer1Pos = SkillHubPosition + dir * SkillSegment;
            _skillEdges.Add((SkillHubPosition, layer1Pos, branch));
            _skillNodes.Add((SkillRealNodeIds.GetValueOrDefault((branch, 1)), layer1Pos, branch));

            BuildSkillBranch(layer1Pos, dir, 2, 1, branch);
        }
    }

    private void BuildSkillBranch(Vector2 parentPos, Vector2 dir, int layer, int spin, SkillBranch branch)
    {
        if (layer > 6) return;

        var pos = parentPos + dir * SkillSegment;
        _skillEdges.Add((parentPos, pos, branch));
        _skillNodes.Add((SkillRealNodeIds.GetValueOrDefault((branch, layer)), pos, branch));

        if (layer % 2 == 1)
        {
            // Páratlan réteg: egyenesen tovább, 90°-ot fordulva (a "kanyar").
            var turned = dir.Rotated(Mathf.DegToRad(90f * spin));
            BuildSkillBranch(pos, turned, layer + 1, spin, branch);
        }
        else
        {
            // Páros réteg: kettéágazik, ±45°-ban.
            BuildSkillBranch(pos, dir.Rotated(Mathf.DegToRad(45f)), layer + 1, 1, branch);
            BuildSkillBranch(pos, dir.Rotated(Mathf.DegToRad(-45f)), layer + 1, -1, branch);
        }
    }

    private void BuildSkillNodes()
    {
        BuildSkillTreeLayout();

        var hubStyle = MakeNodeStyle(SkillHubColor.Fill, SkillHubColor.Border, NodeDiameter, 3);
        BuildHubButton(hubStyle);

        foreach (var (id, pos, branch) in _skillNodes)
        {
            var (fill, border) = SkillBranchColors[branch];

            if (id != null)
            {
                BuildRealNodeButton(id, pos, fill, border);
            }
            else
            {
                BuildPlaceholderNode(pos, fill, border);
            }
        }
    }

    private void BuildHubButton(StyleBoxFlat style)
    {
        var button = new Button
        {
            Position = SkillHubPosition - new Vector2(NodeDiameter, NodeDiameter) / 2f,
            CustomMinimumSize = new Vector2(NodeDiameter, NodeDiameter),
            Size = new Vector2(NodeDiameter, NodeDiameter),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ClipText = true,
        };
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeFontSizeOverride("font_size", 12);
        button.Pressed += () => OnNodePressed("towers");

        _canvasLayer.AddChild(button);
        _buttons["towers"] = button;
    }

    private void BuildRealNodeButton(string nodeId, Vector2 pos, Color fill, Color border)
    {
        var normalStyle = MakeNodeStyle(fill, border, RealNodeDiameter, 3);
        var hoverStyle = MakeNodeStyle(fill.Lightened(0.1f), border.Lightened(0.15f), RealNodeDiameter, 3);
        var pressedStyle = MakeNodeStyle(fill.Darkened(0.1f), border, RealNodeDiameter, 3);

        var button = new Button
        {
            Position = pos - new Vector2(RealNodeDiameter, RealNodeDiameter) / 2f,
            CustomMinimumSize = new Vector2(RealNodeDiameter, RealNodeDiameter),
            Size = new Vector2(RealNodeDiameter, RealNodeDiameter),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ClipText = true,
        };
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeFontSizeOverride("font_size", 11);
        button.Pressed += () => OnNodePressed(nodeId);

        _canvasLayer.AddChild(button);
        _buttons[nodeId] = button;
    }

    // Üres node — még nincs eldöntve, mi legyen rajta, csak a FORMÁT mutatja.
    // Letiltott, a saját ága színében, elhalványítva (a felhasználó kérése:
    // "minden node a saját nagy típusának megfelelő keretet és hátteret kapjon").
    private void BuildPlaceholderNode(Vector2 pos, Color fill, Color border)
    {
        var dimStyle = MakeNodeStyle(
            new Color(fill.R, fill.G, fill.B, 0.35f),
            new Color(border.R, border.G, border.B, 0.5f),
            PlaceholderNodeDiameter,
            2);

        var button = new Button
        {
            Position = pos - new Vector2(PlaceholderNodeDiameter, PlaceholderNodeDiameter) / 2f,
            CustomMinimumSize = new Vector2(PlaceholderNodeDiameter, PlaceholderNodeDiameter),
            Size = new Vector2(PlaceholderNodeDiameter, PlaceholderNodeDiameter),
            Disabled = true,
            TooltipText = "Coming soon",
        };
        button.AddThemeStyleboxOverride("disabled", dimStyle);

        _canvasLayer.AddChild(button);
    }

    private static StyleBoxFlat MakeNodeStyle(Color fill, Color border, float diameter, int borderWidth)
    {
        var style = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthTop = borderWidth,
            BorderWidthBottom = borderWidth,
            BorderWidthLeft = borderWidth,
            BorderWidthRight = borderWidth,
            CornerRadiusTopLeft = (int)(diameter / 2f),
            CornerRadiusTopRight = (int)(diameter / 2f),
            CornerRadiusBottomLeft = (int)(diameter / 2f),
            CornerRadiusBottomRight = (int)(diameter / 2f),
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
        "towers" => MaxTowersLevel,
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
        foreach (var (from, to, branch) in _skillEdges)
        {
            var (_, border) = SkillBranchColors[branch];
            var dir = (to - from).Normalized();
            DrawLine(from + dir * 4f, to - dir * 4f, new Color(border.R, border.G, border.B, 0.5f), 2f);
        }
    }
}
