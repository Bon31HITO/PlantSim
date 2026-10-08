using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal class BezierSurfacePatchTable
{
    private readonly Dictionary<int, BezierSurfacePatch> _patches = [];

    public BezierSurfacePatchTable()
    {
        RegisterPatch(0, BezierSurfacePatch.CreateLeafPatch());
        RegisterPatch(1, BezierSurfacePatch.CreatePetalPatch());
    }

    public void RegisterPatch(int identifier, BezierSurfacePatch patch)
    {
        ArgumentNullException.ThrowIfNull(patch);
        _patches[identifier] = patch;
    }

    public void RegisterFromText(int identifier, string definitionText)
    {
        BezierSurfacePatch patch = BezierSurfacePatch.Parse(definitionText);
        RegisterPatch(identifier, patch);
    }

    public void RegisterFromFile(int identifier, string filePath)
    {
        BezierSurfacePatch patch = BezierSurfacePatch.LoadFromFile(filePath);
        RegisterPatch(identifier, patch);
    }

    public BezierSurfacePatch GetPatch(int identifier)
    {
        if (_patches.TryGetValue(identifier, out BezierSurfacePatch? patch))
        {
            return patch;
        }

        if (_patches.TryGetValue(0, out BezierSurfacePatch? fallbackPatch))
        {
            return fallbackPatch;
        }

        return BezierSurfacePatch.CreateLeafPatch();
    }

    public bool ContainsPatch(int identifier)
    {
        return _patches.ContainsKey(identifier);
    }
}