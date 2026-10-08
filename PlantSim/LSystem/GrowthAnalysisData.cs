using System.Collections.Generic;

namespace PlantSim.LSystem;

internal enum GrowthClassificationKind
{
    Constant,
    Linear,
    Polynomial,
    Exponential,
    Sigmoidal,
    Terminating,
    Unknown
}

internal sealed record ComplexEigenvalue(
    double RealPart,
    double ImaginaryPart,
    double Magnitude);

internal sealed record GrowthMatrixData(
    IReadOnlyList<string> AlphabetSymbols,
    double[,] MatrixElements,
    IReadOnlyList<ComplexEigenvalue> Eigenvalues,
    double SpectralRadius,
    int DominantMultiplicity);

internal sealed record GrowthStepRecord(
    int StepIndex,
    int TotalSymbolCount,
    int BranchSegmentCount,
    IReadOnlyDictionary<string, int> SymbolFrequencies);

internal sealed record GrowthAnalysisResult(
    IReadOnlyList<GrowthStepRecord> StepRecords,
    GrowthClassificationKind ClassificationKind,
    string SummaryDescription,
    double DeterminationCoefficient,
    int PolynomialDegree,
    GrowthMatrixData? ProductionMatrixData);