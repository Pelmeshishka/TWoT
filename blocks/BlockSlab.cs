using Godot;
using System;

public class BlockSlab : Block
{
    public BlockSlab(string key, Properties properties) : base(key, properties.NonSolid())
    {
        this.defaultBlockState = new BlockStateSlab(this);
    }
}
