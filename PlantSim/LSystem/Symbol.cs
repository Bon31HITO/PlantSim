using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

public readonly record struct Symbol
{
    private static readonly double[] EmptyParameters = [];

    public string Name { get; }
    public IReadOnlyList<double> Parameters { get; }

    public Symbol(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Parameters = EmptyParameters;
    }

    public Symbol(string name, IReadOnlyList<double> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parameters);
        Name = name;
        Parameters = parameters;
    }

    public Symbol(string name, params double[] parameters)
        : this(name, (IReadOnlyList<double>)parameters)
    {
    }

    public override string ToString()
    {
        return Parameters.Count == 0 ? Name : $"{Name}({string.Join(", ", Parameters)})";
    }
}