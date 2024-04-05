using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using static Godot.Image;
using gc = Godot.Collections;

public partial class ModelToJson : Node3D
{
    [Export] MeshInstance3D fromInstance;
    [Export] CollisionShape3D fromColl;

    [Export] MeshInstance3D toInstance;
    [Export] CollisionShape3D toColl;

    [Export] Label fromLabel;

    [Export] Container texturesContainer;

    [Export] FileDialog openDialog;
    [Export] FileDialog saveDialog;

    [Export] CheckBox uvLockEntry;
    [Export] SpinBox rotXEntry;
    [Export] SpinBox rotYEntry;

    PackedScene texturePathPacked = ResourceLoader.Load<PackedScene>("res://model_to_json/TexturePath.tscn");

    string parent = "";

    Dictionary<string, string> textures = new();
    Dictionary<string, TexturePath> texturePaths = new();
    List<BlockModel.Face> faces = new();

    ShaderMaterial blocksMaterial = ResourceLoader.Load<ShaderMaterial>("res://resources/BlocksMaterial.material");

    public override void _Ready()
    {
        ClearData();
    }

    private void OpenPressed()
    {
        openDialog.Visible = true;
    }

    private void SavePressed()
    {
        saveDialog.Visible = true;
    }

    private void ClearData()
    {
        parent = "";
        fromInstance.Mesh = null;
        fromColl.Shape = null;
        toInstance.Mesh = null;
        toColl.Shape = null;
        fromLabel.Text = "";
        foreach (var node in texturesContainer.GetChildren())
        {
            texturesContainer.RemoveChild(node);
            node.QueueFree();
        }
        textures.Clear();
        texturePaths.Clear();
        faces.Clear();
    }

    private void OpenModel(string filePath)
    {
        ClearData();

        GD.Print($"Open model: {filePath}");
        if (filePath.EndsWith(".glb"))
        {
            Node node = ResourceLoader.Load<PackedScene>(filePath).Instantiate<Node>().GetChildren()[0];
            if (!(node is MeshInstance3D meshInstance))
            {
                OS.Alert("Invalid model data! Node in scene should containt MeshInstance3D as first child");
                return;
            }

            Mesh mesh = meshInstance.Mesh;
            fromInstance.Mesh = mesh;
            fromColl.Shape = mesh.CreateTrimeshShape();

            for (int surfaceId = 0; surfaceId < mesh.GetSurfaceCount(); surfaceId++)
            {
                string texturePath = mesh.SurfaceGetMaterial(surfaceId).ResourceName;
                TexturePath textureNode = texturePathPacked.Instantiate<TexturePath>();
                textureNode.SetData(false, texturePath);
                textureNode.Name = texturePath;
                texturePaths.Add(texturePath, textureNode);
                texturesContainer.AddChild(textureNode);

                gc.Array data = mesh.SurfaceGetArrays(surfaceId);
                faces.AddRange(MeshUtils.GetFaces((Vector3[])data[0], (Vector2[])data[4], (int[])data[12], texturePath));

            }
            ApplyTextures();
        }
        else if (filePath.EndsWith(".json"))
        {
            filePath = filePath.Replace("res://", "");
            using (FileStream file = File.OpenRead(filePath))
            {
                using (var stream = new StreamReader(file))
                {
                    parent = filePath;
                    BlockModel model = JsonSerializer.Deserialize<BlockModel>(stream.ReadToEnd(), BlockModels.jsonOptions);
                    faces = model.faces;

                    foreach (var t in model.texturePaths)
                    {
                        TexturePath textureNode = texturePathPacked.Instantiate<TexturePath>();
                        textureNode.SetData(true, t.Key, t.Value);
                        textureNode.Name = t.Key;
                        texturePaths.Add(t.Key, textureNode);
                        texturesContainer.AddChild(textureNode);
                    }

                    ApplyTextures();
                }
            }
        }
    }



