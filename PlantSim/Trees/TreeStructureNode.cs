using PlantSim.LSystem;
using System.Collections.Generic;

namespace PlantSim.Trees;

public class TreeStructureNode
{
    public int Identifier { get; set; }
    public int BranchOrder { get; set; }
    public int AgeInSteps { get; set; }
    public double SegmentLength { get; set; }
    public double Radius { get; set; }
    public VectorThreeDimensional StartPosition { get; set; }
    public VectorThreeDimensional EndPosition { get; set; }
    public TurtleOrientation BaseOrientation { get; set; }
    public TurtleOrientation Orientation { get; set; }
    public TreeStructureNode? Parent { get; set; }
    public List<TreeStructureNode> Children { get; } = [];
    public bool IsTerminalBud { get; set; }
    public bool IsActiveApex { get; set; } = true;
    public bool IsPruned { get; set; }
    public double AccumulatedFlux { get; set; }
    public double BiomassWeight { get; set; }
    public double LocalLightIntensity { get; set; } = 1.0;
    public int ColorIndex { get; set; }

    public TreeStructureNode(
        int identifier,
        int branchOrder,
        double segmentLength,
        double radius,
        VectorThreeDimensional startPosition,
        TurtleOrientation baseOrientation,
        TreeStructureNode? parent = null)
    {
        Identifier = identifier;
        BranchOrder = branchOrder;
        SegmentLength = segmentLength;
        Radius = radius;
        StartPosition = startPosition;
        BaseOrientation = baseOrientation;
        Orientation = baseOrientation;
        Parent = parent;
        EndPosition = startPosition + (baseOrientation.HeadingDirection * segmentLength);
    }
}