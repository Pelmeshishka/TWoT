using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

public class ChunkSection
{
    public Chunk chunk { get; private set; }
    public int pos { get; private set; }

    Dictionary<ushort, PalleteNode> blockStatesPalette = new();
    ushort[] blockStatesData = new ushort[GWS.MAX_BLOCKS_IN_SECTION];

    Rid colBody;
    Shape3D shape;

    Rid instance;
    Mesh mesh;

    Object dataUpdate = new Object();

    public ChunkSection(int pos, Chunk chunk) 
    {
        this.pos = pos;
        this.chunk = chunk;

        blockStatesPalette.Add(0, new PalleteNode(Blocks.Air.defaultBlockState, GWS.MAX_BLOCKS_IN_SECTION));

        Transform3D transform = new Transform3D(Basis.Identity, Vector3.Up * pos * GWS.SECTION_HEIGHT + Vector3.Right * chunk.pos.X * GWS.CHUNK_WIDTH + Vector3.Back * chunk.pos.Y * GWS.CHUNK_WIDTH);
        colBody = PhysicsServer3D.BodyCreate();
        PhysicsServer3D.BodySetMode(colBody, PhysicsServer3D.BodyMode.Static);
        PhysicsServer3D.BodySetState(colBody, PhysicsServer3D.BodyState.Transform, transform);
        PhysicsServer3D.BodySetSpace(colBody, chunk.dimSection.world3D.Space);
        PhysicsServer3D.BodySetCollisionLayer(colBody, 2);

        instance = RenderingServer.InstanceCreate();
        RenderingServer.InstanceSetScenario(instance, chunk.dimSection.world3D.Scenario);
        RenderingServer.InstanceSetTransform(instance, transform);

        mesh = new BoxMesh();
        RenderingServer.InstanceSetBase(instance, mesh.GetRid());
    }


    public void SetBlockStateData(BlockState blockState, int blockIndex)
    {
        lock (dataUpdate)
        {
            ushort foundKey = ushort.MaxValue;
            foreach (var v in blockStatesPalette)
            {
                if ((v.Value.blockState.block.key + v.Value.blockState.ToString()) == (blockState.block.key + blockState.ToString()))
                {
                    foundKey = v.Key;
                    break;
                }
            }

            if (foundKey == ushort.MaxValue)
            {
                foreach (ushort k in blockStatesPalette.Keys)
                {
                    if (k < ushort.MaxValue - 1 && !blockStatesPalette.ContainsKey((ushort)(k + 1)))
                    {
                        foundKey = (ushort)(k + 1);
                        break;
                    }

                    if (k > 0 && !blockStatesPalette.ContainsKey((ushort)(k - 1)))
                    {
                        foundKey = (ushort)(k - 1);
                        break;
                    }
                }

                blockStatesPalette.Add(foundKey, new PalleteNode(blockState, 0));
            }

            ushort prevKey = blockStatesData[blockIndex];
            if (prevKey == foundKey)
            {
                return;
            }

            PalleteNode prevNode = blockStatesPalette[prevKey];
            prevNode.usages -= 1;

            PalleteNode newNode = blockStatesPalette[foundKey];
            newNode.usages += 1;

            blockStatesData[blockIndex] = foundKey;

            if (prevNode.usages <= 0)
            {
                blockStatesPalette.Remove(prevKey);
            }
        }
    }

