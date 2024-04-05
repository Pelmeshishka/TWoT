using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Security.AccessControl;

public partial class TestCompress : Node
{

    public override void _Ready()
    {
        Stopwatch st = new();
        st.Start();

        ushort[] originalData = new ushort[4096];
        for (ushort i = 0; i < originalData.Length; i++)
        {
            originalData[i] = (ushort)(i / 6);
        }

        byte[] compressedData = Compress(originalData);
        ushort[] decompressedData = Decompress(compressedData);


        st.Stop();
        GD.Print($"(){originalData.Length} => {compressedData.Length} => (){decompressedData.Length} milsec: {st.ElapsedMilliseconds}");
        
        for (int i = 0; i < decompressedData.Length; i++)
        {
            if (originalData[i] != decompressedData[i])
            {
                GD.PushError("Decompressed data is not same!");
                break;
            }
        }

    }

    public static byte[] Compress(ushort[] data)
    {
        byte[] result = new byte[0];
        using (var memoryStream = new MemoryStream())
        {
            using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
            {
                using (var binaryWriter = new BinaryWriter(deflateStream))
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        binaryWriter.Write(data[i]);
                    }
                }
            }
            result = memoryStream.ToArray();
        }
        return result;
    }

    public static ushort[] Decompress(byte[] compressedData)
    {
        ushort[] result = new ushort[0];
        using (var inputStream = new MemoryStream(compressedData))
        {
            using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
            {
                using (var binaryReader = new BinaryReader(deflateStream))
                {
                    result = new ushort[GWS.MAX_BLOCKS_IN_SECTION];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = binaryReader.ReadUInt16();
                    }
                }
            }
        }
        return result;
    }
}
