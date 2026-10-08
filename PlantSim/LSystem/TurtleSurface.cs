namespace PlantSim.LSystem;

internal readonly record struct TurtleSurface(
    int SurfaceKind,
    VectorThreeDimensional Position,
    TurtleOrientation Orientation,
    double Scale,
    int ColorIndex,
    BezierSurfacePatch? CustomPatch = null);