using Godot;
using TowerDefense.Core;

namespace TowerDefense.UI;

public static class DamageNumberSpawner
{
    public static void Spawn(Node context, Vector2 globalPosition, float amount)
    {
        if (!DamageTracker.ShowDamageNumbers) return;

        var scene = GD.Load<PackedScene>("res://Scenes/UI/DamageNumber.tscn");
        var instance = scene.Instantiate<Node2D>();
        context.GetTree().CurrentScene.AddChild(instance);
        instance.GlobalPosition = globalPosition;
        ((DamageNumber)instance).SetValue(amount);
    }
}
