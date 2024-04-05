using Godot;
using System;

public class AirBlock : Block
{
    public AirBlock(string key) : base(key, new Properties().NonSolid())
    {
    }
}
