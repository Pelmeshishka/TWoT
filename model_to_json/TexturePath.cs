using Godot;
using System;

public partial class TexturePath : HBoxContainer
{
    public bool ByParent { get; private set; }
    [Export] private Label originLabel;
    [Export] private Label pathLabel;
    [Export] private LineEdit textureLabel;
    public string Key => textureLabel.Text;

    public float arrayId = 0;

    public void SetData(bool byParent, string path, string key = null)
    {
        if (string.IsNullOrEmpty(key))
        {
            key = $"twot:{path}";
        }
        ByParent = byParent;
        originLabel.Text = ByParent ? "Parent| " : "Self| ";
        pathLabel.Text = path;
        textureLabel.Text = key;
    }
}
