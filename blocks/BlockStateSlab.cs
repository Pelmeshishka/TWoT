using Godot;
using System;

public class BlockStateSlab : BlockState, IBlockState_Half
{
    public IBlockState_Half.Half half { get; set; }

    public BlockStateSlab(Block block, IBlockState_Half.Half half = IBlockState_Half.Half.bottom) : base(block)
    {
        this.half = half;
    }

    public IBlockState_Half.Half GetHalf()
    {
        return half;
    }

    public void SetHalf(IBlockState_Half.Half half)
    {
        this.half = half;
    }

    public override BlockState Clone()
    {
        return new BlockStateSlab(block, half);
    }

    public override string ToString()
    {
        return $"half={half}";
    }
}
