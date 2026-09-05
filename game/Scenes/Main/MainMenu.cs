using System.Collections.Generic;
using Godot;
using TowerDefense.Save;

namespace TowerDefense.MainMenu;

// Bootstrap skill fa: 5 node (hub + 4 irány), lásd GAMEPLAY.md "Skill fa".
// Node id -> irány: towers=hub, hp=fel, fireRate=le, currency=jobb, dmg=bal.
public partial class MainMenu : Node2D
{
    private const int MaxLevel = 5;
    private static readonly int[] DmgHpCosts = { 5, 10, 20, 35, 50 };
    private static readonly int[] CurrencyCosts = { 50, 150, 300, 500, 1000 };
    private static readonly int[] TowerCosts = { 50, 200, 500, 1000, 1500 };

    // TBD: a tűzgyorsaság node árát nem adta meg a design — egyelőre a
    // sebzés/élet görbét használjuk placeholderként.
    private static readonly int[] FireRateCosts = DmgHpCosts;

    private static readonly Dictionary<string, Vector2> NodePositions = new()
    {
        ["towers"] = new Vector2(500, 300),
        ["hp"] = new Vector2(500, 160),
        ["fireRate"] = new Vector2(500, 440),
        ["currency"] = new Vector2(680, 300),
        ["dmg"] = new Vector2(320, 300),
    };

    private readonly Dictionary<string, Button> _buttons = new();
    private PlayerProgress _progress;
    private Label _goldLabel;

    public override void _Ready()
    {
        _progress = new LocalFileSaveProvider().Load();
        _goldLabel = GetNode<Label>("CanvasLayer/GoldLabel");

        _buttons["towers"] = GetNode<Button>("CanvasLayer/HubButton");
        _buttons["hp"] = GetNode<Button>("CanvasLayer/UpButton");
        _buttons["fireRate"] = GetNode<Button>("CanvasLayer/DownButton");
        _buttons["currency"] = GetNode<Button>("CanvasLayer/RightButton");
        _buttons["dmg"] = GetNode<Button>("CanvasLayer/LeftButton");

        foreach (var entry in _buttons)
        {
            var nodeId = entry.Key;
            entry.Value.Pressed += () => OnNodePressed(nodeId);
        }

        GetNode<Button>("CanvasLayer/PlayButton").Pressed += OnPlayPressed;

        RefreshUi();
        QueueRedraw();
    }

    private static int[] CostsFor(string nodeId) => nodeId switch
    {
        "dmg" or "hp" => DmgHpCosts,
        "towers" => TowerCosts,
        "fireRate" => FireRateCosts,
        _ => CurrencyCosts,
    };

    private static string LabelFor(string nodeId) => nodeId switch
    {
        "dmg" => "Sebzés",
        "hp" => "Élet",
        "towers" => "Tornyok",
        "currency" => "Arany",
        "fireRate" => "Tűzgyorsaság",
        _ => nodeId,
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
        _goldLabel.Text = $"Arany: {_progress.MetaCurrency}";

        foreach (var entry in _buttons)
        {
            var nodeId = entry.Key;
            var level = _progress.GetSkillLevel(nodeId);
            var costs = CostsFor(nodeId);
            entry.Value.Text = level >= MaxLevel
                ? $"{LabelFor(nodeId)}\nMAX (5)"
                : $"{LabelFor(nodeId)}\nSzint {level}\n{costs[level]} arany";
        }
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Levels/Level01Test.tscn");
    }

    public override void _Draw()
    {
        var hub = NodePositions["towers"];
        foreach (var entry in NodePositions)
        {
            if (entry.Key == "towers") continue;
            DrawLine(hub, entry.Value, new Color(1f, 1f, 1f, 0.5f), 3f);
        }
    }
}
