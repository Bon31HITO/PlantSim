using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

public class LindenmayerGrammar
{
    public Word Axiom { get; }
    public IReadOnlyList<IProductionRule> Rules { get; }
    public IReadOnlyList<IProductionRule> InterpretationRules { get; }
    public IReadOnlySet<string> IgnoredSymbols { get; }
    public IReadOnlyDictionary<string, double> Constants { get; }

    public LindenmayerGrammar(
        Word axiom,
        IReadOnlyList<IProductionRule> rules,
        IReadOnlyList<IProductionRule>? interpretationRules = null,
        IReadOnlySet<string>? ignoredSymbols = null,
        IReadOnlyDictionary<string, double>? constants = null)
    {
        ArgumentNullException.ThrowIfNull(axiom);
        ArgumentNullException.ThrowIfNull(rules);

        Axiom = axiom;
        Rules = rules;
        InterpretationRules = interpretationRules ?? Array.Empty<IProductionRule>();
        IgnoredSymbols = ignoredSymbols ?? new HashSet<string>();
        Constants = constants ?? new Dictionary<string, double>();
    }
}