    public bool TryGenerateMeshAndShape()
    {
        lock (dataUpdate)
        {
            Dictionary<string, List<BlockModel.Face>> meshesData = new()
            {
                { "land", new() }
            };

            for (ushort blockIndex = 0; blockIndex < GWS.MAX_BLOCKS_IN_SECTION; blockIndex++)
            {
                Vector3I blockPos = World.GetBlockPos(blockIndex);

                if (GetBlockAt(blockPos + Vector3.Up).properties.isSolid &&
                    GetBlockAt(blockPos + Vector3.Down).properties.isSolid &&
                    GetBlockAt(blockPos + Vector3.Forward).properties.isSolid &&
                    GetBlockAt(blockPos + Vector3.Back).properties.isSolid &&
                    GetBlockAt(blockPos + Vector3.Right).properties.isSolid &&
                    GetBlockAt(blockPos + Vector3.Left).properties.isSolid)
                {
                    continue;
                }

                BlockState blockState = blockStatesPalette[blockStatesData[blockIndex]].blockState;

                if (!BlockStates.STATES.TryGetValue(blockState.block.key, out BlockStateModel stateModel))
                    continue;

                if (!stateModel.variants.TryGetValue(blockState.ToString(), out BlockStateVariant variant) &&
                    !stateModel.variants.TryGetValue("default", out variant))
                    continue;

                ProcessParentModels(in meshesData, variant.modelKey, variant, blockPos);
            }

            SurfaceTool collST = new();
            collST.Begin(Mesh.PrimitiveType.Triangles);
            bool meshIsEmpty = true;

            foreach (var pair in meshesData)
            {
                foreach (BlockModel.Face face in MeshUtils.GreedyMeshing(MeshUtils.GreedyMeshing(pair.Value)))
                {
                    collST.AddVertex(face.vertices[0]);
                    collST.AddVertex(face.vertices[1]);
                    collST.AddVertex(face.vertices[2]);
                    if (face.isQuadrat)
                    {
                        collST.AddVertex(face.vertices[0]);
                        collST.AddVertex(face.vertices[2]);
                        collST.AddVertex(face.vertices[3]);
                    }

                    meshIsEmpty = false;
                }
            }

            if (meshIsEmpty)
            {
                mesh = null;
                shape = null;
                PhysicsServer3D.BodyClearShapes(colBody);
            }
            else
            {
                collST.Index();
                mesh = collST.Commit();
                shape = mesh.CreateTrimeshShape();
                PhysicsServer3D.BodyAddShape(colBody, shape.GetRid());
                if (PhysicsServer3D.BodyGetShapeCount(colBody) > 1)
                {
                    PhysicsServer3D.BodyRemoveShape(colBody, 0);
                }
                RenderingServer.InstanceSetBase(instance, mesh.GetRid());
                RenderingServer.InstanceSetSurfaceOverrideMaterial(instance, 0, BlockTextures.blocksMaterial.GetRid());
            }
        }
        return true;
    }

    void ProcessParentModels(in Dictionary<string, List<BlockModel.Face>> meshesData, in string modelKey, in BlockStateVariant variant, in Vector3I blockPos, in Dictionary<string, string> additionalTextures = null)
    {
        if (BlockModels.MODELS.TryGetValue(modelKey, out BlockModel blockModel))
        {
            foreach (BlockModel.Face face in blockModel.faces)
            {
                Block sideBlock = GetBlockAt(blockPos + face.normal);

                if (!sideBlock.properties.isSolid)
                {
                    Vector3[] vertices = new Vector3[face.vertices.Length];
                    for(int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = face.vertices[i] + blockPos;
                    }

                    meshesData["land"].Add(new BlockModel.Face(face.normal, vertices, face.uvs, face.texturePath, face.overlayPath));
                }
            }

            ProcessParentModels(in meshesData, blockModel.parent, variant, blockPos, additionalTextures is null? blockModel.texturePaths : additionalTextures); 
        }
    }

    public Block GetBlockAt(Vector3 bp)
    {
        lock (dataUpdate)
        {
            Vector3I blockPos = (Vector3I)bp;

            int sectionOffset = 0;
            if (blockPos.Y < 0 || blockPos.Y >= GWS.SECTION_HEIGHT)
            {
                sectionOffset = Mathf.FloorToInt(blockPos.Y * 1.0f / GWS.SECTION_HEIGHT);
            }

            Vector2I chunkOffset = Vector2I.Zero;
            if (blockPos.X < 0 || blockPos.X >= GWS.CHUNK_WIDTH)
            {
                chunkOffset.X = Mathf.FloorToInt(blockPos.X * 1.0f / GWS.CHUNK_WIDTH);
            }
            if (blockPos.Z < 0 || blockPos.Z >= GWS.CHUNK_WIDTH)
            {
                chunkOffset.Y = Mathf.FloorToInt(blockPos.Z * 1.0f / GWS.CHUNK_WIDTH);
            }

            if (chunkOffset == Vector2I.Zero && sectionOffset == 0)
            {
                return blockStatesPalette[blockStatesData[World.GetBlockIndex(blockPos)]].blockState.block;
            }
            else
            {
                blockPos.X = blockPos.X - chunkOffset.X * GWS.CHUNK_WIDTH;
                blockPos.Y = blockPos.Y - sectionOffset * GWS.SECTION_HEIGHT;
                blockPos.Z = blockPos.Z - chunkOffset.Y * GWS.CHUNK_WIDTH;

                if (chunk.dimSection.TryGetChunk(chunk.pos + chunkOffset, out Chunk sideChunk) && sideChunk.TryGetSection(pos + sectionOffset, out ChunkSection sideSection))
                {
                    return sideSection.GetBlockAt(blockPos);
                }

            }
        }
        return Blocks.Air;
    }

    public void ClearAllData()
    {
        blockStatesPalette.Clear();
        blockStatesData = null;

        shape = null;
        PhysicsServer3D.FreeRid(colBody);

        mesh = null;
        RenderingServer.FreeRid(instance);
    }

    private class PalleteNode 
    {
        public BlockState blockState;
        public ushort usages;

        public PalleteNode(BlockState blockState, ushort usages)
        {
            this.blockState = blockState;
            this.usages = usages;
        }
    }

}
