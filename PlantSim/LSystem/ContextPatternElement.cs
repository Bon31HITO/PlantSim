using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal abstract record ContextPatternElement;

internal sealed record ContextBranchPattern : ContextPatternElement
{
    public IReadOnlyList<ContextPatternElement> Elements { get; }

    public ContextBranchPattern(IReadOnlyList<ContextPatternElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        Elements = elements;
    }
}