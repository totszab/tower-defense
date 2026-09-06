using Godot;

namespace TowerDefense.UI;

public partial class DamageNumber : Node2D
{
    private const float Duration = 0.6f;
    private const float RiseSpeed = 40f;

    private float _elapsed;
    private Label _label;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
    }

    public void SetValue(float amount)
    {
        _label.Text = $"-{amount:0}";
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        Position += new Vector2(0f, -RiseSpeed) * (float)delta;
        Modulate = new Color(1f, 1f, 1f, 1f - _elapsed / Duration);

        if (_elapsed >= Duration)
        {
            QueueFree();
        }
    }
}
