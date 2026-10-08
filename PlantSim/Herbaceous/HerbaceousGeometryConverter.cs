using PlantSim.LSystem;
using System;
using System.Collections.Generic;

namespace PlantSim.Herbaceous;

internal class HerbaceousGeometryConverter
{
    internal (IReadOnlyList<TurtleBranchSegment> Branches,
              IReadOnlyList<TurtlePolygon> FoliagePolygons,
              IReadOnlyList<TurtleSphere> Buds,
              IReadOnlyList<TurtlePolygon> Petals,
              IReadOnlyList<TurtlePolygon> Fruits) ConvertToTurtleGeometry(
        HerbaceousMetamerNode rootNode,
        HerbaceousParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(parameters);

        List<TurtleBranchSegment> branchList = [];
        List<TurtlePolygon> foliageList = [];
        List<TurtleSphere> budList = [];
        List<TurtlePolygon> petalList = [];
        List<TurtlePolygon> fruitList = [];

        TraverseAndConvert(rootNode, parameters, branchList, foliageList, budList, petalList, fruitList);
        return (branchList, foliageList, budList, petalList, fruitList);
    }

    private void TraverseAndConvert(
        HerbaceousMetamerNode currentNode,
        HerbaceousParameters parameters,
        List<TurtleBranchSegment> branchList,
        List<TurtlePolygon> foliageList,
        List<TurtleSphere> budList,
        List<TurtlePolygon> petalList,
        List<TurtlePolygon> fruitList)
    {
        int stemColorIndex = currentNode.BranchOrder == 0 ? 1 : 2;
        double startThickness = currentNode.Parent != null ? currentNode.Parent.Radius * 2.0 : currentNode.Radius * 2.0;
        double endThickness = currentNode.Radius * 2.0;

        branchList.Add(new TurtleBranchSegment(
            currentNode.StartPosition,
            currentNode.EndPosition,
            currentNode.Orientation,
            currentNode.Orientation,
            startThickness,
            endThickness,
            stemColorIndex));

        // Foliage (Leaves)
        if (currentNode.LeafScale > 0.05)
        {
            foliageList.Add(GenerateFoliagePolygon(
                currentNode.StartPosition,
                currentNode.Orientation,
                currentNode.LeafScale,
                parameters.BranchingType));
        }

        // Floral Organ Morphogenesis
        switch (currentNode.Stage)
        {
            case HerbaceousOrganStage.VegetativeBud:
                if (currentNode.IsActiveApex)
                {
                    budList.Add(new TurtleSphere(currentNode.EndPosition, Math.Max(0.08, currentNode.Radius * 1.3), 3));
                }
                break;

            case HerbaceousOrganStage.FloralBud:
                budList.Add(new TurtleSphere(currentNode.EndPosition, Math.Max(0.12, currentNode.Radius * 1.8), 4));
                break;

            case HerbaceousOrganStage.BloomingFlower:
                budList.Add(new TurtleSphere(currentNode.EndPosition, 0.16, 8)); // Center
                GenerateFlowerPetals(currentNode.EndPosition, currentNode.Orientation, petalList, parameters);
                break;

            case HerbaceousOrganStage.FruitCapsule:
                GenerateFruitCapsule(currentNode.EndPosition, currentNode.Orientation, fruitList, parameters);
                break;

            case HerbaceousOrganStage.Senescent:
                budList.Add(new TurtleSphere(currentNode.EndPosition, 0.10, 14));
                break;
        }

        for (int index = 0; index < currentNode.Children.Count; index++)
        {
            TraverseAndConvert(currentNode.Children[index], parameters, branchList, foliageList, budList, petalList, fruitList);
        }
        for (int index = 0; index < currentNode.LateralBranches.Count; index++)
        {
            TraverseAndConvert(currentNode.LateralBranches[index], parameters, branchList, foliageList, budList, petalList, fruitList);
        }
    }

