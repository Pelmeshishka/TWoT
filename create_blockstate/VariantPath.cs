using Godot;
using System;

public partial class VariantPath : VBoxContainer
{
    [Export] RichTextLabel variantKey;

    private string _key;
    public string Key => _key;

    [Export] LineEdit modelKey;
    public string ModelKey => modelKey.Text;
    [Export] CheckBox uvLock;
    public bool UVLock => uvLock.ButtonPressed;

    [Export] SpinBox rotY;
    public float RotY => (float)rotY.Value;

    [Export] SpinBox rotX;
    public float RotX => (float)rotX.Value;

    [Signal] public delegate void OnRemoveVariantEventHandler(string key);
    [Signal] public delegate void OnApplyChangesEventHandler(string key);


    public void SetKey(string key)
    {
        this._key = key;
        variantKey.Text = key;
        this.Name = key;
    }

    private void RemoveVariant()
    {
        EmitSignal(SignalName.OnRemoveVariant, Key);
    }

    private void ApplyChanges()
    {
        EmitSignal(SignalName.OnApplyChanges, Key);
    }

}
