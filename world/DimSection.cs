using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using static Godot.WebSocketPeer;
using static System.Collections.Specialized.BitVector32;

public partial class DimSection : SubViewport
{
    [Export] FastNoiseLite continents;
    [Export] FastNoiseLite peaks;
    [Export] FastNoiseLite errosion;

    public Dimension dimension;
    public Vector2I key { get; private set; }

    ConcurrentDictionary<Vector2I, Chunk> chunks = new();
    [Export] Node entityContainer;

    public World3D world3D { get; private set; }

    Object chunksUpdate = new Object();

    

    public void SetKey(Dimension dimension, Vector2I key)
    {
        this.dimension = dimension;
        this.key = key;
        this.OwnWorld3D = true;
        world3D = this.FindWorld3D();
    }

    public bool TryCreateChunk(Vector2I chunkPos, out Chunk result)
    {
        Stopwatch sp = new();
        sp.Start();

        Chunk chunk;
        
        lock (chunksUpdate)
        {
            if (chunks.TryGetValue(chunkPos, out result))
            {
                return false;
            }

            chunk = new Chunk(chunkPos, this);
            chunks.TryAdd(chunkPos, chunk);
        }

        for (int sectionID = 0; sectionID < GWS.WORLD_SECTIONS; sectionID++)
        {
            if (!chunk.TryCreateSection((byte)sectionID, out ChunkSection section))
            {
                continue;
            }

            Vector3I globalSectionPos = Vector3I.Up * section.pos * GWS.SECTION_HEIGHT + Vector3I.Right * chunk.pos.X * GWS.CHUNK_WIDTH + Vector3I.Back * chunk.pos.Y * GWS.CHUNK_WIDTH;

            for (ushort blockIndex = 0; blockIndex < GWS.MAX_BLOCKS_IN_SECTION; blockIndex++)
            {
                Vector3I globalBlockPos = World.GetBlockPos(blockIndex) + globalSectionPos;

                float continentsRatio = -continents.GetNoise2D(globalBlockPos.X, globalBlockPos.Z);
                float peaksRation = Mathf.Clamp(peaks.GetNoise2D(globalBlockPos.X, globalBlockPos.Z), 0, 1);
                float errosionRation = errosion.GetNoise2D(globalBlockPos.X, globalBlockPos.Z) * 0.5f;

                float heightRation = (continentsRatio + peaksRation + errosionRation + 1.5f) / 4;

                float height = heightRation * (GWS.WORLD_HEIGHT - 6) + 3;

                if (globalBlockPos.Y < height)
                {
                    section.SetBlockStateData(Blocks.Stone.defaultBlockState, blockIndex);
                }
            }
        }
        
        /*Parallel.For(0, GWS.WORLD_SECTIONS - 1, (sectionID, state) =>
        {
            if (!chunk.TryCreateSection((byte)sectionID, out ChunkSection section))
            {
                state.Break();
            }
            Vector3I globalSectionPos = Vector3I.Up * section.pos * GWS.SECTION_HEIGHT + Vector3I.Right * chunk.pos.X * GWS.CHUNK_WIDTH + Vector3I.Back * chunk.pos.Y * GWS.CHUNK_WIDTH;

            for (ushort blockIndex = 0; blockIndex < GWS.MAX_BLOCKS_IN_SECTION; blockIndex++)
            {
                Vector3I globalBlockPos = World.GetBlockPos(blockIndex) + globalSectionPos;

                float continentsRatio = -continents.GetNoise2D(globalBlockPos.X, globalBlockPos.Z);
                float peaksRation = Mathf.Clamp(peaks.GetNoise2D(globalBlockPos.X, globalBlockPos.Z), 0, 1);
                float errosionRation = errosion.GetNoise2D(globalBlockPos.X, globalBlockPos.Z) * 0.5f;

                float heightRation = (continentsRatio + peaksRation + errosionRation + 1.5f) / 4;

                float height = heightRation * (GWS.WORLD_HEIGHT - 6) + 3;

                if (globalBlockPos.Y < height)
                {
                    section.SetBlockStateData(Blocks.Stone.defaultBlockState, blockIndex);
                }
            }
        });*/

        result = chunk;
        sp.Stop();
        GD.Print($"Chunk {chunkPos} created in {sp.ElapsedMilliseconds} milliseconds");
        return true;
        
    }