    private static TurtlePolygon GenerateFoliagePolygon(
        VectorThreeDimensional attachPosition,
        TurtleOrientation stemOrientation,
        double leafScale,
        InflorescenceBranchingType branchingType)
    {
        double length = 2.2 * leafScale;
        double width = (branchingType == InflorescenceBranchingType.SympodialDichasium ? 0.7 : 1.1) * leafScale;

        VectorThreeDimensional heading = stemOrientation.HeadingDirection;
        VectorThreeDimensional left = stemOrientation.LeftDirection;
        VectorThreeDimensional normal = stemOrientation.UpDirection;

        VectorThreeDimensional basePoint = attachPosition;
        VectorThreeDimensional midPoint = attachPosition + (heading * (length * 0.45)) + (normal * (length * 0.25));
        VectorThreeDimensional leftPoint = midPoint + (left * (width * 0.5));
        VectorThreeDimensional rightPoint = midPoint - (left * (width * 0.5));
        VectorThreeDimensional tipPoint = attachPosition + (heading * length) + (normal * (length * 0.15));

        return new TurtlePolygon([basePoint, leftPoint, tipPoint, rightPoint], 5);
    }

    private static void GenerateFlowerPetals(
        VectorThreeDimensional flowerCenter,
        TurtleOrientation orientation,
        List<TurtlePolygon> petalList,
        HerbaceousParameters parameters)
    {
        int petalCount = parameters.BranchingType == InflorescenceBranchingType.SympodialDichasium ? 5 : 4;
        int petalColorIndex = parameters.BranchingType == InflorescenceBranchingType.SympodialDichasium ? 6 : 7;
        double petalLength = parameters.BranchingType == InflorescenceBranchingType.SympodialDichasium ? 1.4 : 0.85;
        double petalWidth = petalLength * 0.55;

        for (int index = 0; index < petalCount; index++)
        {
            double angleRadians = (2.0 * Math.PI * index) / petalCount;
            double cosine = Math.Cos(angleRadians);
            double sine = Math.Sin(angleRadians);

            VectorThreeDimensional radialDirection = (orientation.LeftDirection * cosine) + (orientation.UpDirection * sine);
            VectorThreeDimensional lateralDirection = (orientation.LeftDirection * -sine) + (orientation.UpDirection * cosine);

            VectorThreeDimensional petalBase = flowerCenter;
            VectorThreeDimensional petalLeft = flowerCenter + (radialDirection * (petalLength * 0.5)) + (lateralDirection * (petalWidth * 0.5));
            VectorThreeDimensional petalTip = flowerCenter + (radialDirection * petalLength);
            VectorThreeDimensional petalRight = flowerCenter + (radialDirection * (petalLength * 0.5)) - (lateralDirection * (petalWidth * 0.5));

            petalList.Add(new TurtlePolygon([petalBase, petalLeft, petalTip, petalRight], petalColorIndex));
        }
    }

    private static void GenerateFruitCapsule(
        VectorThreeDimensional position,
        TurtleOrientation orientation,
        List<TurtlePolygon> fruitList,
        HerbaceousParameters parameters)
    {
        // Capsella bursa-pastoris Silicle (Heart-shaped / Shamisen bachi flat capsule)
        double silicleLength = 1.35;
        double silicleWidth = 1.25;

        VectorThreeDimensional heading = orientation.HeadingDirection;
        VectorThreeDimensional left = orientation.LeftDirection;

        VectorThreeDimensional stalkAttachment = position;
        VectorThreeDimensional shoulderLeft = position + (heading * silicleLength) + (left * (silicleWidth * 0.5));
        VectorThreeDimensional apexNotch = position + (heading * (silicleLength * 0.85));
        VectorThreeDimensional shoulderRight = position + (heading * silicleLength) - (left * (silicleWidth * 0.5));

        fruitList.Add(new TurtlePolygon([stalkAttachment, shoulderLeft, apexNotch, shoulderRight], 4));
    }
}