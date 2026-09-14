using System.Collections.Generic;
using Godot;

namespace TowerDefense.UI;

// Külön Control, ami a skill fa node-jaival AZONOS szülő (a pan/zoom-olható
// canvas) gyereke — így ugyanazt a Scale/Position transzformot örökli, a
// vonalak a node-okkal együtt mozognak/zoomolnak, nem egy külön (statikus)
// rajzolási térben.
public partial class SkillTreeEdgeLayer : Control
{
    public List<(Vector2 From, Vector2 To, Color Color)> Edges { get; set; } = new();

    public override void _Draw()
    {
        foreach (var (from, to, color) in Edges)
        {
            var dir = (to - from).Normalized();
            // Vastagabb, kevésbé áttetsző vonal — a felhasználó jelezte, hogy
            // közelről (nagyobb zoomnál) alig látszott, pedig ez mutatja, melyik
            // node melyikből ered.
            DrawLine(from + dir * 4f, to - dir * 4f, color, 4f);
        }
    }
}
