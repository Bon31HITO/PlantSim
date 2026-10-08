using PlantSim.LSystem;
using System;
using System.Collections.Generic;

namespace PlantSim.Trees;

internal class TreeInterpretationConverter
{
    internal (IReadOnlyList<TurtleBranchSegment> Branches,
              IReadOnlyList<TurtlePolygon> FoliagePolygons,
              IReadOnlyList<TurtleSphere> Buds) ConvertToTurtleGeometry(
        TreeStructureNode rootNode,
        int trunkColorIndex = 0,
        int foliageColorIndex = 2)
    {
        ArgumentNullException.ThrowIfNull(rootNode);

        List<TurtleBranchSegment> branchList = [];
        List<TurtlePolygon> foliageList = [];
        List<TurtleSphere> budList = [];

        TraverseAndConvert(rootNode, branchList, foliageList, budList, trunkColorIndex, foliageColorIndex);

        return (branchList, foliageList, budList);
    }

    internal Word ConvertToLSystemWord(TreeStructureNode rootNode)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        List<Symbol> symbolList = [];
        TraverseAndGenerateWord(rootNode, symbolList);
        return new Word(symbolList);
    }

    private void TraverseAndConvert(
        TreeStructureNode currentNode,
        List<TurtleBranchSegment> branchList,
        List<TurtlePolygon> foliageList,
        List<TurtleSphere> budList,
        int trunkColorIndex,
        int foliageColorIndex)
    {
        if (currentNode.IsPruned && currentNode.Children.Count == 0)
        {
            return;
        }

        double parentThickness = currentNode.Parent != null ? currentNode.Parent.Radius * 2.0 : currentNode.Radius * 2.0;
        double currentThickness = currentNode.Radius * 2.0;

        // 木質化度（Lignification）に応じた樹皮色・若枝色の自動遷移
        int activeColorIndex;
        if (currentNode.BranchOrder == 0 || currentNode.Radius >= 0.45)
        {
            // 成熟した主幹・大骨格枝：成熟樹皮色（濃褐色）
            activeColorIndex = trunkColorIndex;
        }
        else if (currentNode.Radius >= 0.28 || currentNode.AgeInSteps >= 10)
        {
            // 二次肥大の進んだ中径側枝：木質化遷移色（オリーブ褐色）
            activeColorIndex = trunkColorIndex + 1;
        }
        else if (currentNode.Radius >= 0.18 || currentNode.AgeInSteps >= 4)
        {
            // 木質化が始まった小枝：成熟若枝色（暗緑色）
            activeColorIndex = foliageColorIndex + 1;
        }
        else
        {
            // 今期伸長したばかりの新梢先端：明緑色
            activeColorIndex = foliageColorIndex;
        }

        branchList.Add(new TurtleBranchSegment(
            currentNode.StartPosition,
            currentNode.EndPosition,
            currentNode.Orientation,
            currentNode.Orientation,
            parentThickness,
            currentThickness,
            activeColorIndex));

        // 生存している末端芽にのみ新葉と頂芽を配置
        if (!currentNode.IsPruned && (currentNode.Children.Count == 0 || currentNode.IsTerminalBud))
        {
            budList.Add(new TurtleSphere(currentNode.EndPosition, Math.Max(0.12, currentNode.Radius * 1.2), foliageColorIndex));
            foliageList.Add(GenerateTerminalLeafPolygon(currentNode.EndPosition, currentNode.Orientation, foliageColorIndex));
        }

        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            TraverseAndConvert(currentNode.Children[childIndex], branchList, foliageList, budList, trunkColorIndex, foliageColorIndex);
        }
    }

    private void TraverseAndGenerateWord(TreeStructureNode currentNode, List<Symbol> symbols)
    {
        if (currentNode.IsPruned && currentNode.Children.Count == 0)
        {
            return;
        }

        symbols.Add(new Symbol("!", currentNode.Radius * 2.0));
        symbols.Add(new Symbol("F", currentNode.SegmentLength));

        if (currentNode.Children.Count == 0)
        {
            if (!currentNode.IsPruned)
            {
                symbols.Add(new Symbol("@", Math.Max(0.15, currentNode.Radius * 1.2)));
            }
            return;
        }

        for (int childIndex = 0; childIndex < currentNode.Children.Count; childIndex++)
        {
            TreeStructureNode childNode = currentNode.Children[childIndex];
            symbols.Add(new Symbol("["));
            symbols.Add(new Symbol("&", 25.0));
            symbols.Add(new Symbol("/", 137.5 * childIndex));
            TraverseAndGenerateWord(childNode, symbols);
            symbols.Add(new Symbol("]"));
        }
    }

    private static TurtlePolygon GenerateTerminalLeafPolygon(
        VectorThreeDimensional apexPosition,
        TurtleOrientation orientation,
        int colorIndex)
    {
        double leafLength = 2.4;
        double leafWidth = 0.9;
        VectorThreeDimensional heading = orientation.HeadingDirection;
        VectorThreeDimensional left = orientation.LeftDirection;

        VectorThreeDimensional basePoint = apexPosition;
        VectorThreeDimensional midLeftPoint = apexPosition + (heading * (leafLength * 0.4)) + (left * (leafWidth * 0.5));
        VectorThreeDimensional tipPoint = apexPosition + (heading * leafLength);
        VectorThreeDimensional midRightPoint = apexPosition + (heading * (leafLength * 0.4)) - (left * (leafWidth * 0.5));

        return new TurtlePolygon([basePoint, midLeftPoint, tipPoint, midRightPoint], colorIndex);
    }
}