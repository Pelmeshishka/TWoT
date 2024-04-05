using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public class Chunk
{
    public DimSection dimSection { get; private set; }
    public Vector2I pos { get; private set; }
    ConcurrentDictionary<int, ChunkSection> sections = new();

    private int lc = 0;
    public int loadersCount { get { return lc; } set { lc = value; if (lc <= 0) { dimSection.RemoveChunk(this); } } }

    public Chunk(Vector2I pos, DimSection dimSection)
    {
        this.pos = pos;
        this.dimSection = dimSection;
    }

    public bool TryCreateSection(int sectionID, out ChunkSection section)
    {
        if (sections.ContainsKey(sectionID))
        {
            section = null;
            return false;
        }

        section = new ChunkSection(sectionID, this);
        sections.TryAdd(sectionID, section);
        return true;
    }

    public bool TryGetSection(int sectionID, out ChunkSection section) 
    {
        return sections.TryGetValue(sectionID, out section);
    }

    public void ClearAllData()
    {
        foreach (ChunkSection section in sections.Values)
        {
            section.ClearAllData();
        }
    }

}
