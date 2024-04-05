using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class BlockStates
{
    public static Dictionary<string, BlockStateModel> STATES = new Dictionary<string, BlockStateModel>();

    public static void Register(string blockKey)
    {
        if (STATES.ContainsKey(blockKey))
        {
            return;
        }

        GD.Print($" -Register BlockState for block {blockKey}");
        string[] ok = blockKey.Split(":");
        if (ok.Length != 2 )
        {
            throw new Exception($"Invalid block key {blockKey}! Expected <origin:key>");
        }

        string filePath = $"assets/{ok[0]}/blockstates/{ok[1]}.json";
        if (!Godot.FileAccess.FileExists(filePath))
        {
            throw new Exception($"File doesnt exists! Path: {filePath}");
        }

        using (FileStream file = File.OpenRead(filePath))
        {
            using (var stream = new StreamReader(file))
            {
                BlockStateModel blockState = JsonSerializer.Deserialize<BlockStateModel>(stream.ReadToEnd(), BlockModels.jsonOptions);
                STATES.TryAdd(blockKey, blockState);

                foreach(var v in blockState.variants)
                {
                    BlockModels.Register(v.Value.modelKey);
                }
            }
        }

    }

}

public class BlockStateModel
{
    public BlockStateModel() { }
    public BlockStateModel(Dictionary<string, BlockStateVariant> variants) { this.variants = variants; }
    public Dictionary<string, BlockStateVariant> variants { get; set; } = new() { { "default", new BlockStateVariant("error") } };
}

public class BlockStateVariant
{
    public BlockStateVariant() { }

    public BlockStateVariant(string modelKey, bool uvLock = false, float rotY = 0, float rotX = 0)
    {
        this.modelKey = modelKey;
        this.uvLock = uvLock;
        this.rotY = rotY;
        this.rotX = rotX;
    }

    public string modelKey { get; set; }
    public bool uvLock { get; set; } = false;
    public float rotY { get; set; } = 0;
    public float rotX { get; set; } = 0;
}

