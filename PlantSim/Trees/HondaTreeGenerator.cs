using PlantSim.LSystem;
using System;
using System.Collections.Generic;

namespace PlantSim.Trees;

public class HondaTreeGenerator
{
    private int _nodeIdentifierCounter;

    public TreeStructureNode GenerateTree(TreeBranchingParameters parameters, int recursionDepth)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _nodeIdentifierCounter = 0;

        TurtleOrientation initialOrientation = TurtleOrientation.CreateDefaultTwoDimensional();
        TreeStructureNode rootNode = new TreeStructureNode(
            ++_nodeIdentifierCounter,
            0,
            parameters.InitialBranchLength,
            parameters.TerminalBranchRadius,
            VectorThreeDimensional.Zero,
            initialOrientation);

        BuildHondaBranchRecursive(rootNode, parameters, parameters.InitialBranchLength, 0, recursionDepth);

        LeonardoDaVinciRuleCalculator radiusCalculator = new LeonardoDaVinciRuleCalculator();
        radiusCalculator.CalculateRadiiRecursively(rootNode, parameters.PipeModelExponent, parameters.TerminalBranchRadius);

        return rootNode;
    }

    public string BuildParametricLSystemGrammar(TreeBranchingParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        string mainAngleText = parameters.MainBranchAngleInDegrees.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        string lateralAngleText = parameters.LateralBranchAngleInDegrees.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        string divergenceAngleText = parameters.DivergenceAngleInDegrees.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        string mainRatioText = parameters.MainBranchLengthRatio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        string lateralRatioText = parameters.LateralBranchLengthRatio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        return $"""
            #define MainRatio {mainRatioText}
            #define LateralRatio {lateralRatioText}
            #define MainAngle {mainAngleText}
            #define LateralAngle {lateralAngleText}
            #define DivergenceAngle {divergenceAngleText}

            A(length, order) : order > 0 -> !(order * 0.28 + 0.1)F(length)[&({lateralAngleText})/({divergenceAngleText})A(length * LateralRatio, order - 1)]&({mainAngleText})A(length * MainRatio, order - 1)
            A(length, order) : order <= 0 -> !(0.1)F(length)
            """;
    }

    private void BuildHondaBranchRecursive(
        TreeStructureNode parentNode,
        TreeBranchingParameters parameters,
        double currentLength,
        int currentDepth,
        int maximumDepth)
    {
        if (currentDepth >= maximumDepth)
        {
            parentNode.IsTerminalBud = true;
            return;
        }

        double mainLength = currentLength * parameters.MainBranchLengthRatio;
        double lateralLength = currentLength * parameters.LateralBranchLengthRatio;

        double lateralPitchAngleRadians = parameters.LateralBranchAngleInDegrees * (Math.PI / 180.0);
        double divergenceAngleRadians = parameters.DivergenceAngleInDegrees * (Math.PI / 180.0);
        TurtleOrientation lateralOrientation = parentNode.Orientation
            .Pitch(lateralPitchAngleRadians)
            .Roll(divergenceAngleRadians);

        TreeStructureNode lateralChildNode = new TreeStructureNode(
            ++_nodeIdentifierCounter,
            parentNode.BranchOrder + 1,
            lateralLength,
            parameters.TerminalBranchRadius,
            parentNode.EndPosition,
            lateralOrientation,
            parentNode);
        parentNode.Children.Add(lateralChildNode);

        double mainPitchAngleRadians = parameters.MainBranchAngleInDegrees * (Math.PI / 180.0);
        TurtleOrientation mainOrientation = parentNode.Orientation.Pitch(mainPitchAngleRadians);

        TreeStructureNode mainChildNode = new TreeStructureNode(
            ++_nodeIdentifierCounter,
            parentNode.BranchOrder,
            mainLength,
            parameters.TerminalBranchRadius,
            parentNode.EndPosition,
            mainOrientation,
            parentNode);
        parentNode.Children.Add(mainChildNode);

        BuildHondaBranchRecursive(lateralChildNode, parameters, lateralLength, currentDepth + 1, maximumDepth);
        BuildHondaBranchRecursive(mainChildNode, parameters, mainLength, currentDepth + 1, maximumDepth);
    }
}