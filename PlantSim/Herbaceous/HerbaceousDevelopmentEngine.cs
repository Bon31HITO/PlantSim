using PlantSim.LSystem;
using System;
using System.Collections.Generic;

namespace PlantSim.Herbaceous;

internal class HerbaceousDevelopmentEngine
{
    private int _nodeIdentifierCounter = 50000;

    public void SimulateGrowthStep(
        HerbaceousMetamerNode rootNode,
        HerbaceousParameters parameters,
        int currentSimulationStep)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(parameters);

        bool isFloweringTriggered = currentSimulationStep >= parameters.FloralInductionStep;
        if (isFloweringTriggered)
        {
            PropagateFloweringSignal(rootNode, parameters, currentSimulationStep);
        }

        UpdateMetamerKinetics(rootNode, parameters, isFloweringTriggered);
        DevelopMeristems(rootNode, parameters, currentSimulationStep);
        PropagateSpatialPositions(rootNode);
    }

    private void PropagateFloweringSignal(
        HerbaceousMetamerNode rootNode,
        HerbaceousParameters parameters,
        int currentSimulationStep)
    {
        int signalStepProgress = currentSimulationStep - parameters.FloralInductionStep;
        double signalReachableHeight = signalStepProgress * parameters.SignalPropagationVelocity;

        switch (parameters.FloweringSequence)
        {
            case FloweringSequenceKind.Simultaneous:
                SetSignalRecursive(rootNode, true);
                break;

            case FloweringSequenceKind.Acropetal:
                PropagateAcropetalSignalRecursive(rootNode, signalReachableHeight);
                break;

            case FloweringSequenceKind.Basipetal:
                int maximumNodeHeight = FindMaximumNodeIndexOnAxis(rootNode);
                double basipetalCutoffHeight = Math.Max(0.0, maximumNodeHeight - signalReachableHeight);
                PropagateBasipetalSignalRecursive(rootNode, basipetalCutoffHeight);
                break;
        }
    }

    private static void SetSignalRecursive(HerbaceousMetamerNode currentNode, bool hasSignal)
    {
        currentNode.HasSignalArrived = hasSignal;
        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            SetSignalRecursive(currentNode.Children[index], hasSignal);
        }
        for (int index = 0; index < currentNode.LateralBranches.Count; index++)
        {
            SetSignalRecursive(currentNode.LateralBranches[index], hasSignal);
        }
    }

    private static void PropagateAcropetalSignalRecursive(HerbaceousMetamerNode currentNode, double signalHeight)
    {
        if (currentNode.NodeIndexOnAxis <= signalHeight)
        {
            currentNode.HasSignalArrived = true;
        }
        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            PropagateAcropetalSignalRecursive(currentNode.Children[index], signalHeight);
        }
        for (int index = 0; index < currentNode.LateralBranches.Count; index++)
        {
            double lateralAdjustedSignal = signalHeight - currentNode.NodeIndexOnAxis;
            PropagateAcropetalSignalRecursive(currentNode.LateralBranches[index], lateralAdjustedSignal);
        }
    }

    private static void PropagateBasipetalSignalRecursive(HerbaceousMetamerNode currentNode, double cutoffHeight)
    {
        if (currentNode.NodeIndexOnAxis >= cutoffHeight)
        {
            currentNode.HasSignalArrived = true;
        }
        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            PropagateBasipetalSignalRecursive(currentNode.Children[index], cutoffHeight);
        }
        for (int index = 0; index < currentNode.LateralBranches.Count; index++)
        {
            PropagateBasipetalSignalRecursive(currentNode.LateralBranches[index], cutoffHeight);
        }
    }

    private static int FindMaximumNodeIndexOnAxis(HerbaceousMetamerNode currentNode)
    {
        int maximumIndex = currentNode.NodeIndexOnAxis;
        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            int childMaximum = FindMaximumNodeIndexOnAxis(currentNode.Children[index]);
            if (childMaximum > maximumIndex)
            {
                maximumIndex = childMaximum;
            }
        }
        return maximumIndex;
    }

    private void UpdateMetamerKinetics(
        HerbaceousMetamerNode currentNode,
        HerbaceousParameters parameters,
        bool isFloweringTriggered)
    {
        currentNode.AgeInSteps++;
        currentNode.StageAgeInSteps++;

        double dominanceMultiplier = CalculateDominanceMultiplier(currentNode, parameters);
        double elongationDelta = parameters.InternodeElongationRate * dominanceMultiplier;
        currentNode.InternodeLength = Math.Min(currentNode.TargetInternodeLength, currentNode.InternodeLength + elongationDelta);

        if (currentNode.Stage == HerbaceousOrganStage.VegetativeBud)
        {
            if (currentNode.HasSignalArrived)
            {
                currentNode.Stage = HerbaceousOrganStage.FloralBud;
                currentNode.StageAgeInSteps = 0;
            }
        }
        else if (currentNode.Stage == HerbaceousOrganStage.FloralBud)
        {
            if (currentNode.StageAgeInSteps >= parameters.FloralBudDuration)
            {
                currentNode.Stage = HerbaceousOrganStage.BloomingFlower;
                currentNode.StageAgeInSteps = 0;
            }
        }
        else if (currentNode.Stage == HerbaceousOrganStage.BloomingFlower)
        {
            if (currentNode.StageAgeInSteps >= parameters.BloomingDuration)
            {
                currentNode.Stage = HerbaceousOrganStage.FruitCapsule;
                currentNode.StageAgeInSteps = 0;
            }
        }
        else if (currentNode.Stage == HerbaceousOrganStage.FruitCapsule)
        {
            if (currentNode.StageAgeInSteps >= parameters.FruitDuration)
            {
                currentNode.Stage = HerbaceousOrganStage.Senescent;
                currentNode.StageAgeInSteps = 0;
            }
        }

        if (currentNode.NodeIndexOnAxis < parameters.VegetativeNodeCount)
        {
            currentNode.LeafScale = Math.Min(1.8, 0.4 + (currentNode.AgeInSteps * 0.15));
        }
        else
        {
            currentNode.LeafScale = Math.Min(0.9, 0.2 + (currentNode.AgeInSteps * 0.08));
        }

        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            UpdateMetamerKinetics(currentNode.Children[index], parameters, isFloweringTriggered);
        }
        for (int index = 0; index < currentNode.LateralBranches.Count; index++)
        {
            UpdateMetamerKinetics(currentNode.LateralBranches[index], parameters, isFloweringTriggered);
        }
    }

    private static double CalculateDominanceMultiplier(HerbaceousMetamerNode node, HerbaceousParameters parameters)
    {
        return parameters.BranchingDominance switch
        {
            BranchingDominanceKind.Acrotonic => 0.5 + (0.5 * Math.Min(1.0, node.NodeIndexOnAxis * 0.15)),
            BranchingDominanceKind.Basitonic => Math.Max(0.3, 1.2 - (node.NodeIndexOnAxis * 0.12)),
            BranchingDominanceKind.Mesotonic => Math.Max(0.4, 1.2 - Math.Abs((node.NodeIndexOnAxis - 5) * 0.18)),
            _ => 1.0
        };
    }

    private void DevelopMeristems(
        HerbaceousMetamerNode currentNode,
        HerbaceousParameters parameters,
        int currentSimulationStep)
    {
        if (currentNode.IsActiveApex && !currentNode.IsTerminated)
        {
            bool isPlastochronTriggered = (currentNode.AgeInSteps % Math.Max(1, parameters.PlastochronInterval)) == 0;
            if (isPlastochronTriggered)
            {
                ExecuteApicalDevelopment(currentNode, parameters, currentSimulationStep);
            }
        }

        if (currentNode.BranchOrder < parameters.MaximumBranchingOrder &&
            currentNode.AgeInSteps >= parameters.SecondaryBranchDelay &&
            currentNode.LateralBranches.Count == 0)
        {
            InitiateLateralBranching(currentNode, parameters);
        }

        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            DevelopMeristems(currentNode.Children[index], parameters, currentSimulationStep);
        }
        for (int index = 0; index < currentNode.LateralBranches.Count; index++)
        {
            DevelopMeristems(currentNode.LateralBranches[index], parameters, currentSimulationStep);
        }
    }

    private void ExecuteApicalDevelopment(
        HerbaceousMetamerNode apexNode,
        HerbaceousParameters parameters,
        int currentSimulationStep)
    {
        if (parameters.BranchingType == InflorescenceBranchingType.SympodialDichasium)
        {
            apexNode.IsTerminated = true;
            apexNode.IsActiveApex = false;
            apexNode.Stage = apexNode.HasSignalArrived ? HerbaceousOrganStage.BloomingFlower : HerbaceousOrganStage.FloralBud;

            double branchingAngleRadians = parameters.BranchingAngleInDegrees * (Math.PI / 180.0);
            TurtleOrientation leftDaughterOrientation = apexNode.Orientation.Pitch(branchingAngleRadians);
            TurtleOrientation rightDaughterOrientation = apexNode.Orientation.Pitch(-branchingAngleRadians);

            double targetLength = Math.Max(1.5, apexNode.TargetInternodeLength * parameters.LateralBranchLengthRatio);
            double reducedRadius = Math.Max(0.06, apexNode.Radius * 0.85);

            HerbaceousMetamerNode leftChild = new HerbaceousMetamerNode(
                ++_nodeIdentifierCounter,
                apexNode.BranchOrder + 1,
                apexNode.NodeIndexOnAxis + 1,
                targetLength,
                reducedRadius,
                apexNode.EndPosition,
                leftDaughterOrientation,
                apexNode);

            HerbaceousMetamerNode rightChild = new HerbaceousMetamerNode(
                ++_nodeIdentifierCounter,
                apexNode.BranchOrder + 1,
                apexNode.NodeIndexOnAxis + 1,
                targetLength,
                reducedRadius,
                apexNode.EndPosition,
                rightDaughterOrientation,
                apexNode);

            apexNode.LateralBranches.Add(leftChild);
            apexNode.LateralBranches.Add(rightChild);
            return;
        }

        if (parameters.BranchingType == InflorescenceBranchingType.SympodialMonochasium)
        {
            apexNode.IsTerminated = true;
            apexNode.IsActiveApex = false;
            apexNode.Stage = HerbaceousOrganStage.FloralBud;

            double branchingAngleRadians = parameters.BranchingAngleInDegrees * (Math.PI / 180.0);
            double alternatingAngle = (apexNode.NodeIndexOnAxis % 2 == 0) ? branchingAngleRadians : -branchingAngleRadians;
            TurtleOrientation daughterOrientation = apexNode.Orientation.Pitch(alternatingAngle);

            double targetLength = Math.Max(1.4, apexNode.TargetInternodeLength * parameters.LateralBranchLengthRatio);
            HerbaceousMetamerNode monochasialChild = new HerbaceousMetamerNode(
                ++_nodeIdentifierCounter,
                apexNode.BranchOrder + 1,
                apexNode.NodeIndexOnAxis + 1,
                targetLength,
                Math.Max(0.06, apexNode.Radius * 0.88),
                apexNode.EndPosition,
                daughterOrientation,
                apexNode);

            apexNode.LateralBranches.Add(monochasialChild);
            return;
        }

        // Monopodial Inflorescence (Capsella, Dibotryoid, etc.)
        double divergenceRadians = parameters.DivergenceAngleInDegrees * (Math.PI / 180.0);
        TurtleOrientation nextOrientation = apexNode.Orientation.Roll(divergenceRadians);
        double targetInternodeLength = Math.Max(1.2, parameters.MaximumInternodeLength * Math.Pow(0.95, apexNode.NodeIndexOnAxis));

        HerbaceousMetamerNode successorNode = new HerbaceousMetamerNode(
            ++_nodeIdentifierCounter,
            apexNode.BranchOrder,
            apexNode.NodeIndexOnAxis + 1,
            targetInternodeLength,
            Math.Max(0.05, apexNode.Radius * 0.94),
            apexNode.EndPosition,
            nextOrientation,
            apexNode);

        apexNode.IsActiveApex = false;
        apexNode.Children.Add(successorNode);
    }

    private void InitiateLateralBranching(
        HerbaceousMetamerNode parentNode,
        HerbaceousParameters parameters)
    {
        double lateralAngleRadians = parameters.BranchingAngleInDegrees * (Math.PI / 180.0);
        double divergenceAngleRadians = (parentNode.NodeIndexOnAxis * parameters.DivergenceAngleInDegrees) * (Math.PI / 180.0);

        TurtleOrientation lateralOrientation = parentNode.Orientation
            .Roll(divergenceAngleRadians)
            .Pitch(lateralAngleRadians);

        double targetLength = Math.Max(1.8, parentNode.TargetInternodeLength * parameters.LateralBranchLengthRatio);
        double lateralRadius = Math.Max(0.05, parentNode.Radius * 0.8);

        HerbaceousMetamerNode lateralChild = new HerbaceousMetamerNode(
            ++_nodeIdentifierCounter,
            parentNode.BranchOrder + 1,
            1,
            targetLength,
            lateralRadius,
            parentNode.EndPosition,
            lateralOrientation,
            parentNode);

        parentNode.LateralBranches.Add(lateralChild);
    }

    private void PropagateSpatialPositions(HerbaceousMetamerNode currentNode)
    {
        if (currentNode.Parent != null)
        {
            currentNode.StartPosition = currentNode.Parent.EndPosition;
        }
        currentNode.EndPosition = currentNode.StartPosition + (currentNode.Orientation.HeadingDirection * currentNode.InternodeLength);

        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            PropagateSpatialPositions(currentNode.Children[index]);
        }
        for (int index = 0; index < currentNode.LateralBranches.Count; index++)
        {
            PropagateSpatialPositions(currentNode.LateralBranches[index]);
        }
    }
}