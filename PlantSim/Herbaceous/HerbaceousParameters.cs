namespace PlantSim.Herbaceous;

public class HerbaceousParameters
{
    public double BranchingAngleInDegrees { get; set; } = 38.0;

    public double DivergenceAngleInDegrees { get; set; } = 137.5;

    public int PlastochronInterval { get; set; } = 2;

    public int FloralInductionStep { get; set; } = 8;

    public double SignalPropagationVelocity { get; set; } = 1.0;

    public double InternodeElongationRate { get; set; } = 0.45;

    public double MaximumInternodeLength { get; set; } = 5.5;

    public double StemBaseRadius { get; set; } = 0.20;

    public int FloralBudDuration { get; set; } = 3;

    public int BloomingDuration { get; set; } = 5;

    public int FruitDuration { get; set; } = 7;

    public int VegetativeNodeCount { get; set; } = 5;

    public int MaximumBranchingOrder { get; set; } = 3;

    public int SecondaryBranchDelay { get; set; } = 2;

    public double LateralBranchLengthRatio { get; set; } = 0.72;

    public InflorescenceBranchingType BranchingType { get; set; } = InflorescenceBranchingType.Monopodial;

    public FloweringSequenceKind FloweringSequence { get; set; } = FloweringSequenceKind.Acropetal;

    public BranchingDominanceKind BranchingDominance { get; set; } = BranchingDominanceKind.Acrotonic;
}