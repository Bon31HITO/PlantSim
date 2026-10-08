using System.Collections.Generic;

namespace PlantSim.LSystem;

public readonly record struct TurtlePolygon(
    IReadOnlyList<VectorThreeDimensional> Vertices,
    int ColorIndex);