using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal class PatchContinuityEquationSolver
{
    public (BezierSurfacePatch LeftPatch, BezierSurfacePatch RightPatch) SolveBilateralPatches(
        IReadOnlyList<VectorThreeDimensional> leftBoundaryCurve,
        IReadOnlyList<VectorThreeDimensional> centralMidribCurve,
        IReadOnlyList<VectorThreeDimensional> rightBoundaryCurve)
    {
        ArgumentNullException.ThrowIfNull(leftBoundaryCurve);
        ArgumentNullException.ThrowIfNull(centralMidribCurve);
        ArgumentNullException.ThrowIfNull(rightBoundaryCurve);

        VectorThreeDimensional[] resampledLeft = ResampleCurve(leftBoundaryCurve, 4);
        VectorThreeDimensional[] resampledCenter = ResampleCurve(centralMidribCurve, 4);
        VectorThreeDimensional[] resampledRight = ResampleCurve(rightBoundaryCurve, 4);

        VectorThreeDimensional[,] leftGrid = new VectorThreeDimensional[4, 4];
        VectorThreeDimensional[,] rightGrid = new VectorThreeDimensional[4, 4];

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            leftGrid[rowIndex, 0] = resampledLeft[rowIndex];
            leftGrid[rowIndex, 3] = resampledCenter[rowIndex];
            rightGrid[rowIndex, 0] = resampledCenter[rowIndex];
            rightGrid[rowIndex, 3] = resampledRight[rowIndex];
        }

        for (int coordinateIndex = 0; coordinateIndex < 3; coordinateIndex++)
        {
            double[] leftColumnZero = ExtractCoordinateColumn(resampledLeft, coordinateIndex);
            double[] centerBoundary = ExtractCoordinateColumn(resampledCenter, coordinateIndex);
            double[] rightColumnThree = ExtractCoordinateColumn(resampledRight, coordinateIndex);

            (double[,] leftInner, double[,] rightInner) = SolveRowContinuitySystem(
                leftColumnZero,
                centerBoundary,
                rightColumnThree);

            for (int rowIndex = 0; rowIndex < 4; rowIndex++)
            {
                SetCoordinateComponent(ref leftGrid[rowIndex, 1], coordinateIndex, leftInner[rowIndex, 0]);
                SetCoordinateComponent(ref leftGrid[rowIndex, 2], coordinateIndex, leftInner[rowIndex, 1]);
                SetCoordinateComponent(ref rightGrid[rowIndex, 1], coordinateIndex, rightInner[rowIndex, 0]);
                SetCoordinateComponent(ref rightGrid[rowIndex, 2], coordinateIndex, rightInner[rowIndex, 1]);
            }
        }

        return (new BezierSurfacePatch(leftGrid), new BezierSurfacePatch(rightGrid));
    }

    private (double[,] LeftInnerColumns, double[,] RightInnerColumns) SolveRowContinuitySystem(
        double[] leftBoundary,
        double[] centerBoundary,
        double[] rightBoundary)
    {
        const int rowCount = 4;
        const int unknownCountPerRow = 4;
        const int totalUnknownCount = rowCount * unknownCountPerRow;

        double[,] coefficientMatrix = new double[totalUnknownCount, totalUnknownCount];
        double[] rightHandSideVector = new double[totalUnknownCount];

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            int baseEquationIndex = rowIndex * unknownCountPerRow;
            int baseUnknownIndex = rowIndex * unknownCountPerRow;

            // Equation 1: C1 Continuity Constraint across central seam (P^R_{i,1} + P^L_{i,2} - 2 * M_i = 0)
            coefficientMatrix[baseEquationIndex, baseUnknownIndex + 1] = 1.0;
            coefficientMatrix[baseEquationIndex, baseUnknownIndex + 2] = 1.0;
            rightHandSideVector[baseEquationIndex] = 2.0 * centerBoundary[rowIndex];

            // Equation 2: Uniform chord ratio constraint for Left patch inner row
            coefficientMatrix[baseEquationIndex + 1, baseUnknownIndex] = -2.0;
            coefficientMatrix[baseEquationIndex + 1, baseUnknownIndex + 1] = 1.0;
            rightHandSideVector[baseEquationIndex + 1] = -leftBoundary[rowIndex];

            // Equation 3: Uniform chord ratio constraint for Right patch inner row
            coefficientMatrix[baseEquationIndex + 2, baseUnknownIndex + 2] = 1.0;
            coefficientMatrix[baseEquationIndex + 2, baseUnknownIndex + 3] = -2.0;
            rightHandSideVector[baseEquationIndex + 2] = -rightBoundary[rowIndex];

            // Equation 4: Transverse smoothness constraint
            coefficientMatrix[baseEquationIndex + 3, baseUnknownIndex] = 1.0;
            coefficientMatrix[baseEquationIndex + 3, baseUnknownIndex + 1] = -1.0;
            coefficientMatrix[baseEquationIndex + 3, baseUnknownIndex + 2] = -1.0;
            coefficientMatrix[baseEquationIndex + 3, baseUnknownIndex + 3] = 1.0;
            rightHandSideVector[baseEquationIndex + 3] = 0.0;
        }

        double[] solutionVector = SolveLinearSystemWithPartialPivoting(coefficientMatrix, rightHandSideVector);

        double[,] leftInner = new double[rowCount, 2];
        double[,] rightInner = new double[rowCount, 2];

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            int baseUnknownIndex = rowIndex * unknownCountPerRow;
            leftInner[rowIndex, 0] = solutionVector[baseUnknownIndex];
            leftInner[rowIndex, 1] = solutionVector[baseUnknownIndex + 1];
            rightInner[rowIndex, 0] = solutionVector[baseUnknownIndex + 2];
            rightInner[rowIndex, 1] = solutionVector[baseUnknownIndex + 3];
        }

        return (leftInner, rightInner);
    }

    private double[] SolveLinearSystemWithPartialPivoting(double[,] matrix, double[] rightHandSide)
    {
        int equationCount = rightHandSide.Length;
        double[,] augmentedMatrix = new double[equationCount, equationCount + 1];

        for (int rowIndex = 0; rowIndex < equationCount; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < equationCount; columnIndex++)
            {
                augmentedMatrix[rowIndex, columnIndex] = matrix[rowIndex, columnIndex];
            }
            augmentedMatrix[rowIndex, equationCount] = rightHandSide[rowIndex];
        }

        for (int pivotColumnIndex = 0; pivotColumnIndex < equationCount; pivotColumnIndex++)
        {
            int maximumPivotRowIndex = pivotColumnIndex;
            double maximumPivotValue = Math.Abs(augmentedMatrix[pivotColumnIndex, pivotColumnIndex]);

            for (int candidateRowIndex = pivotColumnIndex + 1; candidateRowIndex < equationCount; candidateRowIndex++)
            {
                double candidateValue = Math.Abs(augmentedMatrix[candidateRowIndex, pivotColumnIndex]);
                if (candidateValue > maximumPivotValue)
                {
                    maximumPivotValue = candidateValue;
                    maximumPivotRowIndex = candidateRowIndex;
                }
            }

            if (maximumPivotValue < 1e-12)
            {
                continue;
            }

            if (maximumPivotRowIndex != pivotColumnIndex)
            {
                for (int columnIndex = pivotColumnIndex; columnIndex <= equationCount; columnIndex++)
                {
                    (augmentedMatrix[pivotColumnIndex, columnIndex], augmentedMatrix[maximumPivotRowIndex, columnIndex]) =
                        (augmentedMatrix[maximumPivotRowIndex, columnIndex], augmentedMatrix[pivotColumnIndex, columnIndex]);
                }
            }

            double pivotElement = augmentedMatrix[pivotColumnIndex, pivotColumnIndex];

            for (int targetRowIndex = pivotColumnIndex + 1; targetRowIndex < equationCount; targetRowIndex++)
            {
                double eliminationFactor = augmentedMatrix[targetRowIndex, pivotColumnIndex] / pivotElement;
                for (int columnIndex = pivotColumnIndex; columnIndex <= equationCount; columnIndex++)
                {
                    augmentedMatrix[targetRowIndex, columnIndex] -= eliminationFactor * augmentedMatrix[pivotColumnIndex, columnIndex];
                }
            }
        }

        double[] solutionVector = new double[equationCount];

        for (int rowIndex = equationCount - 1; rowIndex >= 0; rowIndex--)
        {
            double accumulatedSum = augmentedMatrix[rowIndex, equationCount];
            for (int columnIndex = rowIndex + 1; columnIndex < equationCount; columnIndex++)
            {
                accumulatedSum -= augmentedMatrix[rowIndex, columnIndex] * solutionVector[columnIndex];
            }

            double diagonalElement = augmentedMatrix[rowIndex, rowIndex];
            solutionVector[rowIndex] = Math.Abs(diagonalElement) > 1e-12 ? accumulatedSum / diagonalElement : 0.0;
        }

        return solutionVector;
    }

    private static double[] ExtractCoordinateColumn(IReadOnlyList<VectorThreeDimensional> points, int coordinateIndex)
    {
        double[] coordinates = new double[points.Count];
        for (int index = 0; index < points.Count; index++)
        {
            coordinates[index] = coordinateIndex switch
            {
                0 => points[index].X,
                1 => points[index].Y,
                _ => points[index].Z
            };
        }
        return coordinates;
    }

    private static void SetCoordinateComponent(ref VectorThreeDimensional vector, int coordinateIndex, double value)
    {
        vector = coordinateIndex switch
        {
            0 => new VectorThreeDimensional(value, vector.Y, vector.Z),
            1 => new VectorThreeDimensional(vector.X, value, vector.Z),
            _ => new VectorThreeDimensional(vector.X, vector.Y, value)
        };
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
}