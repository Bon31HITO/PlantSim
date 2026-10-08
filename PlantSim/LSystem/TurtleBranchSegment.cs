namespace PlantSim.LSystem;

internal readonly record struct TurtleBranchSegment(
    VectorThreeDimensional StartPosition,
    VectorThreeDimensional EndPosition,
    TurtleOrientation StartOrientation,
    TurtleOrientation EndOrientation,
    double StartThickness,
    double EndThickness,
    int ColorIndex);