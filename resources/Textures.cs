using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public static class Textures
{
    public static Dictionary<string, Vector2I> TEXTURES = new ();
     

    const int blockMaxSize = 32;
    const int maxBlocksCount = (int)Image.MaxWidth / blockMaxSize;
    const int maxTextureSize = maxBlocksCount * blockMaxSize;

    static Dictionary<string, Image> images = new();

    public static string RegistryTexture(string key)
    {
        if (key == "error")
        {
            return key;
        }

        if (images.TryGetValue(key, out Image image))
        {
            return image is null ? "error" : key;
        }

        GD.Print($"Register Texture {key}");
        string[] origin_name = key.Split(":");

        if (origin_name.Length < 2)
        {
            GD.Print($"Cannot register Texture {key}");
            images.Add(key, null);
            return "error";
        }

        string filePath = $"assets\\{origin_name[0]}\\textures\\blocks\\{origin_name[1]}.png";
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), filePath)))
        {
            GD.Print($"Cannot register Texture {key}");
            images.Add(key, null);
            return "error";
        }

        image = ResourceLoader.Load<Texture2D>(filePath).GetImage();
        images.Add(key, image);
        return key;
    }

    public static void Register()
    {

    }

}
