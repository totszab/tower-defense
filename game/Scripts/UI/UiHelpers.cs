using Godot;
using TowerDefense.Data;

namespace TowerDefense.UI;

public static class UiHelpers
{
    // Codex kártyák és enemy breakdown sorok közös ikon-építője: kör alakú
    // ellenségnél a tint-elt sprite-ot, háromszögnél a kód-rajzolt
    // TriangleIcon-t adja vissza — a hívó nem kell tudja, melyik.
    public static Control MakeEnemyIcon(EnemyData data)
    {
        if (data.Shape == EnemyData.EnemyShape.Triangle)
        {
            return new TriangleIcon
            {
                TriangleColor = data.Tint,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
        }

        // Fontos a property-sorrend: ExpandMode-nak a Texture beállítása ELŐTT
        // kell állnia, különben a minimum-méret a natív textúraméret alapján
        // rögzül, és a Size beállítása arra clampelődik.
        return new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = data.Sprite,
            Modulate = data.Tint,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    public static Panel MakeCircle(Vector2 size, Color color)
    {
        var panel = new Panel
        {
            CustomMinimumSize = size,
            Size = size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var style = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = (int)(size.X / 2f),
            CornerRadiusTopRight = (int)(size.X / 2f),
            CornerRadiusBottomLeft = (int)(size.X / 2f),
            CornerRadiusBottomRight = (int)(size.X / 2f),
        };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    public static StyleBoxFlat MakeOpaquePanelStyle(Color bgColor, Color borderColor)
    {
        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
        };
    }
}
