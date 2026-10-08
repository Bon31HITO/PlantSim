using PlantSim.LSystem;

namespace PlantSim.Trees;

public class TreeBranchingParameters
{
    public double MainBranchLengthRatio { get; set; } = 0.9;
    public double LateralBranchLengthRatio { get; set; } = 0.6;
    public double MainBranchAngleInDegrees { get; set; } = 25.0;
    public double LateralBranchAngleInDegrees { get; set; } = 35.0;
    public double DivergenceAngleInDegrees { get; set; } = 137.5;
    public double InitialBranchLength { get; set; } = 10.0;
    public double TerminalBranchRadius { get; set; } = 0.2;
    public double PipeModelExponent { get; set; } = 2.0;
    public double GravitropicSusceptibility { get; set; } = 0.05;
    public double PhototropicSusceptibility { get; set; } = 0.02;
    public VectorThreeDimensional LightDirection { get; set; } = new(0.0, 1.0, 0.0);
    public VectorThreeDimensional GravitationalDirection { get; set; } = new(0.0, -1.0, 0.0);
    public double InfluxCapacity { get; set; } = 100.0;
    public double MinimumFluxThreshold { get; set; } = 2.0;
    public double ApicalDominanceFactor { get; set; } = 0.7;
    public int MaximumBranchingOrder { get; set; } = 6;
}