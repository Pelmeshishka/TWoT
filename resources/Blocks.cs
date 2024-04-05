using Godot;
using System;
using System.Collections.Generic;

public static class Blocks
{
    public static Dictionary<string, Block> BLOCKS = new Dictionary<string, Block>();

    public static AirBlock Air => (AirBlock)BLOCKS["twot:air"];
    public static Block Grass => BLOCKS["twot:grass"];
    public static Block GrassSlab => BLOCKS["twot:grass_slab"];
    public static Block Dirt => BLOCKS["twot:dirt"];
    public static Block Stone => BLOCKS["twot:stone"];
    public static Block Bedrock => BLOCKS["twot:bedrock"];



    public static void Register()
    {
        RegisterBlock(new AirBlock("twot:air"));
        RegisterBlock(new Block("twot:grass", new Block.Properties()));
        RegisterBlock(new BlockSlab("twot:grass_slab", new Block.Properties()));
        RegisterBlock(new Block("twot:dirt", new Block.Properties()));
        RegisterBlock(new Block("twot:stone", new Block.Properties()));
        RegisterBlock(new Block("twot:bedrock", new Block.Properties()));
    }

    private static void RegisterBlock(Block block)
    {
        try
        {
            GD.Print($"Register Block: {block.key}");
            BLOCKS.Add(block.key, block);
            BlockStates.Register(block.key);
            GD.Print("");
        } 
        catch (Exception ex)
        {
            GD.PrintErr($"Cannot register block: {block.key},\n{ex.Message}");
        }
        
    }
    
    
}
