using System;

namespace PlantSim.Trees;

public class LeonardoDaVinciRuleCalculator
{
    public void CalculateRadiiRecursively(
        TreeStructureNode rootNode,
        double pipeModelExponent,
        double terminalBranchRadius)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        double effectiveExponent = pipeModelExponent > 0.0 ? pipeModelExponent : 2.0;
        CalculateNodeRadiusBottomUp(rootNode, effectiveExponent, terminalBranchRadius);
    }

    private double CalculateNodeRadiusBottomUp(
        TreeStructureNode currentNode,
        double pipeModelExponent,
        double terminalBranchRadius)
    {
        if (currentNode.Children.Count == 0 || currentNode.IsTerminalBud)
        {
            currentNode.Radius = Math.Max(0.02, terminalBranchRadius);
            return Math.Pow(currentNode.Radius, pipeModelExponent);
        }

        double accumulatedSumOfPowers = 0.0;
        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            TreeStructureNode childNode = currentNode.Children[childIndex];
            if (!childNode.IsPruned)
            {
                accumulatedSumOfPowers += CalculateNodeRadiusBottomUp(childNode, pipeModelExponent, terminalBranchRadius);
            }
        }

        if (accumulatedSumOfPowers <= 0.0)
        {
            currentNode.Radius = Math.Max(0.02, terminalBranchRadius);
            return Math.Pow(currentNode.Radius, pipeModelExponent);
        }

        currentNode.Radius = Math.Pow(accumulatedSumOfPowers, 1.0 / pipeModelExponent);
        return accumulatedSumOfPowers;
    }
}