    private void ApplyTextures()
    {
        Texture2DArray textureArray = new();
        List<string> keys = new();
        gc.Array<Image> images = new();
        foreach (var path in texturePaths)
        {
            string texKey = path.Value.Key;
            string filePath = "res://resources/error.png";
            if (texKey != "error")
            {
                string[] ok = texKey.ToLower().Split(":");
                if (ok.Length != 2)
                {
                    OS.Alert($"Invalid texture key: {texKey}! Expected <origin:key>");
                    texKey = "error";
                } 
                else
                {
                    filePath = $"res://assets/{ok[0]}/textures/blocks/{ok[1]}.png";
                }
            }
            
            if (!Godot.FileAccess.FileExists(filePath))
            {
                OS.Alert($"Texture: {texKey} doesnt exist in path: {filePath}");
                continue;
            }

            if (!keys.Contains(texKey))
            {
                keys.Add(texKey);
                Image image = ResourceLoader.Load<Texture2D>(filePath).GetImage();
                if(image.IsCompressed())
                {
                    image.Decompress();
                }
                image.Convert(Image.Format.Rgba8);
                image.ClearMipmaps();
                image.GenerateMipmaps();
                images.Add(image);
            } 

            path.Value.arrayId = keys.IndexOf(texKey);
        }

        textureArray.CreateFromImages(images);
        blocksMaterial.SetShaderParameter("textures", textureArray);

        textures.Clear();
        foreach (var t in texturePaths)
        {
            textures.Add(t.Key, t.Value.Key);
        }
        GenerateToMesh();
    }

    private void GreedyMeshing()
    {
        faces = MeshUtils.GreedyMeshing(faces, textures);
        GenerateToMesh();
    }

    private void UVLockChanged(bool val)
    {
        GenerateToMesh();
    }
    private void OnRotChanged(float val)
    {
        GenerateToMesh();
    }

    private void GenerateToMesh()
    {
        toInstance.Mesh = null;
        toColl.Shape = null;

        SurfaceTool st = new();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetMaterial(blocksMaterial);

        BlockModel convertedModel = MeshUtils.ConvertModel(uvLockEntry.ButtonPressed, (float)rotYEntry.Value, (float)rotXEntry.Value, new BlockModel(parent, textures, faces), Vector3.Zero);
        Dictionary<string, float> actualTextures = new();
        foreach (var pair in texturePaths)
        {
            actualTextures.TryAdd(pair.Value.Key, pair.Value.arrayId);
        }


        foreach (BlockModel.Face face in convertedModel.faces)
        {
            Vector3[] normals = new Vector3[] { face.normal, face.normal, face.normal };
            Vector2 textureID = new Vector2(
                    (string.IsNullOrEmpty(face.texturePath) || !actualTextures.ContainsKey(face.texturePath)) ? 0 : actualTextures[face.texturePath],
                    string.IsNullOrEmpty(face.overlayPath) ? -1 : (!actualTextures.ContainsKey(face.overlayPath) ? 0 : actualTextures[face.overlayPath]));
            Vector2[] textures = new Vector2[] { textureID, textureID, textureID };

            Vector3 center = face.isQuadrat ? (face.vertices[0] + face.vertices[1]+ face.vertices[2] + face.vertices[3]) / 4 : (face.vertices[0] + face.vertices[1] + face.vertices[2]) / 3;
            st.AddTriangleFan(new Vector3[] { center, face.vertices[1], face.vertices[1] }, new Vector2[] { face.uvs[0], face.uvs[1], face.uvs[2] }, normals: normals, uv2S: textures);


            st.SetSmoothGroup(UInt32.MaxValue);
            st.AddTriangleFan(new Vector3[] { face.vertices[0], face.vertices[1], face.vertices[2] }, new Vector2[] { face.uvs[0], face.uvs[1], face.uvs[2] }, normals: normals, uv2S: textures);
            
            if (face.isQuadrat)
            {
                st.SetSmoothGroup(UInt32.MaxValue);
                st.AddTriangleFan(new Vector3[] { face.vertices[0], face.vertices[2], face.vertices[3] }, new Vector2[] { face.uvs[0], face.uvs[2], face.uvs[3] }, normals: normals, uv2S: textures);
            }
        }

        st.Index();
        st.OptimizeIndicesForCache();
        toInstance.Mesh = st.Commit();
        toColl.Shape = toInstance.Mesh.CreateTrimeshShape();
    }


    private void SaveJson(string filePath)
    {
        filePath = filePath.Replace("res://", "");
        GD.Print($"Save model to: {filePath}");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        using (FileStream file = File.Create(filePath))
        {
            using (var stream = new StreamWriter(file))
            {
                if (string.IsNullOrEmpty(parent))
                {
                    stream.Write(JsonSerializer.Serialize(new BlockModel(textures, faces), BlockModels.jsonOptions));
                }
                else
                {
                    stream.Write(JsonSerializer.Serialize(new BlockModel(parent, textures), BlockModels.jsonOptions));
                }

            }
        }
    }
}
