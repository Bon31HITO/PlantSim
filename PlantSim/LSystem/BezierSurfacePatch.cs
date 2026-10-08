using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PlantSim.LSystem;

internal class BezierSurfacePatch
{
    private readonly VectorThreeDimensional[,] _controlPoints;

    public BezierSurfacePatch(VectorThreeDimensional[,] controlPoints)
    {
        ArgumentNullException.ThrowIfNull(controlPoints);
        if (controlPoints.GetLength(0) != 4 || controlPoints.GetLength(1) != 4)
        {
            throw new ArgumentException("Bicubic Bezier patch requires a 4x4 control point grid.");
        }
        _controlPoints = controlPoints;
    }

    public static BezierSurfacePatch Parse(string definitionText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionText);

        List<double> coordinates = [];
        string[] lines = definitionText.Split(['\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (line.StartsWith('#') || line.StartsWith("//"))
            {
                continue;
            }

            string[] tokens = line.Split([' ', '\t', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                if (double.TryParse(tokens[tokenIndex], CultureInfo.InvariantCulture, out double parsedValue))
                {
                    coordinates.Add(parsedValue);
                }
            }
        }

        if (coordinates.Count < 48)
        {
            throw new FormatException($"Invalid Bezier patch data. Expected 48 coordinates, but found {coordinates.Count}.");
        }

        VectorThreeDimensional[,] points = new VectorThreeDimensional[4, 4];
        int coordinateIndex = 0;

        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                double coordinateX = coordinates[coordinateIndex++];
                double coordinateY = coordinates[coordinateIndex++];
                double coordinateZ = coordinates[coordinateIndex++];
                points[row, column] = new VectorThreeDimensional(coordinateX, coordinateY, coordinateZ);
            }
        }

