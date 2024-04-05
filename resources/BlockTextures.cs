using Godot;
using gc = Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public static class BlockTextures
{
    public static Dictionary<string, int> IMAGES = new ();


    static Dictionary<string, Image> images = new() { { "error", ConvertImage(ResourceLoader.Load<Texture2D>("resources/error.png").GetImage()) } };
    public static Texture2DArray textureArray = new();
    public static ShaderMaterial blocksMaterial = ResourceLoader.Load<ShaderMaterial>("res://resources/BlocksMaterial.material");


    public static void Register(string textureKey)
    {
        if (images.ContainsKey(textureKey))
        {
            return;
        }

        GD.Print($"   -Register Texture {textureKey}");
        string[] ok = textureKey.Split(":");
        if (ok.Length != 2)
        {
            throw new Exception($"Invalid texture key {textureKey}! Expected <origin:key>");
        }

        string filePath = $"assets/{ok[0]}/textures/blocks/{ok[1]}.png";
        if (!Godot.FileAccess.FileExists(filePath))
        {
            throw new Exception($"File doesnt exists! Path: {filePath}");
        }

        images.TryAdd(textureKey, ConvertImage(ResourceLoader.Load<Texture2D>(filePath).GetImage()));
    }

    public static void Register()
    {
        gc.Array<Image> img = new();

        foreach (var i in images)
        {
            IMAGES.TryAdd(i.Key, img.Count);
            img.Add(i.Value);
        }

        images.Clear();
        textureArray.CreateFromImages(img);
        blocksMaterial.SetShaderParameter("textures", textureArray);
    }

    private static Image ConvertImage(Image image)
    {
        if (image.IsCompressed())
        {
            image.Decompress();
        }
        image.Convert(Image.Format.Rgba8);
        image.ClearMipmaps();
        image.GenerateMipmaps();
        return image;
    }
}
