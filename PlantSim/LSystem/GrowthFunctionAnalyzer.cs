using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal class GrowthFunctionAnalyzer
{
    public GrowthAnalysisResult Analyze(
        IReadOnlyList<GrowthStepRecord> stepRecords,
        LindenmayerGrammar? grammar = null)
    {
        ArgumentNullException.ThrowIfNull(stepRecords);

        GrowthMatrixData? productionMatrixData = null;
        if (grammar != null)
        {
            productionMatrixData = AnalyzeProductionMatrix(grammar);
        }

        if (stepRecords.Count < 2)
        {
            return new GrowthAnalysisResult(
                stepRecords,
                GrowthClassificationKind.Constant,
                "Insufficient steps for growth kinetics estimation.",
                1.0,
                0,
                productionMatrixData);
        }

        int stepCount = stepRecords.Count;
        double[] horizontalCoordinates = new double[stepCount];
        double[] verticalCoordinates = new double[stepCount];
        for (int recordIndex = 0; recordIndex < stepCount; recordIndex++)
        {
            horizontalCoordinates[recordIndex] = stepRecords[recordIndex].StepIndex;
            verticalCoordinates[recordIndex] = stepRecords[recordIndex].TotalSymbolCount;
        }

        double linearCoefficient = CalculateLinearDetermination(horizontalCoordinates, verticalCoordinates);
        double exponentialCoefficient = CalculateExponentialDetermination(horizontalCoordinates, verticalCoordinates);
        double quadraticCoefficient = CalculateQuadraticDetermination(horizontalCoordinates, verticalCoordinates);
        bool isSigmoidalKinetics = CheckSigmoidalTrend(verticalCoordinates);

        GrowthClassificationKind selectedKind;
        double bestCoefficient;
        int polynomialDegree = 1;
        string description;

        if (productionMatrixData != null && productionMatrixData.Eigenvalues.Count > 0)
        {
            double spectralRadius = productionMatrixData.SpectralRadius;
            if (spectralRadius > 1.0001)
            {
                selectedKind = GrowthClassificationKind.Exponential;
                bestCoefficient = exponentialCoefficient;
                description = $"Algebraic Exponential Growth (Spectral Radius = {spectralRadius:F4}, R² = {bestCoefficient:F4})";
            }
            else if (Math.Abs(spectralRadius - 1.0) <= 0.0001)
            {
                if (quadraticCoefficient > 0.96 && quadraticCoefficient > linearCoefficient + 0.03)
                {
                    selectedKind = GrowthClassificationKind.Polynomial;
                    bestCoefficient = quadraticCoefficient;
                    polynomialDegree = 2;
                    description = $"Algebraic Polynomial Growth (Degree 2, Spectral Radius = 1.0, R² = {bestCoefficient:F4})";
                }
                else if (linearCoefficient > 0.85)
                {
                    selectedKind = GrowthClassificationKind.Linear;
                    bestCoefficient = linearCoefficient;
                    description = $"Algebraic Linear Growth (Degree 1, Spectral Radius = 1.0, R² = {bestCoefficient:F4})";
                }
                else
                {
                    selectedKind = GrowthClassificationKind.Constant;
                    bestCoefficient = 1.0;
                    description = "Algebraic Constant Growth (Spectral Radius = 1.0)";
                }
            }
            else
            {
                selectedKind = GrowthClassificationKind.Terminating;
                bestCoefficient = linearCoefficient;
                description = $"Terminating / Decaying Growth (Spectral Radius = {spectralRadius:F4})";
            }
        }
        else
        {
            if (isSigmoidalKinetics && stepCount >= 4)
            {
                selectedKind = GrowthClassificationKind.Sigmoidal;
                bestCoefficient = Math.Max(linearCoefficient, quadraticCoefficient);
                description = $"Sigmoidal (Logistic saturation, R² = {bestCoefficient:F3})";
            }
            else if (exponentialCoefficient > 0.95 && exponentialCoefficient >= linearCoefficient)
            {
                selectedKind = GrowthClassificationKind.Exponential;
                bestCoefficient = exponentialCoefficient;
                description = $"Exponential Growth (Geometric multiplication, R² = {bestCoefficient:F4})";
            }
            else if (quadraticCoefficient > 0.96 && quadraticCoefficient > linearCoefficient + 0.03)
            {
                selectedKind = GrowthClassificationKind.Polynomial;
                bestCoefficient = quadraticCoefficient;
                polynomialDegree = 2;
                description = $"Polynomial Growth (Degree 2, R² = {bestCoefficient:F4})";
            }
            else if (linearCoefficient > 0.85)
            {
                selectedKind = GrowthClassificationKind.Linear;
                bestCoefficient = linearCoefficient;
                description = $"Linear Growth (Constant elongation, R² = {bestCoefficient:F4})";
            }
            else
            {
                selectedKind = GrowthClassificationKind.Unknown;
                bestCoefficient = Math.Max(linearCoefficient, exponentialCoefficient);
                description = $"Complex Growth Kinetics (R² = {bestCoefficient:F3})";
            }
        }

        return new GrowthAnalysisResult(
            stepRecords,
            selectedKind,
            description,
            bestCoefficient,
            polynomialDegree,
            productionMatrixData);
    }

    private GrowthMatrixData AnalyzeProductionMatrix(LindenmayerGrammar grammar)
    {
        List<string> alphabetList = ExtractAlphabetSymbols(grammar);
        int dimension = alphabetList.Count;
        if (dimension == 0)
        {
            return new GrowthMatrixData(alphabetList, new double[0, 0], Array.Empty<ComplexEigenvalue>(), 0.0, 0);
        }

        Dictionary<string, int> symbolToIndexMap = new Dictionary<string, int>(dimension);
        for (int index = 0; index < dimension; index++)
        {
            symbolToIndexMap[alphabetList[index]] = index;
        }

        double[,] productionMatrix = new double[dimension, dimension];

        for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
        {
            string predecessorSymbol = alphabetList[rowIndex];
            List<IProductionRule> candidateRules = [];
            for (int ruleIndex = 0; ruleIndex < grammar.Rules.Count; ruleIndex++)
            {
                if (grammar.Rules[ruleIndex].PredecessorName == predecessorSymbol)
                {
                    candidateRules.Add(grammar.Rules[ruleIndex]);
                }
            }

            if (candidateRules.Count == 0)
            {
                productionMatrix[rowIndex, rowIndex] = 1.0;
                continue;
            }

            double totalProbability = 0.0;
            for (int candidateIndex = 0; candidateIndex < candidateRules.Count; candidateIndex++)
            {
                totalProbability += candidateRules[candidateIndex].Probability;
            }
            if (totalProbability <= 1e-9)
            {
                totalProbability = 1.0;
            }

            for (int candidateIndex = 0; candidateIndex < candidateRules.Count; candidateIndex++)
            {
                IProductionRule rule = candidateRules[candidateIndex];
                double weight = rule.Probability / totalProbability;

                for (int symbolIndex = 0; symbolIndex < rule.SuccessorSymbolNames.Count; symbolIndex++)
                {
                    string successorSymbolName = rule.SuccessorSymbolNames[symbolIndex];
                    if (symbolToIndexMap.TryGetValue(successorSymbolName, out int columnIndex))
                    {
                        productionMatrix[rowIndex, columnIndex] += weight;
                    }
                }
            }
        }

        double[] polynomialCoefficients = CalculateCharacteristicPolynomial(productionMatrix, dimension);
        IReadOnlyList<ComplexEigenvalue> eigenvalues = CalculatePolynomialRoots(polynomialCoefficients);

        double maximumSpectralRadius = 0.0;
        int dominantMultiplicity = 0;
        for (int eigenvalueIndex = 0; eigenvalueIndex < eigenvalues.Count; eigenvalueIndex++)
        {
            double magnitude = eigenvalues[eigenvalueIndex].Magnitude;
            if (magnitude > maximumSpectralRadius)
            {
                maximumSpectralRadius = magnitude;
                dominantMultiplicity = 1;
            }
            else if (Math.Abs(magnitude - maximumSpectralRadius) < 1e-4)
            {
                dominantMultiplicity++;
            }
        }

        return new GrowthMatrixData(
            alphabetList,
            productionMatrix,
            eigenvalues,
            maximumSpectralRadius,
            dominantMultiplicity);
    }

    private static List<string> ExtractAlphabetSymbols(LindenmayerGrammar grammar)
    {
        HashSet<string> uniqueSymbols = [];
        for (int index = 0; index < grammar.Axiom.Count; index++)
        {
            uniqueSymbols.Add(grammar.Axiom[index].Name);
        }
        for (int ruleIndex = 0; ruleIndex < grammar.Rules.Count; ruleIndex++)
        {
            uniqueSymbols.Add(grammar.Rules[ruleIndex].PredecessorName);
        }
        return [.. uniqueSymbols];
    }

    private static double[] CalculateCharacteristicPolynomial(double[,] matrix, int dimension)
    {
        double[] coefficients = new double[dimension + 1];
        coefficients[0] = 1.0;

        double[,] matrixPower = new double[dimension, dimension];
        double[,] temporaryMatrix = new double[dimension, dimension];

        for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < dimension; columnIndex++)
            {
                matrixPower[rowIndex, columnIndex] = matrix[rowIndex, columnIndex];
            }
        }

        for (int stepIndex = 1; stepIndex <= dimension; stepIndex++)
        {
            double trace = 0.0;
            for (int diagonalIndex = 0; diagonalIndex < dimension; diagonalIndex++)
            {
                trace += matrixPower[diagonalIndex, diagonalIndex];
            }

            double currentCoefficient = -trace / stepIndex;
            coefficients[stepIndex] = currentCoefficient;

            if (stepIndex == dimension)
            {
                break;
            }

            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < dimension; columnIndex++)
                {
                    double value = matrixPower[rowIndex, columnIndex];
                    if (rowIndex == columnIndex)
                    {
                        value += currentCoefficient;
                    }
                    temporaryMatrix[rowIndex, columnIndex] = value;
                }
            }

            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < dimension; columnIndex++)
                {
                    double sum = 0.0;
                    for (int innerIndex = 0; innerIndex < dimension; innerIndex++)
                    {
                        sum += matrix[rowIndex, innerIndex] * temporaryMatrix[innerIndex, columnIndex];
                    }
                    matrixPower[rowIndex, columnIndex] = sum;
                }
            }
        }

        return coefficients;
    }

    private static IReadOnlyList<ComplexEigenvalue> CalculatePolynomialRoots(double[] coefficients)
    {
        int degree = coefficients.Length - 1;
        if (degree <= 0)
        {
            return Array.Empty<ComplexEigenvalue>();
        }

        if (degree == 1)
        {
            double rootValue = -coefficients[1] / coefficients[0];
            return [new ComplexEigenvalue(rootValue, 0.0, Math.Abs(rootValue))];
        }

        double[] normalizedCoefficients = new double[degree + 1];
        double leadingCoefficient = coefficients[0];
        for (int index = 0; index <= degree; index++)
        {
            normalizedCoefficients[index] = coefficients[index] / leadingCoefficient;
        }

        double[] realRoots = new double[degree];
        double[] imaginaryRoots = new double[degree];
        double initialRadius = 1.0;
        for (int index = 1; index <= degree; index++)
        {
            initialRadius = Math.Max(initialRadius, Math.Pow(Math.Abs(normalizedCoefficients[index]), 1.0 / index));
        }

        for (int rootIndex = 0; rootIndex < degree; rootIndex++)
        {
            double angle = (2.0 * Math.PI * rootIndex) / degree + 0.35;
            realRoots[rootIndex] = initialRadius * Math.Cos(angle);
            imaginaryRoots[rootIndex] = initialRadius * Math.Sin(angle);
        }

        const int maximumIterations = 80;
        for (int iteration = 0; iteration < maximumIterations; iteration++)
        {
            for (int rootIndex = 0; rootIndex < degree; rootIndex++)
            {
                (double polynomialReal, double polynomialImaginary) = EvaluatePolynomialComplex(
                    normalizedCoefficients,
                    realRoots[rootIndex],
                    imaginaryRoots[rootIndex]);

                double denominatorReal = 1.0;
                double denominatorImaginary = 0.0;

                for (int otherIndex = 0; otherIndex < degree; otherIndex++)
                {
                    if (otherIndex == rootIndex)
                    {
                        continue;
                    }

                    double differenceReal = realRoots[rootIndex] - realRoots[otherIndex];
                    double differenceImaginary = imaginaryRoots[rootIndex] - imaginaryRoots[otherIndex];

                    double multipliedReal = (denominatorReal * differenceReal) - (denominatorImaginary * differenceImaginary);
                    double multipliedImaginary = (denominatorReal * differenceImaginary) + (denominatorImaginary * differenceReal);
                    denominatorReal = multipliedReal;
                    denominatorImaginary = multipliedImaginary;
                }

                double denominatorMagnitudeSquared = (denominatorReal * denominatorReal) + (denominatorImaginary * denominatorImaginary);
                if (denominatorMagnitudeSquared < 1e-18)
                {
                    continue;
                }

                double correctionReal = ((polynomialReal * denominatorReal) + (polynomialImaginary * denominatorImaginary)) / denominatorMagnitudeSquared;
                double correctionImaginary = ((polynomialImaginary * denominatorReal) - (polynomialReal * denominatorImaginary)) / denominatorMagnitudeSquared;

                realRoots[rootIndex] -= correctionReal;
                imaginaryRoots[rootIndex] -= correctionImaginary;
            }
        }

        List<ComplexEigenvalue> results = new List<ComplexEigenvalue>(degree);
        for (int rootIndex = 0; rootIndex < degree; rootIndex++)
        {
            double real = Math.Abs(realRoots[rootIndex]) < 1e-8 ? 0.0 : realRoots[rootIndex];
            double imaginary = Math.Abs(imaginaryRoots[rootIndex]) < 1e-8 ? 0.0 : imaginaryRoots[rootIndex];
            double magnitude = Math.Sqrt((real * real) + (imaginary * imaginary));
            results.Add(new ComplexEigenvalue(real, imaginary, magnitude));
        }

        return results;
    }

    private static (double Real, double Imaginary) EvaluatePolynomialComplex(
        double[] coefficients,
        double realValue,
        double imaginaryValue)
    {
        double accumulatedReal = coefficients[0];
        double accumulatedImaginary = 0.0;

        for (int index = 1; index < coefficients.Length; index++)
        {
            double nextReal = (accumulatedReal * realValue) - (accumulatedImaginary * imaginaryValue) + coefficients[index];
            double nextImaginary = (accumulatedReal * imaginaryValue) + (accumulatedImaginary * realValue);
            accumulatedReal = nextReal;
            accumulatedImaginary = nextImaginary;
        }

        return (accumulatedReal, accumulatedImaginary);
    }

    private static double CalculateLinearDetermination(double[] horizontalValues, double[] verticalValues)
    {
        int pointCount = horizontalValues.Length;
        double horizontalSum = 0.0;
        double verticalSum = 0.0;
        double horizontalVerticalSum = 0.0;
        double horizontalSquaredSum = 0.0;
        double verticalSquaredSum = 0.0;

        for (int index = 0; index < pointCount; index++)
        {
            double horizontal = horizontalValues[index];
            double vertical = verticalValues[index];
            horizontalSum += horizontal;
            verticalSum += vertical;
            horizontalVerticalSum += horizontal * vertical;
            horizontalSquaredSum += horizontal * horizontal;
            verticalSquaredSum += vertical * vertical;
        }

        double numerator = (pointCount * horizontalVerticalSum) - (horizontalSum * verticalSum);
        double denominatorHorizontal = (pointCount * horizontalSquaredSum) - (horizontalSum * horizontalSum);
        double denominatorVertical = (pointCount * verticalSquaredSum) - (verticalSum * verticalSum);

        if (denominatorHorizontal <= 1e-10 || denominatorVertical <= 1e-10)
        {
            return 1.0;
        }

        double correlation = numerator / Math.Sqrt(denominatorHorizontal * denominatorVertical);
        return Math.Clamp(correlation * correlation, 0.0, 1.0);
    }

    private static double CalculateExponentialDetermination(double[] horizontalValues, double[] verticalValues)
    {
        int pointCount = horizontalValues.Length;
        double[] logarithmicVerticalValues = new double[pointCount];
        for (int index = 0; index < pointCount; index++)
        {
            if (verticalValues[index] <= 0.0)
            {
                return 0.0;
            }
            logarithmicVerticalValues[index] = Math.Log(verticalValues[index]);
        }
        return CalculateLinearDetermination(horizontalValues, logarithmicVerticalValues);
    }

    private static double CalculateQuadraticDetermination(double[] horizontalValues, double[] verticalValues)
    {
        int pointCount = horizontalValues.Length;
        if (pointCount < 3)
        {
            return 0.0;
        }

        double horizontalSum = 0.0;
        double horizontalSquareSum = 0.0;
        double horizontalCubeSum = 0.0;
        double horizontalFourthSum = 0.0;
        double verticalSum = 0.0;
        double horizontalVerticalSum = 0.0;
        double horizontalSquareVerticalSum = 0.0;

        for (int index = 0; index < pointCount; index++)
        {
            double horizontal = horizontalValues[index];
            double vertical = verticalValues[index];
            double horizontalSquare = horizontal * horizontal;
            horizontalSum += horizontal;
            horizontalSquareSum += horizontalSquare;
            horizontalCubeSum += horizontalSquare * horizontal;
            horizontalFourthSum += horizontalSquare * horizontalSquare;
            verticalSum += vertical;
            horizontalVerticalSum += horizontal * vertical;
            horizontalSquareVerticalSum += horizontalSquare * vertical;
        }

        double verticalMean = verticalSum / pointCount;
        double totalSumOfSquares = 0.0;
        for (int index = 0; index < pointCount; index++)
        {
            double difference = verticalValues[index] - verticalMean;
            totalSumOfSquares += difference * difference;
        }

        if (totalSumOfSquares <= 1e-10)
        {
            return 1.0;
        }

        double determinant = (pointCount * ((horizontalSquareSum * horizontalFourthSum) - (horizontalCubeSum * horizontalCubeSum)))
            - (horizontalSum * ((horizontalSum * horizontalFourthSum) - (horizontalSquareSum * horizontalCubeSum)))
            + (horizontalSquareSum * ((horizontalSum * horizontalCubeSum) - (horizontalSquareSum * horizontalSquareSum)));

        if (Math.Abs(determinant) < 1e-12)
        {
            return 0.0;
        }

        double coefficientA = ((verticalSum * ((horizontalSquareSum * horizontalFourthSum) - (horizontalCubeSum * horizontalCubeSum)))
            - (horizontalSum * ((horizontalVerticalSum * horizontalFourthSum) - (horizontalSquareVerticalSum * horizontalCubeSum)))
            + (horizontalSquareSum * ((horizontalVerticalSum * horizontalCubeSum) - (horizontalSquareVerticalSum * horizontalSquareSum)))) / determinant;

        double coefficientB = ((pointCount * ((horizontalVerticalSum * horizontalFourthSum) - (horizontalSquareVerticalSum * horizontalCubeSum)))
            - (verticalSum * ((horizontalSum * horizontalFourthSum) - (horizontalSquareSum * horizontalCubeSum)))
            + (horizontalSquareSum * ((horizontalSum * horizontalSquareVerticalSum) - (horizontalSquareSum * horizontalVerticalSum)))) / determinant;

        double coefficientC = ((pointCount * ((horizontalSquareSum * horizontalSquareVerticalSum) - (horizontalCubeSum * horizontalVerticalSum)))
            - (horizontalSum * ((horizontalSum * horizontalSquareVerticalSum) - (horizontalSquareSum * horizontalVerticalSum)))
            + (verticalSum * ((horizontalSum * horizontalCubeSum) - (horizontalSquareSum * horizontalSquareSum)))) / determinant;

        double residualSumOfSquares = 0.0;
        for (int index = 0; index < pointCount; index++)
        {
            double horizontal = horizontalValues[index];
            double predicted = coefficientA + (coefficientB * horizontal) + (coefficientC * horizontal * horizontal);
            double error = verticalValues[index] - predicted;
            residualSumOfSquares += error * error;
        }

        double determinationCoefficient = 1.0 - (residualSumOfSquares / totalSumOfSquares);
        return Math.Clamp(determinationCoefficient, 0.0, 1.0);
    }

    private static bool CheckSigmoidalTrend(double[] verticalValues)
    {
        int pointCount = verticalValues.Length;
        if (pointCount < 4)
        {
            return false;
        }

        double[] increments = new double[pointCount - 1];
        for (int index = 0; index < pointCount - 1; index++)
        {
            increments[index] = verticalValues[index + 1] - verticalValues[index];
        }

        int maximumIncrementIndex = 0;
        double maximumIncrement = increments[0];
        for (int index = 1; index < increments.Length; index++)
        {
            if (increments[index] > maximumIncrement)
            {
                maximumIncrement = increments[index];
                maximumIncrementIndex = index;
            }
        }

        bool hasAcceleration = maximumIncrementIndex > 0;
        bool hasDeceleration = maximumIncrementIndex < increments.Length - 1 && increments[^1] < maximumIncrement * 0.7;
        return hasAcceleration && hasDeceleration;
    }
}