using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Godot.WebSocketPeer;

public partial class World : Node
{
    Dictionary<string, Dimension> dimensions = new();
    Dictionary<Guid, Entity> entitys = new();

    [Export] Node dimensionContainer;
    public static PackedScene dimensionPacked = ResourceLoader.Load<PackedScene>("res://world/Dimension.tscn");
    public static PackedScene dimSectionPacked = ResourceLoader.Load<PackedScene>("res://world/DimSection.tscn");

    ConcurrentDictionary<Entity, bool> chunkLoaders = new ();

    private static World world;
    Thread loadersThread;
    public override void _Ready()
    {
        world = this;

        CreateDimension("twot:terra");
        
        if (TryGetDimension("twot:terra", out Dimension dimension) && dimension.TryGetDimSection(Vector2I.Zero, out DimSection dimSection))
        {
            dimSection.AddEntity(CreateEntity("res://entity/ServerCamera.tscn"), new Vector3(0, 300, 0));
        }

        
    }
 
    private void CreateDimension(string dimKey)
    {
        GD.Print($"Create Dimension {dimKey}");
        if (dimensions.ContainsKey(dimKey)) 
        {
            GD.PushError($"Dimension {dimKey} already exists");
            return;
        }

        Dimension dimension = dimensionPacked.Instantiate<Dimension>();
        dimension.SetKey(dimKey);

        dimensions.Add(dimKey, dimension);
        dimensionContainer.CallDeferred("add_child", dimension);
    }

    private Entity CreateEntity(string path)
    {
        Guid guid = Guid.NewGuid();
        GD.Print($"Create Entity from path {path} with guid {guid}");
        Entity entity = ResourceLoader.Load<PackedScene>(path).Instantiate<Entity>();
        entity.Name = guid.ToString();
        entitys.Add(guid, entity);
        return entity;
    }

    private bool TryGetDimension(string dimKey, out Dimension dimension)
    {
        return dimensions.TryGetValue(dimKey, out dimension);
    }


    public static Vector3I GetBlockPos(int i)
    {
        return new Vector3I(
            i / GWS.CHUNK_WIDTH % GWS.SECTION_HEIGHT,
            i / (GWS.CHUNK_WIDTH * GWS.SECTION_HEIGHT) % GWS.CHUNK_WIDTH,
            i % GWS.CHUNK_WIDTH
        );
    }
    public static int GetBlockIndex(Vector3I blockPos)
    {
        return blockPos.Z + blockPos.X * GWS.CHUNK_WIDTH + blockPos.Y * GWS.CHUNK_WIDTH * GWS.SECTION_HEIGHT;
    }

    public static void AddChunkLoader(Entity entity)
    {
        world.chunkLoaders.TryAdd(entity, false);
        if (world.loadersThread is null)
        {
            world.loadersThread = new Thread(() => world.ChunksProcess());
            world.loadersThread.Start();
        }
    }
    private void ChunksProcess()
    {
        Parallel.ForEach(chunkLoaders, (kv, state) => {

            bool remove = true;
            if (kv.Key.dimSection is not null && kv.Key.chunksLoadQueue.TryDequeue(out Vector2I chunkPos))
            {
                kv.Key.dimSection.TryCreateChunk(chunkPos, out Chunk chunk);
                chunk.loadersCount += 1;
                kv.Key.affectedChunks.Enqueue(chunk);
                remove = false;
            }
            
            

            if (remove)
            {
                chunkLoaders.TryRemove(kv);
            }
        });

        if (chunkLoaders.Count > 0)
        {
            ChunksProcess();
        }
        else
        {
            loadersThread = null;
        }
    }

}
