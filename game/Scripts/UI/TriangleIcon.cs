using Godot;

namespace TowerDefense.UI;

// Kód-rajzolt háromszög ikon a háromszög alakú ellenségeknek (nincs saját
// sprite-juk) — Codex kártyák és az enemy breakdown sorok használják.
public partial class TriangleIcon : Control
{
    [Export] public Color TriangleColor { get; set; } = Colors.White;

    public override void _Ready()
    {
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        var w = Size.X;
        var h = Size.Y;
        var points = new[]
        {
            new Vector2(w * 0.5f, 0f),
            new Vector2(w, h),
            new Vector2(0f, h),
        };
        DrawColoredPolygon(points, TriangleColor);
    }
}