    /*public void UpdateChunksAroundCenter(Vector2I center, bool increaseLoaders)
    {
        Thread newThread = new Thread(new ThreadStart(() => {
            if (increaseLoaders)
            {
                TryCreateChunk(center, out Chunk chunk); chunk.loadersCount += 1;
                for (int d = 1; d <= GWS.SIMULATING_DISTANCE; d++)
                {
                    TryCreateChunk(center + Vector2I.Up * d, out chunk); chunk.loadersCount += 1;
                    TryCreateChunk(center + Vector2I.Right * d, out chunk); chunk.loadersCount += 1;

                    TryCreateChunk(center + Vector2I.Down * d, out chunk); chunk.loadersCount += 1;
                    TryCreateChunk(center + Vector2I.Left * d, out chunk); chunk.loadersCount += 1;

                    for (int d2 = 1; d2 <= d; d2++)
                    {
                        if (d == d2)
                        {
                            continue;
                        }

                        TryCreateChunk(center + Vector2I.Up * d + Vector2I.Left * d2, out chunk); chunk.loadersCount += 1;
                        TryCreateChunk(center + Vector2I.Up * d + Vector2I.Right * d2, out chunk); chunk.loadersCount += 1;

                        TryCreateChunk(center + Vector2I.Right * d + Vector2I.Up * d2, out chunk); chunk.loadersCount += 1;
                        TryCreateChunk(center + Vector2I.Right * d + Vector2I.Down * d2, out chunk); chunk.loadersCount += 1;

                        TryCreateChunk(center + Vector2I.Down * d + Vector2I.Right * d2, out chunk); chunk.loadersCount += 1;
                        TryCreateChunk(center + Vector2I.Down * d + Vector2I.Left * d2, out chunk); chunk.loadersCount += 1;

                        TryCreateChunk(center + Vector2I.Left * d + Vector2I.Down * d2, out chunk); chunk.loadersCount += 1;
                        TryCreateChunk(center + Vector2I.Left * d + Vector2I.Up * d2, out chunk); chunk.loadersCount += 1;
                    }

                    TryCreateChunk(center + Vector2I.Up * d + Vector2I.Right * d, out chunk); chunk.loadersCount += 1;
                    TryCreateChunk(center + Vector2I.Right * d + Vector2I.Down * d, out chunk); chunk.loadersCount += 1;

                    TryCreateChunk(center + Vector2I.Down * d + Vector2I.Left * d, out chunk); chunk.loadersCount += 1;
                    TryCreateChunk(center + Vector2I.Left * d + Vector2I.Up * d, out chunk); chunk.loadersCount += 1;
                }
            }
            else
            {
                if (TryGetChunk(center, out Chunk chunk)) { chunk.loadersCount -= 1; }
                for (int d = 1; d <= GWS.SIMULATING_DISTANCE; d++)
                {
                    if (TryGetChunk(center + Vector2I.Up * d, out chunk)) { chunk.loadersCount -= 1; }
                    if (TryGetChunk(center + Vector2I.Right * d, out chunk)) { chunk.loadersCount -= 1; }

                    if (TryGetChunk(center + Vector2I.Down * d, out chunk)) { chunk.loadersCount -= 1; }
                    if (TryGetChunk(center + Vector2I.Left * d, out chunk)) { chunk.loadersCount -= 1; }

                    for (int d2 = 1; d2 <= d; d2++)
                    {
                        if (d == d2)
                        {
                            continue;
                        }

                        if (TryGetChunk(center + Vector2I.Up * d + Vector2I.Left * d2, out chunk)) { chunk.loadersCount -= 1; }
                        if (TryGetChunk(center + Vector2I.Up * d + Vector2I.Right * d2, out chunk)) { chunk.loadersCount -= 1; }

                        if (TryGetChunk(center + Vector2I.Right * d + Vector2I.Up * d2, out chunk)) { chunk.loadersCount -= 1; }
                        if (TryGetChunk(center + Vector2I.Right * d + Vector2I.Down * d2, out chunk)) { chunk.loadersCount -= 1; }

                        if (TryGetChunk(center + Vector2I.Down * d + Vector2I.Right * d2, out chunk)) { chunk.loadersCount -= 1; }
                        if (TryGetChunk(center + Vector2I.Down * d + Vector2I.Left * d2, out chunk)) { chunk.loadersCount -= 1; }

                        if (TryGetChunk(center + Vector2I.Left * d + Vector2I.Down * d2, out chunk)) { chunk.loadersCount -= 1; }
                        if (TryGetChunk(center + Vector2I.Left * d + Vector2I.Up * d2, out chunk)) { chunk.loadersCount -= 1; }
                    }

                    if (TryGetChunk(center + Vector2I.Up * d + Vector2I.Right * d, out chunk)) { chunk.loadersCount -= 1; }
                    if (TryGetChunk(center + Vector2I.Right * d + Vector2I.Down * d, out chunk)) { chunk.loadersCount -= 1; }

                    if (TryGetChunk(center + Vector2I.Down * d + Vector2I.Left * d, out chunk)) { chunk.loadersCount -= 1; }
                    if (TryGetChunk(center + Vector2I.Left * d + Vector2I.Up * d, out chunk)) { chunk.loadersCount -= 1; }
                }
            }

        }));
        newThread.Start();
    }*/

    /*public void GenerateChunk(Vector2I chunkPos)
    {
        Stopwatch sp = new();
        sp.Start();

        if (!TryCreateChunk(chunkPos, out Chunk chunk) && chunk.status == Chunk.Status.Ready)
        {
            return;
        }
        
        TryCreateChunk(chunkPos + Vector2I.Up, out _);
        TryCreateChunk(chunkPos + Vector2I.Down, out _);
        TryCreateChunk(chunkPos + Vector2I.Right, out _);
        TryCreateChunk(chunkPos + Vector2I.Left, out _);

        if (chunk.status == Chunk.Status.WaitCollision)
        {
            chunk.GenerateMeshes();
        }

        sp.Stop();
        GD.Print($"Chunk {chunkPos} generated in {sp.ElapsedMilliseconds} milliseconds");
        return;
    }*/

    public void AddEntity(Entity entity, Vector3 position)
    {
        entity.Position = position;
        entityContainer.AddChild(entity);
        //GD.Print("DimSection: ", this.World3D.Scenario, " Env: ", this.World3D.Environment);
        //GD.Print("Entity: ", entity.GetWorld3D());
    }

    public bool TryGetChunk(Vector2I chunkPos, out Chunk chunk)
    {
        lock (chunksUpdate)
        {
            return chunks.TryGetValue(chunkPos, out chunk);
        }
    }

    public void RemoveChunk(Chunk chunk)
    {
        lock (chunksUpdate)
        {
            chunk.ClearAllData();
            chunks.Remove(chunk.pos, out _);
        }
        //GD.Print($"Remove Chunk {chunk.pos}");
    }



}
