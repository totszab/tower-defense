using System.Collections.Generic;
using Godot;
using TowerDefense.Data;
using TowerDefense.Levels;
using TowerDefense.Save;
using TowerDefense.UI;

namespace TowerDefense.MainMenu;

// Bootstrap skill fa: 5 node (hub + 4 irány), lásd GAMEPLAY.md "Skill fa".
// Node id -> irány: towers=hub, hp=fel, fireRate=le, currency=jobb, dmg=bal.
public partial class MainMenu : Node2D
{
    private const int MaxLevel = 5;
    private const float NodeDiameter = 110f;

    // Csak Level 1 létezik egyelőre, 1-5. kör tartalommal (6-10 TBD, lásd
    // GAMEPLAY.md "Pályák"). Ha ennél több kör kap tartalmat, ezt bővíteni kell.
    // (LevelBuild.MaxPlayableRound ugyanezt a tényt tükrözi, szándékosan duplikált.)
    private const int PlayableRounds = 5;
    private const int TotalRoundsPerLevel = 10;

    private static readonly int[] DmgHpCosts = { 5, 10, 20, 35, 50 };
    private static readonly int[] CurrencyCosts = { 50, 150, 300, 500, 1000 };

    // Index 0 sosem kerül lekérdezésre — a "towers" mindig >=1 szinten van
    // (PlayerProgress.GetSkillLevel baseline). 1->2, 2->3, 3->4, 4->5 árak.
    private static readonly int[] TowerCosts = { 0, 100, 250, 500, 750 };

    // TBD: a tűzgyorsaság node árát nem adta meg a design — egyelőre a
    // sebzés/élet görbét használjuk placeholderként.
    private static readonly int[] FireRateCosts = DmgHpCosts;

    // Kézzel karbantartott lista — nincs központi "minden ellenség" registry,
    // amikor új ellenségtípus készül, ide is fel kell venni.
    private static readonly string[] CodexEnemyPaths =
    {
        "res://Data/Enemies/enemy_basic.tres",
        "res://Data/Enemies/blue_slime.tres",
        "res://Data/Enemies/blue_slime_boss.tres",
    };

    private static readonly Dictionary<string, Vector2> NodePositions = new()
    {
        ["towers"] = new Vector2(640, 340),
        ["hp"] = new Vector2(640, 200),
        ["fireRate"] = new Vector2(640, 480),
        ["currency"] = new Vector2(820, 340),
        ["dmg"] = new Vector2(460, 340),
    };

    // Tooltip (hover) szöveg: az egy szintnyi (marginális) hatás, angolul.
    private static readonly Dictionary<string, string> NodePerLevelText = new()
    {
        ["towers"] = "+1 tower",
        ["hp"] = "+2 health",
        ["currency"] = "+10% gold",
        ["dmg"] = "+1 damage",
        ["fireRate"] = "+5% attack speed",
    };

    private readonly Dictionary<string, Button> _buttons = new();
    private readonly List<Button> _roundButtons = new();
    private PlayerProgress _progress;
    private Label _goldLabel;
    private CanvasLayer _canvasLayer;
    private Panel _codexPopup;
    private Panel _levelSelectPopup;

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

            // Fontos a property-sorrend: ExpandMode-nak a Texture beállítása ELŐTT
            // kell állnia, különben a minimum-méret a natív textúraméret alapján
            // rögzül, és a Size beállítása arra clampelődik.
            var icon = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Texture = data.Sprite,
                Modulate = data.Tint,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

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
        _ => CurrencyCosts,
    };

    // A node kompakt (hover nélküli) szövege: a JELENLEGI kumulált hatás + szint.
    private static string CumulativeText(string nodeId, int level) => nodeId switch
    {
        "towers" => $"{level} towers",
        "hp" => $"+{level * 2} health",
        "currency" => $"+{level * 10}% gold",
        "dmg" => $"+{level} damage",
        "fireRate" => $"+{level * 5}% attack speed",
        _ => "",
    };

    private void OnNodePressed(string nodeId)
    {
        var level = _progress.GetSkillLevel(nodeId);
        if (level >= MaxLevel) return;

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

        // Csak a ténylegesen tartalommal rendelkező körök disabled-állapota
        // változhat a progressz szerint — a PlayableRounds utániak véglegesen
        // le vannak tiltva (nincs mit betölteni), ne írjuk felül.
        for (var i = 0; i < PlayableRounds; i++)
        {
            _roundButtons[i].Disabled = i + 1 > _progress.HighestUnlockedRound;
        }

        foreach (var entry in _buttons)
        {
            var nodeId = entry.Key;
            var button = entry.Value;
            var level = _progress.GetSkillLevel(nodeId);
            var costs = CostsFor(nodeId);

            button.Text = $"{CumulativeText(nodeId, level)}\n{level}/{MaxLevel}";
            button.TooltipText = level >= MaxLevel
                ? $"{NodePerLevelText[nodeId]}\nMAX LEVEL"
                : $"{NodePerLevelText[nodeId]}\nCost: {costs[level]} gold";
        }
    }

    private void BuildPlaySystem()
    {
        _levelSelectPopup = GetNode<Panel>("CanvasLayer/LevelSelectPopup");
        GetNode<Button>("CanvasLayer/PlayButton").Pressed += () => _levelSelectPopup.Visible = true;
        GetNode<Button>("CanvasLayer/LevelSelectPopup/CloseButton").Pressed += () => _levelSelectPopup.Visible = false;

        // Csak Level 1 létezik — a "Level 1" gomb egyelőre dísz (mindig az
        // egyetlen, automatikusan kiválasztott opciót mutatja), de előkészíti
        // a UI-t arra, ha majd több pálya lesz (GAMEPLAY.md "Pályák és körök").
        var roundGrid = GetNode<GridContainer>("CanvasLayer/LevelSelectPopup/RoundGrid");
        for (var round = 1; round <= TotalRoundsPerLevel; round++)
        {
            var roundNumber = round;
            var button = new Button { CustomMinimumSize = new Vector2(130, 60), Text = $"Round {round}" };

            if (round > PlayableRounds)
            {
                button.Disabled = true;
                button.TooltipText = "Coming soon";
            }
            else
            {
                button.Disabled = round > _progress.HighestUnlockedRound;
            }

            button.Pressed += () => OnRoundPressed(roundNumber);
            roundGrid.AddChild(button);
            _roundButtons.Add(button);
        }
    }

    private void OnRoundPressed(int roundNumber)
    {
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
