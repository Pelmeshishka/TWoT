using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class BlockModels
{
    public static Dictionary<string, BlockModel> MODELS = new ();

    public static void Register(string modelKey)
    {
        if (MODELS.ContainsKey(modelKey))
        {
            return;
        }

        GD.Print($"  -Register Block model {modelKey}");
        string[] ok = modelKey.Split(":");
        if (ok.Length != 2)
        {
            throw new Exception($"Invalid model key {modelKey}! Expected <origin:key>");
        }

        string filePath = $"assets/{ok[0]}/models/blocks/{ok[1]}.json";
        if (!Godot.FileAccess.FileExists(filePath))
        {
            throw new Exception($"File doesnt exists! Path: {filePath}");
        }


        using (FileStream file = File.OpenRead(filePath))
        {
            using (var stream = new StreamReader(file))
            {
                BlockModel model = JsonSerializer.Deserialize<BlockModel>(stream.ReadToEnd(), BlockModels.jsonOptions);

                MODELS.TryAdd(modelKey, model);

                SurfaceTool st = new SurfaceTool();
                st.Begin(Mesh.PrimitiveType.Triangles);
                bool meshIsEmpty = true;
                foreach (BlockModel.Face face in model.faces)
                {
                    st.AddTriangleFan(new Vector3[] { face.vertices[0], face.vertices[1], face.vertices[2] });
                    if (face.isQuadrat)
                    {
                        st.AddTriangleFan(new Vector3[] { face.vertices[0], face.vertices[2], face.vertices[3] });
                    }
                    meshIsEmpty = false;
                }
                if (!meshIsEmpty)
                {
                    st.Index();
                    st.OptimizeIndicesForCache();
                    model.shape = st.Commit().CreateTrimeshShape();
                }

                foreach (var t in model.texturePaths)
                {
                    BlockTextures.Register(t.Value);
                }

                if (!string.IsNullOrEmpty(model.parent)) {
                    Register(model.parent);
                }
            }
        }
    }

    public static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new CustomVector3Converter(), new CustomVector2Converter() }
    };
    public class CustomVector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string[] data = reader.GetString().Split(";");
            return new Vector3(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]));
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            value.X = value.X >= 0 ? Math.Abs(value.X) : value.X;
            value.Y = value.Y >= 0 ? Math.Abs(value.Y) : value.Y;
            value.Z = value.Z >= 0 ? Math.Abs(value.Z) : value.Z;
            writer.WriteStringValue($"{value.X};{value.Y};{value.Z}");
        }
    }
    public class CustomVector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string[] data = reader.GetString().Split(";");
            return new Vector2(float.Parse(data[0]), float.Parse(data[1]));
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            value.X = value.X >= 0 ? Math.Abs(value.X) : value.X;
            value.Y = value.Y >= 0 ? Math.Abs(value.Y) : value.Y;
            writer.WriteStringValue($"{value.X};{value.Y}");
        }
    }
}

public class BlockModel
{
    public string? parent { get; set; }
    public Dictionary<string, string>? texturePaths { get; set; } = new();
    public List<Face>? faces { get; set; } = new();

    public Shape3D shape;

    public BlockModel() { }

    public BlockModel(string parent, Dictionary<string, string> texturePaths, List<Face> faces)
    {
        this.parent = parent;
        this.texturePaths = texturePaths;
        this.faces = faces;
    }

    public BlockModel(Dictionary<string, string> texturePaths, List<Face> faces)
    {
        this.texturePaths = texturePaths;
        this.faces = faces;
    }

    public BlockModel(string parent, Dictionary<string, string> texturePaths)
    {
        this.parent = parent;
        this.texturePaths = texturePaths;
    }

    public class Face
    {
        public Face() { }
        public Face(Vector3 normal, Vector3[] vertices, Vector2[] uvs, string texturePath, string overlayPath=null)
        {
            this.texturePath = texturePath;
            this.overlayPath = overlayPath;
            this.normal = normal;
            this.vertices = vertices;
            this.uvs = uvs;
        }

        [JsonIgnore]
        public bool isQuadrat => !(vertices is null) && vertices.Length == 4;

        public string? texturePath { get; set; }
        public string? overlayPath { get; set; }
        public Vector3 normal { get; set; }
        public Vector3[]? vertices { get; set; }
        public Vector2[]? uvs { get; set; }
    }
}

