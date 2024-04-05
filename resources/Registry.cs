using Godot;
using System;

public partial class Registry : Node
{
    public override void _Ready()
    {
        Blocks.Register();
        BlockTextures.Register();
    }
}