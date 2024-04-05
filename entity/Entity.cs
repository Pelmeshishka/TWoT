using Godot;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

public partial class Entity : CharacterBody3D
{
    public Guid guid { get; private set; }

    public bool isChunkLoader = true;
    public Vector2I lastChunk { get; private set; } = Vector2I.Zero;
    public int lastSection { get; private set; } = 0;

    public ConcurrentQueue<Vector2I> chunksLoadQueue = new();
    public ConcurrentQueue<Chunk> chunksUnloadQueue = new();

    public ConcurrentQueue<Chunk> affectedChunks = new();

    public Vector3 lookDirection => -head.GlobalTransform.Basis.Z;
    public Vector3I lookAtBlock;

    [Export] Node3D head;
    [Export] MeshInstance3D testMesh;

    public DimSection dimSection { get; private set; }
    public override void _Ready()
    {
        if (head is null)
        {
            head = this;
        }

        if (Guid.TryParse(this.Name, out Guid guid))
        {
            this.guid = guid;
        }

        if (TryGetDimSection(out DimSection dimSection))
        {
            this.dimSection = dimSection;
        }

        UpdateChunks(CurrentChunkPos());
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector3 tempVec = head.GlobalPosition + lookDirection * 10f;

        Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(head.GlobalPosition, tempVec));
        if (result.Count > 0)
        {
            tempVec = (Vector3)result["position"] - (Vector3)result["normal"] * 0.01f;
        }

        lookAtBlock = new Vector3I(Mathf.FloorToInt(tempVec.X), Mathf.FloorToInt(tempVec.Y), Mathf.FloorToInt(tempVec.Z));

        testMesh.GlobalPosition = lookAtBlock;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (dimSection is null)
        {
            return;
        }

        lastSection = CurrentSectionPos();
        Vector2I currentChunk = CurrentChunkPos();
        if (currentChunk != lastChunk)
        {
            UpdateChunks(currentChunk);
            lastChunk = currentChunk;
        }
    }

    private void UpdateChunks(Vector2I center)
    {
        /*ConcurrentQueue<Chunk> chunksToUnload = affectedChunks;
        while (affectedChunks.TryDequeue(out Chunk chunk))
        {
            chunksToUnload.Enqueue(chunk);
            if (Mathf.Abs(chunk.pos.X - center.X) > GWS.SIMULATING_DISTANCE || Mathf.Abs(chunk.pos.Y - center.Y) > GWS.SIMULATING_DISTANCE)
            {

            }
        }*/
        


        chunksLoadQueue.Clear();
        chunksLoadQueue.Enqueue(center);
        for (int d = 1; d <= GWS.SIMULATING_DISTANCE; d++)
        {
            chunksLoadQueue.Enqueue(center + Vector2I.Up * d);
            chunksLoadQueue.Enqueue(center + Vector2I.Right * d);

            chunksLoadQueue.Enqueue(center + Vector2I.Down * d);
            chunksLoadQueue.Enqueue(center + Vector2I.Left * d);

            for (int d2 = 1; d2 <= d; d2++)
            {
                if (d == d2)
                {
                    continue;
                }

                chunksLoadQueue.Enqueue(center + Vector2I.Up * d + Vector2I.Left * d2);
                chunksLoadQueue.Enqueue(center + Vector2I.Up * d + Vector2I.Right * d2);

                chunksLoadQueue.Enqueue(center + Vector2I.Right * d + Vector2I.Up * d2);
                chunksLoadQueue.Enqueue(center + Vector2I.Right * d + Vector2I.Down * d2);

                chunksLoadQueue.Enqueue(center + Vector2I.Down * d + Vector2I.Right * d2);
                chunksLoadQueue.Enqueue(center + Vector2I.Down * d + Vector2I.Left * d2);

                chunksLoadQueue.Enqueue(center + Vector2I.Left * d + Vector2I.Down * d2);
                chunksLoadQueue.Enqueue(center + Vector2I.Left * d + Vector2I.Up * d2);
            }

            chunksLoadQueue.Enqueue(center + Vector2I.Up * d + Vector2I.Right * d);
            chunksLoadQueue.Enqueue(center + Vector2I.Right * d + Vector2I.Down * d);

            chunksLoadQueue.Enqueue(center + Vector2I.Down * d + Vector2I.Left * d);
            chunksLoadQueue.Enqueue(center + Vector2I.Left * d + Vector2I.Up * d);
        }

        World.AddChunkLoader(this);

    }

    public Vector2I CurrentChunkPos()
    {
        return new Vector2I(Mathf.FloorToInt(this.Position.X / GWS.CHUNK_WIDTH), Mathf.FloorToInt(this.Position.Z / GWS.CHUNK_WIDTH));
    }

    public int CurrentSectionPos()
    {
        return Mathf.FloorToInt(this.Position.Y / GWS.SECTION_HEIGHT);
    }


    public bool TryGetDimSection(out DimSection dimSection)
    {
        if (!this.IsInsideTree())
        {
            dimSection = null;
            return false;
        }

        dimSection = GetParent().GetParent().GetParent<DimSection>();
        return true;
    }

    public bool TryGetDimension(out Dimension dimension)
    {
        if (!this.IsInsideTree())
        {
            dimension = null;
            return false;
        }

        dimension = GetParent().GetParent().GetParent<DimSection>().GetParent().GetParent<Dimension>();
        return true;
    }

}

