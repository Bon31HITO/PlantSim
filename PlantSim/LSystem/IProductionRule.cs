using System.Collections.Generic;

namespace PlantSim.LSystem;

public interface IProductionRule
{
    string PredecessorName { get; }
    IReadOnlyList<string> SuccessorSymbolNames { get; }
    double Probability { get; }

    bool Matches(
        Word word,
        int targetIndex,
        IReadOnlySet<string> ignoredSymbols,
        IReadOnlyDictionary<string, double> globalConstants,
        out IReadOnlyDictionary<string, double> capturedParameters);

    Word ProduceSuccessor(IReadOnlyDictionary<string, double> parameterBindings);
}