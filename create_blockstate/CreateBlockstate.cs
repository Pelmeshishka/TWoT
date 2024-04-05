using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public partial class CreateBlockstate : Node3D
{
    [Export] MeshInstance3D toInstance;
    [Export] CollisionShape3D toColl;

    [Export] LineEdit blockEntry;
    [Export] Container variantsContainer;

    [Export] OptionButton halfOption;

    string blockKey = "";
    BlockState blockState;

    Dictionary<string, BlockStateVariant> variants = new();
    Dictionary<string, VariantPath> variantsPaths = new();


    private PackedScene variantPathPacked = ResourceLoader.Load<PackedScene>("res://create_blockstate/VariantPath.tscn");


    public override void _Ready()
    {
        ushort[] data = new ushort[16 * 16 * 16];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (ushort)((13 << 12) | (10 << 8) | (2 << 4) | 5);
        }

        Image image = Image.Create(data.Length, 1, false, Image.Format.Rgb8);
        for (int i = 0; i < data.Length; i++)
        {
            Color color = Color.Color8(
                (byte)(((data[i] >> 12) & 0xF) * 17),
                (byte)(((data[i] >> 8) & 0xF) * 17),
                (byte)(((data[i] >> 4) & 0xF) * 17),
                (byte)((data[i] & 0xF) * 17));

            image.SetPixel(i, 0, color);
        }

        ImageTexture texture = ImageTexture.CreateFromImage(image);
        ResourceSaver.Save(texture, "resources/TestLightMap.png");


        foreach (IBlockState_Half.Half s in Enum.GetValues(typeof(IBlockState_Half.Half)))
        {
            halfOption.AddItem(s.ToString());
        }
    }

    private void ClearData()
    {
        blockKey = "";
        blockState = null;
        variants.Clear();
        variantsPaths.Clear();
        foreach (var n in variantsContainer.GetChildren())
        {
            variantsContainer.RemoveChild(n);
            if (n is VariantPath path)
            {
                path.OnRemoveVariant -= RemoveVariant;
                path.OnApplyChanges -= VariantChanged;
            }
            n.QueueFree();
        }
    }

    private void LoadBlock()
    {
        ClearData();

        if (!Blocks.BLOCKS.TryGetValue(blockEntry.Text, out Block block)) {
            OS.Alert($"Block {blockEntry.Text} doesnt exists!");
            return;
        }
        blockKey = blockEntry.Text;
        blockState = block.defaultBlockState.Clone();
        GenerateNewVariant("default");
    }

    private void GenerateNewVariant()
    {
        GenerateNewVariant(null);
    }
    private void GenerateNewVariant(string variantKey)
    {
        if (blockState is null)
        {
            OS.Alert("Choose block!");
            return;
        }

        if (string.IsNullOrEmpty(variantKey))
        {
            variantKey = blockState.ToString();
        }

        
        if (variants.ContainsKey(variantKey))
        {
            OS.Alert($"Variant \"{variantKey}\" already exists!");
            return;
        }

        VariantPath path = variantPathPacked.Instantiate<VariantPath>();
        path.SetKey(variantKey);
        variantsPaths.Add(variantKey, path);
        variants.Add(variantKey, new BlockStateVariant(path.ModelKey));
        variantsContainer.AddChild(path);
        path.OnRemoveVariant += RemoveVariant;
        path.OnApplyChanges += VariantChanged;
        RegenerateMesh();
    }

    private void RemoveVariant(string key)
    {
        if (key == "default")
        {
            OS.Alert("Cannot remove default variant!");
            return;
        }

        VariantPath path = variantsPaths[key];
        variants.Remove(key);
        variantsPaths.Remove(key);
        variantsContainer.RemoveChild(path);
        path.OnRemoveVariant -= RemoveVariant;
        path.OnApplyChanges -= VariantChanged;
        path.QueueFree();
    }

    private void VariantChanged(string key)
    {
        VariantPath path = variantsPaths[key];
        BlockStateVariant variant = variants[key];
        variant.modelKey = path.ModelKey;
        variant.uvLock = path.UVLock;
        variant.rotY = path.RotY;
        variant.rotX = path.RotX;

        if (blockState.ToString() == key || (!variants.ContainsKey(blockState.ToString()) && key == "default"))
        {
            RegenerateMesh();
        }
    }


    private void SelectHalf(int id)
    {
        if (blockState is IBlockState_Half bHalf && Enum.TryParse(halfOption.GetItemText(id), out IBlockState_Half.Half half))
        {
            bHalf.SetHalf(half);
            RegenerateMesh();
        }
    }

    private void RegenerateMesh()
    {
        string variantKey = blockState.ToString();
        if (!variants.ContainsKey(variantKey))
        {
            variantKey = "default";
        }

        BlockStateVariant variant = variants[variantKey];

        string filePath = "resources/error.json";
        if (variant.modelKey != "error")
        {
            string[] ok_model = variant.modelKey.Split(":");
            if (ok_model.Length != 2)
            {
                OS.Alert($"Invalid model key \"{variant.modelKey}\"! Expected <origin:key>");
            } 
            else
            {
                filePath = $"assets/{ok_model[0]}/models/blocks/{ok_model[1]}.json";
            }
        }

        if (!Godot.FileAccess.FileExists(filePath))
        {
            OS.Alert($"File {filePath} doesnt exists!");
            return;
        }

        /*using (FileStream file = File.OpenRead(filePath))
        {
            using (var stream = new StreamReader(file))
            {
                BlockModel model = JsonSerializer.Deserialize<BlockModel>(stream.ReadToEnd(), Models.jsonOptions);
                MeshData meshData = MeshUtils.ConvertedMeshData(variant.uvLock, variant.rotY, variant.rotX, model.meshData);

                SurfaceTool st = new();
                st.Begin(Mesh.PrimitiveType.Triangles);

                foreach (var q in meshData.quadrats)
                {
                    Vector3 normal = JsonSerializer.Deserialize<Vector3>(q.Key, Models.jsonOptions);

                    foreach (var p in q.Value)
                    {
                        Vector2 textureID = new Vector2(0,-1);
                        Vector2[] texture = new Vector2[] { textureID, textureID, textureID };

                        Vector3 center = (p.vertices[0]+p.vertices[1]+p.vertices[2]+ p.vertices[3])/4;

                        st.AddTriangleFan(new Vector3[] { center, center+normal, center+normal }, new Vector2[] { p.uvs[0], p.uvs[1], p.uvs[2] }, uv2S: texture);


                        st.SetSmoothGroup(UInt32.MaxValue);
                        st.AddTriangleFan(new Vector3[] { p.vertices[0], p.vertices[1], p.vertices[2] }, new Vector2[] { p.uvs[0], p.uvs[1], p.uvs[2] }, uv2S: texture);
                        st.SetSmoothGroup(UInt32.MaxValue);
                        st.AddTriangleFan(new Vector3[] { p.vertices[0], p.vertices[2], p.vertices[3] }, new Vector2[] { p.uvs[0], p.uvs[2], p.uvs[3] }, uv2S: texture);
                    }

                }

                foreach (var t in meshData.triangles)
                {
                    foreach (var p in t.Value)
                    {
                        Vector2 textureID = new Vector2(0, -1);
                        Vector2[] texture = new Vector2[] { textureID, textureID, textureID };

                        st.SetSmoothGroup(UInt32.MaxValue);
                        st.AddTriangleFan(p.vertices, p.uvs, uv2S: texture);
                    }
                }

                st.Index();
                st.OptimizeIndicesForCache();
                st.GenerateNormals();
                toInstance.Mesh = st.Commit();
                toColl.Shape = toInstance.Mesh.CreateTrimeshShape();

            }
        }*/
    }

    private void SaveJson()
    {
        if (blockState is null)
        {
            OS.Alert("Choose block!");
            return;
        }

        string[] ok = blockKey.Split(":");
        if (ok.Length != 2)
        {
            OS.Alert($"Invalid block key {blockKey}! Expected <origin:key>");
            return;
        }
        
        string filePath = $"assets/{ok[0]}/blockstates/{ok[1]}.json";
        GD.Print($"Save blockstate to: {filePath}");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        using (FileStream file = File.Create(filePath))
        {
            using (var stream = new StreamWriter(file))
            {
                stream.Write(JsonSerializer.Serialize(new BlockStateModel(variants), BlockModels.jsonOptions));
            }
        }
    }
}