        return new BezierSurfacePatch(points);
    }

    public static BezierSurfacePatch LoadFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Bezier patch file not found.", filePath);
        }

        string fileContent = File.ReadAllText(filePath);
        return Parse(fileContent);
    }

    public static BezierSurfacePatch CreateLeafPatch()
    {
        VectorThreeDimensional[,] points = new VectorThreeDimensional[4, 4];
        points[0, 0] = new VectorThreeDimensional(-0.05, 0.0, 0.0);
        points[0, 1] = new VectorThreeDimensional(-0.02, 0.0, 0.02);
        points[0, 2] = new VectorThreeDimensional(0.02, 0.0, 0.02);
        points[0, 3] = new VectorThreeDimensional(0.05, 0.0, 0.0);

        points[1, 0] = new VectorThreeDimensional(-0.35, 0.3, 0.08);
        points[1, 1] = new VectorThreeDimensional(-0.15, 0.3, 0.0);
        points[1, 2] = new VectorThreeDimensional(0.15, 0.3, 0.0);
        points[1, 3] = new VectorThreeDimensional(0.35, 0.3, 0.08);

        points[2, 0] = new VectorThreeDimensional(-0.40, 0.7, 0.06);
        points[2, 1] = new VectorThreeDimensional(-0.15, 0.7, -0.04);
        points[2, 2] = new VectorThreeDimensional(0.15, 0.7, -0.04);
        points[2, 3] = new VectorThreeDimensional(0.40, 0.7, 0.06);

        points[3, 0] = new VectorThreeDimensional(-0.05, 1.0, -0.08);
        points[3, 1] = new VectorThreeDimensional(0.0, 1.05, -0.10);
        points[3, 2] = new VectorThreeDimensional(0.0, 1.05, -0.10);
        points[3, 3] = new VectorThreeDimensional(0.05, 1.0, -0.08);

        return new BezierSurfacePatch(points);
    }

    public static BezierSurfacePatch CreatePetalPatch()
    {
        VectorThreeDimensional[,] points = new VectorThreeDimensional[4, 4];
        points[0, 0] = new VectorThreeDimensional(-0.05, 0.0, 0.0);
        points[0, 1] = new VectorThreeDimensional(-0.02, 0.0, 0.05);
        points[0, 2] = new VectorThreeDimensional(0.02, 0.0, 0.05);
        points[0, 3] = new VectorThreeDimensional(0.05, 0.0, 0.0);

        points[1, 0] = new VectorThreeDimensional(-0.30, 0.35, 0.15);
        points[1, 1] = new VectorThreeDimensional(-0.12, 0.35, 0.02);
        points[1, 2] = new VectorThreeDimensional(0.12, 0.35, 0.02);
        points[1, 3] = new VectorThreeDimensional(0.30, 0.35, 0.15);

        points[2, 0] = new VectorThreeDimensional(-0.35, 0.75, 0.20);
        points[2, 1] = new VectorThreeDimensional(-0.15, 0.75, 0.05);
        points[2, 2] = new VectorThreeDimensional(0.15, 0.75, 0.05);
        points[2, 3] = new VectorThreeDimensional(0.35, 0.75, 0.20);

        points[3, 0] = new VectorThreeDimensional(-0.20, 1.0, 0.10);
        points[3, 1] = new VectorThreeDimensional(-0.05, 1.05, 0.02);
        points[3, 2] = new VectorThreeDimensional(0.05, 1.05, 0.02);
        points[3, 3] = new VectorThreeDimensional(0.20, 1.0, 0.10);

        return new BezierSurfacePatch(points);
    }

    public BezierSurfacePatch CreateDeformedPatch(
        VectorThreeDimensional startContactPoint,
        VectorThreeDimensional endContactPoint,
        VectorThreeDimensional referenceNormalDirection)
    {
        VectorThreeDimensional longitudinalVector = endContactPoint - startContactPoint;
        double spanLength = longitudinalVector.Length;

        if (spanLength < 1e-7)
        {
            spanLength = 1.0;
            longitudinalVector = VectorThreeDimensional.UnitY;
        }

        VectorThreeDimensional headingDirection = longitudinalVector.Normalize();
        VectorThreeDimensional normalDirection = referenceNormalDirection.Normalize();

        if (normalDirection.LengthSquared < 1e-6 || Math.Abs(VectorThreeDimensional.DotProduct(headingDirection, normalDirection)) > 0.95)
        {
            normalDirection = Math.Abs(headingDirection.X) < 0.8 ? VectorThreeDimensional.UnitX : VectorThreeDimensional.UnitZ;
        }

        VectorThreeDimensional lateralDirection = VectorThreeDimensional.CrossProduct(normalDirection, headingDirection).Normalize();
        VectorThreeDimensional correctedNormal = VectorThreeDimensional.CrossProduct(headingDirection, lateralDirection).Normalize();

        VectorThreeDimensional[,] deformedGrid = new VectorThreeDimensional[4, 4];

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            double rowRatio = (double)rowIndex / 3.0;
            VectorThreeDimensional interpolatedBase = startContactPoint + (longitudinalVector * rowRatio);

            for (int columnIndex = 0; columnIndex < 4; columnIndex++)
            {
                VectorThreeDimensional localPoint = _controlPoints[rowIndex, columnIndex];
                double lateralOffset = localPoint.X * spanLength;
                double normalOffset = localPoint.Z * spanLength;

                VectorThreeDimensional transformedPoint = interpolatedBase +
                    (lateralDirection * lateralOffset) +
                    (correctedNormal * normalOffset);

                deformedGrid[rowIndex, columnIndex] = transformedPoint;
            }
        }

        return new BezierSurfacePatch(deformedGrid);
    }

    public static BezierSurfacePatch CreateFromBoundaryCurves(
        IReadOnlyList<VectorThreeDimensional> leftBoundaryCurve,
        IReadOnlyList<VectorThreeDimensional> rightBoundaryCurve)
    {
        ArgumentNullException.ThrowIfNull(leftBoundaryCurve);
        ArgumentNullException.ThrowIfNull(rightBoundaryCurve);

        if (leftBoundaryCurve.Count < 2 || rightBoundaryCurve.Count < 2)
        {
            return CreateLeafPatch();
        }

        VectorThreeDimensional[] resampledLeft = ResampleCurve(leftBoundaryCurve, 4);
        VectorThreeDimensional[] resampledRight = ResampleCurve(rightBoundaryCurve, 4);

        VectorThreeDimensional[,] controlGrid = new VectorThreeDimensional[4, 4];

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            VectorThreeDimensional leftPoint = resampledLeft[rowIndex];
            VectorThreeDimensional rightPoint = resampledRight[rowIndex];

            for (int columnIndex = 0; columnIndex < 4; columnIndex++)
            {
                double columnRatio = (double)columnIndex / 3.0;
                controlGrid[rowIndex, columnIndex] = leftPoint + ((rightPoint - leftPoint) * columnRatio);
            }
        }

        return new BezierSurfacePatch(controlGrid);
    }

    public static (BezierSurfacePatch LeftPatch, BezierSurfacePatch RightPatch) CreateBilateralSmoothPatches(
        IReadOnlyList<VectorThreeDimensional> leftBoundaryCurve,
        IReadOnlyList<VectorThreeDimensional> centralMidribCurve,
        IReadOnlyList<VectorThreeDimensional> rightBoundaryCurve)
    {
        PatchContinuityEquationSolver solver = new PatchContinuityEquationSolver();
        return solver.SolveBilateralPatches(leftBoundaryCurve, centralMidribCurve, rightBoundaryCurve);
    }

    private static VectorThreeDimensional[] ResampleCurve(IReadOnlyList<VectorThreeDimensional> points, int targetCount)
    {
        VectorThreeDimensional[] resultPoints = new VectorThreeDimensional[targetCount];
        if (points.Count == 0)
        {
            return resultPoints;
        }

        double[] cumulativeDistances = new double[points.Count];
        cumulativeDistances[0] = 0.0;
        for (int index = 1; index < points.Count; index++)
        {
            cumulativeDistances[index] = cumulativeDistances[index - 1] + (points[index] - points[index - 1]).Length;
        }

        double totalDistance = cumulativeDistances[^1];
        if (totalDistance < 1e-9)
        {
            for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                resultPoints[targetIndex] = points[0];
            }
            return resultPoints;
        }

        resultPoints[0] = points[0];
        resultPoints[^1] = points[^1];

        for (int targetIndex = 1; targetIndex < targetCount - 1; targetIndex++)
        {
            double targetDistance = (totalDistance * targetIndex) / (targetCount - 1);
            int segmentIndex = 0;
            while (segmentIndex < cumulativeDistances.Length - 1 && cumulativeDistances[segmentIndex + 1] < targetDistance)
            {
                segmentIndex++;
            }

            double segmentStartDistance = cumulativeDistances[segmentIndex];
            double segmentLength = cumulativeDistances[segmentIndex + 1] - segmentStartDistance;
            double interpolationRatio = segmentLength > 1e-9 ? (targetDistance - segmentStartDistance) / segmentLength : 0.0;
            resultPoints[targetIndex] = points[segmentIndex] + ((points[segmentIndex + 1] - points[segmentIndex]) * interpolationRatio);
        }

        return resultPoints;
    }

    public (VectorThreeDimensional Position, VectorThreeDimensional Normal) Evaluate(double parameterU, double parameterV)
    {
        double parameterClampedU = Math.Clamp(parameterU, 0.0, 1.0);
        double parameterClampedV = Math.Clamp(parameterV, 0.0, 1.0);

        Span<double> basisU = stackalloc double[4];
        Span<double> basisV = stackalloc double[4];
        Span<double> derivativeBasisU = stackalloc double[4];
        Span<double> derivativeBasisV = stackalloc double[4];

        CalculateBernsteinBasisPolynomials(parameterClampedU, basisU, derivativeBasisU);
        CalculateBernsteinBasisPolynomials(parameterClampedV, basisV, derivativeBasisV);

        VectorThreeDimensional evaluatedPosition = VectorThreeDimensional.Zero;
        VectorThreeDimensional tangentU = VectorThreeDimensional.Zero;
        VectorThreeDimensional tangentV = VectorThreeDimensional.Zero;

        for (int indexU = 0; indexU < 4; indexU++)
        {
            for (int indexV = 0; indexV < 4; indexV++)
            {
                VectorThreeDimensional point = _controlPoints[indexU, indexV];
                double weight = basisU[indexU] * basisV[indexV];
                evaluatedPosition += point * weight;

                double weightTangentU = derivativeBasisU[indexU] * basisV[indexV];
                tangentU += point * weightTangentU;

                double weightTangentV = basisU[indexU] * derivativeBasisV[indexV];
                tangentV += point * weightTangentV;
            }
        }

        VectorThreeDimensional normal = VectorThreeDimensional.CrossProduct(tangentU, tangentV).Normalize();
        if (normal.LengthSquared < 1e-8)
        {
            normal = VectorThreeDimensional.UnitZ;
        }

        return (evaluatedPosition, normal);
    }

    private static void CalculateBernsteinBasisPolynomials(
        double parameter,
        Span<double> basisValues,
        Span<double> derivativeValues)
    {
        double oneMinusParameter = 1.0 - parameter;
        double parameterSquared = parameter * parameter;
        double oneMinusParameterSquared = oneMinusParameter * oneMinusParameter;

        basisValues[0] = oneMinusParameterSquared * oneMinusParameter;
        basisValues[1] = 3.0 * parameter * oneMinusParameterSquared;
        basisValues[2] = 3.0 * parameterSquared * oneMinusParameter;
        basisValues[3] = parameterSquared * parameter;

        derivativeValues[0] = -3.0 * oneMinusParameterSquared;
        derivativeValues[1] = 3.0 * (oneMinusParameterSquared - (2.0 * parameter * oneMinusParameter));
        derivativeValues[2] = 3.0 * ((2.0 * parameter * oneMinusParameter) - parameterSquared);
        derivativeValues[3] = 3.0 * parameterSquared;
    }
}