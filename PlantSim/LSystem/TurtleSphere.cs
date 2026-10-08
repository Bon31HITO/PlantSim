namespace PlantSim.LSystem;

public readonly record struct TurtleSphere(
    VectorThreeDimensional CenterPosition,
    double Radius,
    int ColorIndex);