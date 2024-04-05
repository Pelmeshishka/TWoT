using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static BlockModel;

public class MeshUtils
{
    public static List<BlockModel.Face> GetFaces(Vector3[] vertices, Vector2[] uvs, int[] indices, string texturePath)
    {
        List<BlockModel.Face> triangles = new();
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3[] v = new Vector3[] { vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]] };
            Vector2[] u = new Vector2[] { uvs[indices[i]], uvs[indices[i + 1]], uvs[indices[i + 2]] };

            Vector3 n = (v[0] - v[1]).Cross(v[2] - v[1]).Normalized();
            n = new Vector3(MathF.Round(n.X * 100) / 100, MathF.Round(n.Y * 100) / 100, MathF.Round(n.Z * 100) / 100);

            triangles.Add(new BlockModel.Face(n, v, u, texturePath));
        }
        
       
        List<BlockModel.Face> result = new();
        triangles = triangles.OrderBy(t => Vector3.Zero.DistanceTo((t.vertices[0] + t.vertices[1] + t.vertices[2]) / 3)).ToList();

        while (triangles.Count > 0)
        {
            BlockModel.Face first = triangles[0];
            Sort(first);

            bool find = false; 
            for (int n = 1; n < triangles.Count; n++)
            {
                BlockModel.Face current = triangles[n];
                if (first.normal != current.normal)
                {
                    continue;
                }
                Sort(current);

                if (first.vertices[0].DistanceTo(current.vertices[0]) < 0.01f && first.vertices[1].DistanceTo(current.vertices[2]) < 0.01f)
                {
                    BlockModel.Face quadrat = new BlockModel.Face(
                        first.normal,
                        new Vector3[] { current.vertices[0], current.vertices[1], current.vertices[2], first.vertices[2] },
                        new Vector2[] { current.uvs[0], current.uvs[1], current.uvs[2], first.uvs[2] },
                        texturePath) ;
                    Sort(quadrat);
                    result.Add(quadrat);
                    triangles.Remove(current);
                    find = true;
                    break;
                }
                else if (first.vertices[0].DistanceTo(current.vertices[0]) < 0.01f && first.vertices[2].DistanceTo(current.vertices[1]) < 0.01f)
                {
                    BlockModel.Face quadrat = new BlockModel.Face(
                        first.normal,
                        new Vector3[] { first.vertices[0], first.vertices[1], first.vertices[2], current.vertices[2] },
                        new Vector2[] { first.uvs[0], first.uvs[1], first.uvs[2], current.uvs[2] },
                        texturePath);
                    Sort(quadrat);
                    result.Add(quadrat);
                    triangles.Remove(current);
                    find = true;
                    break;
                }
            }

            triangles.Remove(first);
            if (!find)
            {
                result.Add(first);
            }
        }

        return result;
    }

    public static void Sort(BlockModel.Face face)
    {
        Sort(face.normal, face.vertices, face.uvs);
    }
    public static void Sort(Vector3 normal, Vector3[] vertices, Vector2[] uvs)
    {
        Vector3 checkVector = GetRight(normal);
        Vector3 central;
        Vector3[] directions;
        float[] angles;

        switch (vertices.Length)
        {
            case 4:
                central = (vertices[0] + vertices[1] + vertices[2] + vertices[3]) / 4;
                directions = new Vector3[] { vertices[0] - central, vertices[1] - central, vertices[2] - central, vertices[3] - central };
                angles = new float[] { checkVector.SignedAngleTo(directions[0], -normal), checkVector.SignedAngleTo(directions[1], -normal), checkVector.SignedAngleTo(directions[2], -normal), checkVector.SignedAngleTo(directions[3], -normal) };
                SortArraysByAngles(vertices, uvs, angles);
                break;
            case 3:
                central = (vertices[0] + vertices[1] + vertices[2]) / 3;
                directions = new Vector3[] { vertices[0] - central, vertices[1] - central, vertices[2] - central };
                angles = new float[] { checkVector.SignedAngleTo(directions[0], -normal), checkVector.SignedAngleTo(directions[1], -normal), checkVector.SignedAngleTo(directions[2], -normal) };
                SortArraysByAngles(vertices, uvs, angles);
                break;
            default:
                throw new ArgumentException("Unsupported number of vertices.");
        }
    }

    private static void SortArraysByAngles(Vector3[] vertices, Vector2[] uvs, float[] angles)
    {
        for (int i = 0; i < angles.Length - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < angles.Length; j++)
            {
                if (angles[j] < angles[minIndex])
                {
                    minIndex = j;
                }
            }

            if (minIndex != i)
            {
                // Swap angles
                float tempAngle = angles[i];
                angles[i] = angles[minIndex];
                angles[minIndex] = tempAngle;

                // Swap vertices
                Vector3 tempVertex = vertices[i];
                vertices[i] = vertices[minIndex];
                vertices[minIndex] = tempVertex;

                // Swap uvs
                Vector2 tempUV = uvs[i];
                uvs[i] = uvs[minIndex];
                uvs[minIndex] = tempUV;
            }
        }
    }

    public static Vector3 GetRight(Vector3 normal)
    {
        if (Vector3.Down.AngleTo(normal) < 0.01f || Vector3.Up.AngleTo(normal) < 0.01f)
        {
            return Vector3.Right;
        } 
        else
        {
            return normal.Cross(Vector3.Down).Normalized();
        }
    }

    public static List<BlockModel.Face> GreedyMeshing(List<BlockModel.Face> faces, Dictionary<string, string> textures = null)
    {
        List<BlockModel.Face> result = faces.Where(f => !f.isQuadrat).ToList();
        
        faces = faces.Where(f => f.isQuadrat)
            .OrderBy(f => f.vertices[0].Y)
            .ThenBy(f => f.vertices[0].DistanceTo(Vector3.Zero))
            .ToList();

        while (faces.Count > 0)
        {
            BlockModel.Face first = faces[0];
            Vector2[] uvs = (Vector2[])first.uvs.Clone();

            if (string.IsNullOrEmpty(first.texturePath))
            {
                first.texturePath = "0";
            }

            for (int i = 1; i < faces.Count; i++)
            {
                BlockModel.Face current = faces[i];
                if (string.IsNullOrEmpty(current.texturePath))
                {
                    current.texturePath = "0";
                }

                if (first.normal != current.normal)
                {
                    continue;
                }

                if (textures is null)
                {
                    if (first.texturePath != current.texturePath || first.overlayPath != current.overlayPath)
                    {
                        continue;
                    }
                } 
                else
                {
                    if (textures.GetValueOrDefault(first.texturePath, "error") != textures.GetValueOrDefault(current.texturePath, "error"))
                    {
                        continue;
                    }

                    if ((!string.IsNullOrEmpty(first.overlayPath) && string.IsNullOrEmpty(current.overlayPath)) || (string.IsNullOrEmpty(first.overlayPath) && !string.IsNullOrEmpty(current.overlayPath)))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(first.overlayPath) && !string.IsNullOrEmpty(first.overlayPath) && textures.GetValueOrDefault(first.overlayPath, "error") != textures.GetValueOrDefault(current.overlayPath, "error"))
                    {
                        continue;
                    }
                }
                

                if (first.vertices[0].DistanceTo(current.vertices[1]) < 0.01f && first.vertices[3].DistanceTo(current.vertices[2]) < 0.01f && (first.vertices[0] - first.vertices[1]).AngleTo(current.vertices[0] - current.vertices[1]) < 0.01f && (first.vertices[2] - first.vertices[3]).AngleTo(current.vertices[2] - current.vertices[3]) < 0.01f)
                {
                    if (Math.Abs(first.uvs[0].X-current.uvs[1].X) < 0.01f && Math.Abs(first.uvs[3].X - current.uvs[2].X) < 0.01f ||
                        Math.Abs(first.uvs[0].X - current.uvs[0].X) < 0.01f && Math.Abs(first.uvs[3].X - current.uvs[3].X) < 0.01f)
                    {
                        first.vertices[0] = current.vertices[0];
                        first.vertices[3] = current.vertices[3];
                        first.uvs[0] = current.uvs[0];
                        first.uvs[3] = current.uvs[3];

                        uvs[0].X -= Math.Abs(current.uvs[0].X - current.uvs[1].X);
                        uvs[3].X -= Math.Abs(current.uvs[3].X - current.uvs[2].X);

                        faces.Remove(current);
                        i--;
                        continue;
                    } 

                }
                else if (current.vertices[0].DistanceTo(first.vertices[1]) < 0.01f && current.vertices[3].DistanceTo(first.vertices[2]) < 0.01f && (first.vertices[0] - first.vertices[1]).AngleTo(current.vertices[0] - current.vertices[1]) < 0.01f && (first.vertices[2] - first.vertices[3]).AngleTo(current.vertices[2] - current.vertices[3]) < 0.01f)
                {
                    if (Math.Abs(current.uvs[0].X - first.uvs[1].X) < 0.01f && Math.Abs(current.uvs[3].X - first.uvs[2].X) < 0.01f ||
                        Math.Abs(current.uvs[1].X - first.uvs[1].X) < 0.01f && Math.Abs(current.uvs[2].X - first.uvs[2].X) < 0.01f)
                    {
                        first.vertices[1] = current.vertices[1];
                        first.vertices[2] = current.vertices[2];
                        first.uvs[1] = current.uvs[1];
                        first.uvs[2] = current.uvs[2];

                        uvs[1].X += Math.Abs(current.uvs[0].X - current.uvs[1].X);
                        uvs[2].X += Math.Abs(current.uvs[3].X - current.uvs[2].X);

                        faces.Remove(current);
                        i--;
                        continue;
                    }
                }
                else if (first.vertices[2].DistanceTo(current.vertices[1]) < 0.01f && first.vertices[3].DistanceTo(current.vertices[0]) < 0.01f && (first.vertices[0] - first.vertices[3]).AngleTo(current.vertices[0] - current.vertices[3]) < 0.01f && (first.vertices[1] - first.vertices[2]).AngleTo(current.vertices[1] - current.vertices[2]) < 0.01f)
                {
                    if (Math.Abs(first.uvs[2].Y - current.uvs[1].Y) < 0.01f && Math.Abs(first.uvs[3].Y - current.uvs[0].Y) < 0.01f ||
                        Math.Abs(first.uvs[2].Y - current.uvs[2].Y) < 0.01f && Math.Abs(first.uvs[3].Y - current.uvs[3].Y) < 0.01f)
                    {
                        first.vertices[2] = current.vertices[2];
                        first.vertices[3] = current.vertices[3];
                        first.uvs[2] = current.uvs[2];
                        first.uvs[3] = current.uvs[3];

                        uvs[2].Y += Math.Abs(current.uvs[2].Y - current.uvs[1].Y);
                        uvs[3].Y += Math.Abs(current.uvs[3].Y - current.uvs[0].Y);

                        faces.Remove(current);
                        i--;
                        continue;
                    }
                }
                else if (current.vertices[2].DistanceTo(first.vertices[1]) < 0.01f && current.vertices[3].DistanceTo(first.vertices[0]) < 0.01f && (first.vertices[0] - first.vertices[3]).AngleTo(current.vertices[0] - current.vertices[3]) < 0.01f && (first.vertices[1] - first.vertices[2]).AngleTo(current.vertices[1] - current.vertices[2]) < 0.01f)
                {
                    if (Math.Abs(current.uvs[2].Y - first.uvs[1].Y) < 0.01f && Math.Abs(current.uvs[3].Y - first.uvs[0].Y) < 0.01f ||
                        Math.Abs(current.uvs[1].Y - first.uvs[1].Y) < 0.01f && Math.Abs(current.uvs[0].Y - first.uvs[0].Y) < 0.01f)
                    {
                        first.vertices[1] = current.vertices[1];
                        first.vertices[0] = current.vertices[0];
                        first.uvs[1] = current.uvs[1];
                        first.uvs[0] = current.uvs[0];

                        uvs[1].Y -= Math.Abs(current.uvs[2].Y - current.uvs[1].Y);
                        uvs[0].Y -= Math.Abs(current.uvs[3].Y - current.uvs[0].Y);

                        faces.Remove(current);
                        i--;
                        continue;
                    }
                }
            }
            faces.Remove(first);
            first.uvs = uvs;
            result.Add(first);
        }
        return result;
    }


    //This method create BlockModel without textures data,
    //But all faces texturesPaths is textureKeys
    public static BlockModel ConvertModel(bool uvLock, float rotY, float rotX, BlockModel originModel, Vector3 pos, Dictionary<string, string> additionalTextures = null)
    {
        Quaternion q = Quaternion.FromEuler(Vector3.Up * rotY + Vector3.Right * rotX);
        Vector3 vOffset = - q * (Vector3.One / 2) + (Vector3.One / 2) + pos;

        List<BlockModel.Face> newFaces = new(originModel.faces.Count);

        foreach (var face in originModel.faces)
        {
            Vector3 originRight = GetRight(face.normal);
            Vector3[] vertices = (Vector3[])face.vertices.Clone();

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = q * vertices[i] + vOffset;
            }

            Vector3 newNormal = q * face.normal;
            Vector2[] uvs = (Vector2[])face.uvs.Clone();

            if (uvLock)
            {
                Vector3 newRight = GetRight(newNormal);
                Vector3 rotatedRight = q * originRight;
                float angle = rotatedRight.SignedAngleTo(newRight, newNormal);

                for (int i = 0; i < uvs.Length; i++)
                {
                    uvs[i] = uvs[i].Rotated(angle);
                }
            }

            newFaces.Add(new BlockModel.Face(
                newNormal,
                vertices,
                uvs,
                !(additionalTextures is null) && additionalTextures.ContainsKey(face.texturePath) ?  additionalTextures[face.texturePath] : originModel.texturePaths[face.texturePath],
                string.IsNullOrEmpty(face.overlayPath) ? null : (!(additionalTextures is null) && additionalTextures.ContainsKey(face.overlayPath) ? additionalTextures[face.overlayPath] : originModel.texturePaths[face.overlayPath])));
        }

        return new BlockModel(originModel.parent, null, newFaces);
    }

}




