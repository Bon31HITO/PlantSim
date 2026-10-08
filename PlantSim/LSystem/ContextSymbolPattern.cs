using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal sealed record ContextSymbolPattern : ContextPatternElement
{
    public string SymbolName { get; }
    public IReadOnlyList<string> FormalParameterNames { get; }

    public ContextSymbolPattern(string symbolName, IReadOnlyList<string> formalParameterNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbolName);
        ArgumentNullException.ThrowIfNull(formalParameterNames);
        SymbolName = symbolName;
        FormalParameterNames = formalParameterNames;
    }

    public ContextSymbolPattern(string symbolName)
        : this(symbolName, Array.Empty<string>())
    {
    }
}
