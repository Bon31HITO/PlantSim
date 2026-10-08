using PlantSim.LSystem;
using System;
using System.Collections.Generic;

namespace PlantSim.Trees;

internal class BorchertSladePhysiologicalEngine
{
    private readonly CanopyLightVoxelGrid _voxelGrid = new();
    private int _nodeIdentifierCounter = 10000;

    public void SimulateGrowthStep(
        TreeStructureNode rootNode,
        TreeBranchingParameters parameters,
        double lightAttenuationFactor = 0.35,
        double shadingSurvivalThreshold = 0.25)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(parameters);

        EvaluateCanopyLightEnvironment(rootNode, lightAttenuationFactor);

        // 光合成生産フラックスと既存木質バイオマスの維持コストを計算
        int activeLeafCount = CountActiveTerminalBuds(rootNode);
        double totalWoodBiomass = CalculateTotalWoodBiomassRecursive(rootNode);
        double maintenanceRespirationCost = totalWoodBiomass * 0.08;

        double grossPhotosyntheticInflux = parameters.InfluxCapacity + (activeLeafCount * 3.0);
        double netAvailableFlux = Math.Max(parameters.MinimumFluxThreshold * 2.0, grossPhotosyntheticInflux - maintenanceRespirationCost);

        DistributeFluxAcropetally(rootNode, netAvailableFlux, parameters);
        DevelopAndPruneBranches(rootNode, parameters, shadingSurvivalThreshold);

