namespace PlantSim.LSystem;

public readonly record struct TurtleState(
    VectorThreeDimensional Position,
    TurtleOrientation Orientation,
    double StepLength,
    double LineThickness,
    double TropismSusceptibility,
    VectorThreeDimensional TropismVector,
    int ColorIndex);