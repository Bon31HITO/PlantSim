using PlantSim.LSystem;
using System.Collections.Generic;

namespace PlantSim.Herbaceous;

public class HerbaceousMetamerNode
{
    public int Identifier { get; set; }

    public int BranchOrder { get; set; }

    public int NodeIndexOnAxis { get; set; }

    public int AgeInSteps { get; set; }

    public double InternodeLength { get; set; }

    public double TargetInternodeLength { get; set; }

    public double Radius { get; set; }

    public VectorThreeDimensional StartPosition { get; set; }

    public VectorThreeDimensional EndPosition { get; set; }

    public TurtleOrientation Orientation { get; set; }

    public HerbaceousOrganStage Stage { get; set; } = HerbaceousOrganStage.VegetativeBud;

    public int StageAgeInSteps { get; set; }

    public bool HasSignalArrived { get; set; }

    public bool IsTerminated { get; set; }

    public bool IsActiveApex { get; set; } = true;

    public HerbaceousMetamerNode? Parent { get; set; }

    public List<HerbaceousMetamerNode> Children { get; } = [];

    public List<HerbaceousMetamerNode> LateralBranches { get; } = [];

    public TurtleOrientation LeafOrientation { get; set; }

    public double LeafScale { get; set; }

    public HerbaceousMetamerNode(
        int identifier,
        int branchOrder,
        int nodeIndexOnAxis,
        double targetInternodeLength,
        double radius,
        VectorThreeDimensional startPosition,
        TurtleOrientation orientation,
        HerbaceousMetamerNode? parent = null)
    {
        Identifier = identifier;
        BranchOrder = branchOrder;
        NodeIndexOnAxis = nodeIndexOnAxis;
        TargetInternodeLength = targetInternodeLength;
        InternodeLength = 0.1;
        Radius = radius;
        StartPosition = startPosition;
        Orientation = orientation;
        LeafOrientation = orientation;
        Parent = parent;
        EndPosition = startPosition + (orientation.HeadingDirection * InternodeLength);
    }
}