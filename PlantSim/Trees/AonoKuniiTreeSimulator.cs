using PlantSim.LSystem;
using System;

namespace PlantSim.Trees;

public class AonoKuniiTreeSimulator
{
    public void ApplyEnvironmentalDeformations(TreeStructureNode rootNode, TreeBranchingParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(parameters);

        AccumulateBiomassRecursively(rootNode);
        PropagateDeformationsTopDown(rootNode, parameters);
    }

    private double AccumulateBiomassRecursively(TreeStructureNode currentNode)
    {
        if (currentNode.IsPruned)
        {
            currentNode.BiomassWeight = 0.0;
            return 0.0;
        }

        const double woodDensity = 0.8;
        double cylinderVolume = Math.PI * (currentNode.Radius * currentNode.Radius) * currentNode.SegmentLength;
        double segmentWeight = cylinderVolume * woodDensity;

        double childrenWeight = 0.0;
        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            childrenWeight += AccumulateBiomassRecursively(currentNode.Children[childIndex]);
        }

        currentNode.BiomassWeight = segmentWeight + childrenWeight;
        return currentNode.BiomassWeight;
    }

    private void PropagateDeformationsTopDown(TreeStructureNode currentNode, TreeBranchingParameters parameters)
    {
        if (currentNode.IsPruned)
        {
            return;
        }

        if (currentNode.Parent != null)
        {
            currentNode.StartPosition = currentNode.Parent.EndPosition;
        }

        // 変形前の幾何学的基準姿勢から静的な撓み量を計算（累積回転の防止）
        TurtleOrientation adjustedOrientation = currentNode.BaseOrientation;

        if (parameters.GravitropicSusceptibility > 0.0 && currentNode.BranchOrder > 0)
        {
            // 木質化（太い枝・古い枝ほど曲げ耐性が急激に高まる）
            double secondMomentOfArea = (Math.PI * Math.Pow(currentNode.Radius, 4.0)) / 4.0;
            double lignificationMultiplier = 1.0 + (currentNode.AgeInSteps * 0.8);
            double structuralRigidity = Math.Max(1e-4, secondMomentOfArea * 8000.0 * lignificationMultiplier);

            double bendingMoment = currentNode.BiomassWeight * parameters.GravitropicSusceptibility;
            // 過剰な垂れ下がりを防ぐため最大撓み角を物理的に制限
            double deflectionRatio = Math.Clamp(bendingMoment / structuralRigidity, 0.0, 0.12);

            adjustedOrientation = adjustedOrientation.ApplyTropism(
                parameters.GravitationalDirection,
                deflectionRatio,
                currentNode.SegmentLength);
        }

        if (parameters.PhototropicSusceptibility > 0.0)
        {
            double lightAttractionFactor = parameters.PhototropicSusceptibility / (1.0 + (currentNode.BranchOrder * 0.5));
            adjustedOrientation = adjustedOrientation.ApplyTropism(
                parameters.LightDirection,
                lightAttractionFactor,
                currentNode.SegmentLength);
        }

        currentNode.Orientation = adjustedOrientation;
        currentNode.EndPosition = currentNode.StartPosition + (currentNode.Orientation.HeadingDirection * currentNode.SegmentLength);

        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            PropagateDeformationsTopDown(currentNode.Children[childIndex], parameters);
        }
    }
}