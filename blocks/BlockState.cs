using Godot;
using System;
using System.Xml.Linq;

public class BlockState
{
    public Block block;

    public BlockState(Block block)
    {
        this.block = block;
    }

    public virtual BlockState Clone()
    {
        return new BlockState(block);
    }

    public override string ToString()
    {
        return "default";
    }

}

public interface IBlockState_Half
{
    public enum Half
    {
        bottom = 0, top = 1
    }
    public Half GetHalf();
    public void SetHalf(Half half);
}

