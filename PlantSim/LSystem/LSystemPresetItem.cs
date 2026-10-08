using System;

namespace PlantSim.LSystem;

public class LSystemPresetItem
{
    public string Title { get; }
    public string SectionReference { get; }
    public string Description { get; }
    public int RecommendedStepCount { get; }
    public double DefaultAngleInDegrees { get; }
    public double DefaultStepLength { get; }
    public string AxiomText { get; }
    public string RulesText { get; }
    public string InterpretationRulesText { get; }
    public string IgnoredSymbolsText { get; }
    public Func<LindenmayerGrammar> GrammarFactory { get; }
    public double DefaultTropismSusceptibility { get; }
    public VectorThreeDimensional DefaultTropismVector { get; }
    public ColorPaletteTable? CustomPalette { get; }

    public LSystemPresetItem(
        string title,
        string sectionReference,
        string description,
        int recommendedStepCount,
        double defaultAngleInDegrees,
        double defaultStepLength,
        string axiomText,
        string rulesText,
        string ignoredSymbolsText,
        Func<LindenmayerGrammar> grammarFactory,
        string interpretationRulesText = "",
        double defaultTropismSusceptibility = 0.0,
        VectorThreeDimensional? defaultTropismVector = null,
        ColorPaletteTable? customPalette = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(axiomText);
        ArgumentNullException.ThrowIfNull(rulesText);
        ArgumentNullException.ThrowIfNull(ignoredSymbolsText);
        ArgumentNullException.ThrowIfNull(grammarFactory);
        ArgumentNullException.ThrowIfNull(interpretationRulesText);

        Title = title;
        SectionReference = sectionReference;
        Description = description;
        RecommendedStepCount = recommendedStepCount;
        DefaultAngleInDegrees = defaultAngleInDegrees;
        DefaultStepLength = defaultStepLength;
        AxiomText = axiomText;
        RulesText = rulesText;
        InterpretationRulesText = interpretationRulesText;
        IgnoredSymbolsText = ignoredSymbolsText;
        GrammarFactory = grammarFactory;
        DefaultTropismSusceptibility = defaultTropismSusceptibility;
        DefaultTropismVector = defaultTropismVector ?? VectorThreeDimensional.Zero;
        CustomPalette = customPalette;
    }

    public override string ToString()
    {
        return $"{SectionReference} - {Title}";
    }
}