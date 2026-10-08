using System;

namespace PlantSim.Trees;

public class TreeSimulationPresetItem
{
    public string Title { get; }
    public string Description { get; }
    public double MainBranchAngleInDegrees { get; }
    public double LateralBranchAngleInDegrees { get; }
    public double DivergenceAngleInDegrees { get; }
    public double MainBranchLengthRatio { get; }
    public double LateralBranchLengthRatio { get; }
    public double PipeModelExponent { get; }
    public double GravitropicSusceptibility { get; }
    public double LightAttenuationFactor { get; }
    public double ShadingSurvivalThreshold { get; }

    public TreeSimulationPresetItem(
        string title,
        string description,
        double mainBranchAngleInDegrees,
        double lateralBranchAngleInDegrees,
        double divergenceAngleInDegrees,
        double mainBranchLengthRatio,
        double lateralBranchLengthRatio,
        double pipeModelExponent,
        double gravitropicSusceptibility,
        double lightAttenuationFactor,
        double shadingSurvivalThreshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Title = title;
        Description = description;
        MainBranchAngleInDegrees = mainBranchAngleInDegrees;
        LateralBranchAngleInDegrees = lateralBranchAngleInDegrees;
        DivergenceAngleInDegrees = divergenceAngleInDegrees;
        MainBranchLengthRatio = mainBranchLengthRatio;
        LateralBranchLengthRatio = lateralBranchLengthRatio;
        PipeModelExponent = pipeModelExponent;
        GravitropicSusceptibility = gravitropicSusceptibility;
        LightAttenuationFactor = lightAttenuationFactor;
        ShadingSurvivalThreshold = shadingSurvivalThreshold;
    }

    public override string ToString() => Title;
}