using Godot;

namespace TowerDefense.UI;

public static class UiHelpers
{
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
