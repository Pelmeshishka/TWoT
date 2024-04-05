using Godot;
using System;
using System.Collections.Generic;

public partial class Dimension : Node
{
    public string key { get; private set; }

    [Export] SubViewportContainer dimSectionContainer;
    Dictionary<Vector2I, DimSection> sections = new();

    public void SetKey(string key)
    {
        this.key = key;
        CreateSection(Vector2I.Zero);
    }

    private void CreateSection(Vector2I secKey)
    {
        GD.Print($" -Create DimensionSection {secKey}");
        if (sections.ContainsKey(secKey))
        {
            GD.PushError($"Dimensions {key} already has section {secKey}");
            return;
        }

        DimSection dimSection = World.dimSectionPacked.Instantiate<DimSection>();
        dimSection.SetKey(this, secKey);
        sections.Add(secKey, dimSection);
        dimSectionContainer.CallDeferred("add_child", dimSection);
    }

    public bool TryGetDimSection(Vector2I secKey, out DimSection dimSection)
    {
        return sections.TryGetValue(secKey, out dimSection);
    }

}