        LeonardoDaVinciRuleCalculator radiusCalculator = new LeonardoDaVinciRuleCalculator();
        radiusCalculator.CalculateRadiiRecursively(rootNode, parameters.PipeModelExponent, parameters.TerminalBranchRadius);
    }

    private static double CalculateTotalWoodBiomassRecursive(TreeStructureNode currentNode)
    {
        if (currentNode.IsPruned)
        {
            return 0.0;
        }

        double segmentBiomass = Math.PI * (currentNode.Radius * currentNode.Radius) * currentNode.SegmentLength;
        double accumulatedChildrenBiomass = 0.0;
        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            accumulatedChildrenBiomass += CalculateTotalWoodBiomassRecursive(currentNode.Children[childIndex]);
        }

        return segmentBiomass + accumulatedChildrenBiomass;
    }

    private static int CountActiveTerminalBuds(TreeStructureNode currentNode)
    {
        if (currentNode.IsPruned)
        {
            return 0;
        }

        if (currentNode.Children.Count == 0 || currentNode.IsTerminalBud)
        {
            return 1;
        }

        int totalCount = 0;
        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            totalCount += CountActiveTerminalBuds(currentNode.Children[childIndex]);
        }

        return totalCount;
    }

    private void EvaluateCanopyLightEnvironment(TreeStructureNode rootNode, double lightAttenuationFactor)
    {
        VectorThreeDimensional minimumBound = new(-50.0, 0.0, -50.0);
        VectorThreeDimensional maximumBound = new(50.0, 100.0, 50.0);
        _voxelGrid.ResetBounds(minimumBound, maximumBound);

        AccumulateFoliageToGridRecursive(rootNode);
        _voxelGrid.PropagateLightAttenuation(lightAttenuationFactor);
        AssignLightIntensityRecursive(rootNode);
    }

    private void AccumulateFoliageToGridRecursive(TreeStructureNode currentNode)
    {
        if (currentNode.IsPruned)
        {
            return;
        }

        if (currentNode.Children.Count == 0 || currentNode.IsTerminalBud)
        {
            _voxelGrid.RegisterFoliageDensity(currentNode.EndPosition, 1.0);
        }

        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            AccumulateFoliageToGridRecursive(currentNode.Children[childIndex]);
        }
    }

    private void AssignLightIntensityRecursive(TreeStructureNode currentNode)
    {
        if (currentNode.IsPruned)
        {
            return;
        }

        currentNode.LocalLightIntensity = _voxelGrid.SampleLightIntensity(currentNode.EndPosition);

        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            AssignLightIntensityRecursive(currentNode.Children[childIndex]);
        }
    }

    private void DistributeFluxAcropetally(
        TreeStructureNode currentNode,
        double incomingFlux,
        TreeBranchingParameters parameters)
    {
        if (currentNode.IsPruned)
        {
            currentNode.AccumulatedFlux = 0.0;
            return;
        }

        currentNode.AccumulatedFlux = incomingFlux * Math.Max(0.3, currentNode.LocalLightIntensity);
        currentNode.AgeInSteps++;

        List<TreeStructureNode> activeChildren = [];
        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            TreeStructureNode childNode = currentNode.Children[childIndex];
            if (!childNode.IsPruned)
            {
                activeChildren.Add(childNode);
            }
        }

        if (activeChildren.Count == 0)
        {
            return;
        }

        double totalWeight = 0.0;
        double[] weights = new double[activeChildren.Count];
        for (int index = 0; index < activeChildren.Count; index++)
        {
            TreeStructureNode childNode = activeChildren[index];
            double orderBonus = childNode.BranchOrder == 0 ? 1.0 : (parameters.ApicalDominanceFactor * 1.1);
            double lightBonus = 0.6 + (0.4 * childNode.LocalLightIntensity);
            double sectionalAreaWeight = Math.Pow(childNode.Radius, 1.6);
            weights[index] = sectionalAreaWeight * orderBonus * lightBonus;
            totalWeight += weights[index];
        }

        if (totalWeight <= 1e-7)
        {
            double splitFlux = incomingFlux / activeChildren.Count;
            for (int index = 0; index < activeChildren.Count; index++)
            {
                DistributeFluxAcropetally(activeChildren[index], splitFlux, parameters);
            }
            return;
        }

        for (int index = 0; index < activeChildren.Count; index++)
        {
            double childFlux = incomingFlux * (weights[index] / totalWeight);
            DistributeFluxAcropetally(activeChildren[index], childFlux, parameters);
        }
    }

    private void DevelopAndPruneBranches(
        TreeStructureNode currentNode,
        TreeBranchingParameters parameters,
        double shadingSurvivalThreshold)
    {
        if (currentNode.IsPruned)
        {
            return;
        }

        // 光合成不足かつ養分枯渇した末端枝の剪定
        if (currentNode.Children.Count == 0 && currentNode.BranchOrder > 0 && currentNode.AgeInSteps > 2)
        {
            if (currentNode.LocalLightIntensity < shadingSurvivalThreshold && currentNode.AccumulatedFlux < parameters.MinimumFluxThreshold)
            {
                currentNode.IsPruned = true;
                currentNode.IsActiveApex = false;
                return;
            }
        }

        if (currentNode.Children.Count == 0 && currentNode.IsActiveApex)
        {
            // 伸長可能条件：十分な余剰フラックスがあり、かつ分枝次数および節間長が限界に達していないこと
            if (currentNode.AccumulatedFlux >= parameters.MinimumFluxThreshold &&
                currentNode.BranchOrder < parameters.MaximumBranchingOrder &&
                currentNode.SegmentLength > 0.45)
            {
                CreateSuccessorBranches(currentNode, parameters);
            }
            else
            {
                // 限界に達した先端芽は休眠（有限成長の終了）
                currentNode.IsActiveApex = false;
                currentNode.IsTerminalBud = true;
            }
        }
        else
        {
            for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
            {
                DevelopAndPruneBranches(currentNode.Children[childIndex], parameters, shadingSurvivalThreshold);
            }
        }
    }

    private void CreateSuccessorBranches(TreeStructureNode apexNode, TreeBranchingParameters parameters)
    {
        apexNode.IsTerminalBud = false;
        apexNode.IsActiveApex = false;

        double divergenceAngleRadians = parameters.DivergenceAngleInDegrees * (Math.PI / 180.0);
        double mainPitchAngleRadians = parameters.MainBranchAngleInDegrees * (Math.PI / 180.0);
        double lateralPitchAngleRadians = parameters.LateralBranchAngleInDegrees * (Math.PI / 180.0);

        if (apexNode.BranchOrder == 0)
        {
            // 主幹（Trunk / Order 0）：直立螺旋伸長
            double mainLength = Math.Max(2.5, apexNode.SegmentLength * parameters.MainBranchLengthRatio);
            TurtleOrientation trunkOrientation = apexNode.BaseOrientation.Roll(divergenceAngleRadians);

            TreeStructureNode trunkSuccessor = new TreeStructureNode(
                ++_nodeIdentifierCounter,
                0,
                mainLength,
                parameters.TerminalBranchRadius,
                apexNode.EndPosition,
                trunkOrientation,
                apexNode);
            apexNode.Children.Add(trunkSuccessor);

            if (apexNode.AccumulatedFlux > parameters.MinimumFluxThreshold * 1.1)
            {
                double lateralLength = Math.Max(2.2, apexNode.SegmentLength * parameters.LateralBranchLengthRatio);
                TurtleOrientation primaryBranchOrientation = trunkOrientation.Pitch(lateralPitchAngleRadians);

                TreeStructureNode primaryBranchSuccessor = new TreeStructureNode(
                    ++_nodeIdentifierCounter,
                    1,
                    lateralLength,
                    parameters.TerminalBranchRadius,
                    apexNode.EndPosition,
                    primaryBranchOrientation,
                    apexNode);
                apexNode.Children.Add(primaryBranchSuccessor);
            }
        }
        else
        {
            // 側枝系（Order >= 1）：二叉分岐による自己相似的縮小展開
            double mainLength = apexNode.SegmentLength * parameters.MainBranchLengthRatio;
            double lateralLength = apexNode.SegmentLength * parameters.LateralBranchLengthRatio;

            TurtleOrientation branchingFrame = apexNode.BaseOrientation.Roll(divergenceAngleRadians);

            // 娘枝1（主軸方向継続枝）
            TurtleOrientation mainDaughterOrientation = branchingFrame.Pitch(mainPitchAngleRadians);
            TreeStructureNode mainDaughter = new TreeStructureNode(
                ++_nodeIdentifierCounter,
                apexNode.BranchOrder,
                mainLength,
                parameters.TerminalBranchRadius,
                apexNode.EndPosition,
                mainDaughterOrientation,
                apexNode);
            apexNode.Children.Add(mainDaughter);

            // 娘枝2（側軸方向分岐枝）
            if (apexNode.AccumulatedFlux > parameters.MinimumFluxThreshold * 1.2 && apexNode.LocalLightIntensity > 0.2)
            {
                TurtleOrientation lateralDaughterOrientation = branchingFrame.Pitch(-lateralPitchAngleRadians);
                TreeStructureNode lateralDaughter = new TreeStructureNode(
                    ++_nodeIdentifierCounter,
                    apexNode.BranchOrder + 1,
                    lateralLength,
                    parameters.TerminalBranchRadius,
                    apexNode.EndPosition,
                    lateralDaughterOrientation,
                    apexNode);
                apexNode.Children.Add(lateralDaughter);
            }
        }
    }
}