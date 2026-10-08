using System;
using System.Collections.Generic;
using System.Globalization;

namespace PlantSim.LSystem;

internal class LSystemGrammarParser
{
    private readonly ExpressionParser _expressionParser;
    private readonly ExpressionEvaluator _expressionEvaluator;

    public LSystemGrammarParser()
    {
        _expressionParser = new ExpressionParser();
        _expressionEvaluator = new ExpressionEvaluator();
    }

    public LindenmayerGrammar ParseGrammar(
        string axiomText,
        string rulesText,
        string ignoredSymbolsText,
        string interpretationRulesText = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(axiomText);
        ArgumentNullException.ThrowIfNull(rulesText);
        ArgumentNullException.ThrowIfNull(ignoredSymbolsText);

        Dictionary<string, double> constants = [];
        List<string> productionRuleLines = [];
        List<string> interpretationRuleLines = [];
        HashSet<string> ignoredSymbols = [];

        string[] initialIgnored = ignoredSymbolsText.Split([' ', '\t', ';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int index = 0; index < initialIgnored.Length; index++)
        {
            ignoredSymbols.Add(initialIgnored[index]);
        }

        ParseRuleSection(rulesText, constants, ignoredSymbols, productionRuleLines, interpretationRuleLines);
        if (!string.IsNullOrWhiteSpace(interpretationRulesText))
        {
            ParseRuleSection(interpretationRulesText, constants, ignoredSymbols, interpretationRuleLines, interpretationRuleLines);
        }

        Word axiom = Word.Parse(axiomText, constants);

        List<IProductionRule> productionRules = [];
        for (int index = 0; index < productionRuleLines.Count; index++)
        {
            productionRules.Add(ParseRule(productionRuleLines[index], constants));
        }

        List<IProductionRule> interpretationRules = [];
        for (int index = 0; index < interpretationRuleLines.Count; index++)
        {
            interpretationRules.Add(ParseRule(interpretationRuleLines[index], constants));
        }

        return new LindenmayerGrammar(axiom, productionRules, interpretationRules, ignoredSymbols, constants);
    }

    private void ParseRuleSection(
        string text,
        Dictionary<string, double> constants,
        HashSet<string> ignoredSymbols,
        List<string> destinationRules,
        List<string> destinationInterpretationRules)
    {
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        bool insideInterpretationBlock = false;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (line.StartsWith('#') || line.StartsWith("//"))
            {
                if (line.StartsWith("#define", StringComparison.OrdinalIgnoreCase))
                {
                    ParseDefineLine(line, constants);
                }
                else if (line.StartsWith("#ignore", StringComparison.OrdinalIgnoreCase))
                {
                    ParseIgnoreLine(line, ignoredSymbols);
                }
                continue;
            }

            if (line.StartsWith("homomorphism:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("interpretation:", StringComparison.OrdinalIgnoreCase))
            {
                insideInterpretationBlock = true;
                continue;
            }

            if (line.StartsWith("production:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("rules:", StringComparison.OrdinalIgnoreCase))
            {
                insideInterpretationBlock = false;
                continue;
            }

            if (line.StartsWith("define:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
            {
                ParseDefineLine(line, constants);
                continue;
            }

            if (line.StartsWith("ignore:", StringComparison.OrdinalIgnoreCase))
            {
                ParseIgnoreLine(line, ignoredSymbols);
                continue;
            }

            if (insideInterpretationBlock)
            {
                destinationInterpretationRules.Add(line);
            }
            else
            {
                destinationRules.Add(line);
            }
        }
    }

    private void ParseDefineLine(string line, Dictionary<string, double> constants)
    {
        string trimmed = line;
        if (trimmed.StartsWith('#')) trimmed = trimmed[1..].Trim();
        if (trimmed.StartsWith("define", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[6..].TrimStart(':', ' ', '\t');
        }

        string[] parts = trimmed.Split(['=', ' ', '\t'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            string name = parts[0];
            string expressionText = parts[1];
            try
            {
                ExpressionNode node = _expressionParser.Parse(expressionText);
                double evaluatedValue = _expressionEvaluator.Evaluate(node, constants);
                constants[name] = evaluatedValue;
            }
            catch
            {
                if (double.TryParse(expressionText, CultureInfo.InvariantCulture, out double parsedValue))
                {
                    constants[name] = parsedValue;
                }
            }
        }
    }

    private static void ParseIgnoreLine(string line, HashSet<string> ignored)
    {
        string trimmed = line;
        if (trimmed.StartsWith('#')) trimmed = trimmed[1..].Trim();
        if (trimmed.StartsWith("ignore", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[6..].TrimStart(':', ' ', '\t');
        }

        string[] tokens = trimmed.Split([' ', '\t', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int index = 0; index < tokens.Length; index++)
        {
            ignored.Add(tokens[index]);
        }
    }

    public ProductionRule ParseRule(string ruleText, IReadOnlyDictionary<string, double> constants)
    {
        string[] arrowParts = ruleText.Split(["->", "-->"], StringSplitOptions.TrimEntries);
        if (arrowParts.Length != 2)
        {
            throw new FormatException($"Invalid rule syntax (missing '->'): {ruleText}");
        }

        string leftSide = arrowParts[0];
        string successorText = arrowParts[1];

        double probability = 1.0;
        string? conditionText = null;

        string[] colonParts = leftSide.Split(':', StringSplitOptions.TrimEntries);
        string contextAndPredecessor = colonParts[0];

        if (colonParts.Length == 2)
        {
            if (double.TryParse(colonParts[1], CultureInfo.InvariantCulture, out double parsedProbability))
            {
                probability = parsedProbability;
            }
            else
            {
                conditionText = colonParts[1];
            }
        }
        else if (colonParts.Length >= 3)
        {
            conditionText = colonParts[1];
            if (double.TryParse(colonParts[2], CultureInfo.InvariantCulture, out double parsedProbability))
            {
                probability = parsedProbability;
            }
        }

        ExpressionNode? conditionExpression = !string.IsNullOrWhiteSpace(conditionText)
            ? _expressionParser.Parse(conditionText)
            : null;

        int leftAngle = -1;
        int rightAngle = -1;
        int depth = 0;

        for (int index = 0; index < contextAndPredecessor.Length; index++)
        {
            char current = contextAndPredecessor[index];
            if (current == '(') depth++;
            else if (current == ')') depth--;
            else if (depth == 0)
            {
                if (current == '<' && leftAngle == -1) leftAngle = index;
                else if (current == '>') rightAngle = index;
            }
        }

        string leftContext = string.Empty;
        string rightContext = string.Empty;
        string predecessorPart;

        if (leftAngle >= 0 && rightAngle >= 0 && leftAngle < rightAngle)
        {
            leftContext = contextAndPredecessor[..leftAngle].Trim();
            predecessorPart = contextAndPredecessor.Substring(leftAngle + 1, rightAngle - leftAngle - 1).Trim();
            rightContext = contextAndPredecessor[(rightAngle + 1)..].Trim();
        }
        else if (leftAngle >= 0)
        {
            leftContext = contextAndPredecessor[..leftAngle].Trim();
            predecessorPart = contextAndPredecessor[(leftAngle + 1)..].Trim();
        }
        else if (rightAngle >= 0)
        {
            predecessorPart = contextAndPredecessor[..rightAngle].Trim();
            rightContext = contextAndPredecessor[(rightAngle + 1)..].Trim();
        }
        else
        {
            predecessorPart = contextAndPredecessor.Trim();
        }

        ContextSymbolPattern strictPredecessor = ParseContextSymbol(predecessorPart);
        IReadOnlyList<ContextPatternElement> leftPatterns = string.IsNullOrWhiteSpace(leftContext) ? [] : ParsePatternElements(leftContext);
        IReadOnlyList<ContextPatternElement> rightPatterns = string.IsNullOrWhiteSpace(rightContext) ? [] : ParsePatternElements(rightContext);
        IReadOnlyList<ParametricSuccessorPattern> successorPatterns = ParseSuccessorPatterns(successorText);

        List<string> successorSymbolNames = new List<string>(successorPatterns.Count);
        for (int patternIndex = 0; patternIndex < successorPatterns.Count; patternIndex++)
        {
            successorSymbolNames.Add(successorPatterns[patternIndex].SymbolName);
        }

        return new ProductionRule(
            strictPredecessor.SymbolName,
            strictPredecessor.FormalParameterNames,
            leftPatterns,
            rightPatterns,
            successorSymbolNames,
            conditionExpression,
            bindings =>
            {
                List<Symbol> symbols = new List<Symbol>(successorPatterns.Count);
                for (int patternIndex = 0; patternIndex < successorPatterns.Count; patternIndex++)
                {
                    ParametricSuccessorPattern pattern = successorPatterns[patternIndex];
                    if (pattern.ArgumentExpressions.Count == 0)
                    {
                        symbols.Add(new Symbol(pattern.SymbolName));
                    }
                    else
                    {
                        double[] evaluatedArguments = new double[pattern.ArgumentExpressions.Count];
                        for (int argumentIndex = 0; argumentIndex < pattern.ArgumentExpressions.Count; argumentIndex++)
                        {
                            evaluatedArguments[argumentIndex] = _expressionEvaluator.Evaluate(pattern.ArgumentExpressions[argumentIndex], bindings);
                        }
                        symbols.Add(new Symbol(pattern.SymbolName, evaluatedArguments));
                    }
                }
                return new Word(symbols);
            },
            probability);
    }

    private static IReadOnlyList<ContextPatternElement> ParsePatternElements(string text)
    {
        int position = 0;
        return ParsePatternElementsRecursive(text, ref position);
    }

    private static IReadOnlyList<ContextPatternElement> ParsePatternElementsRecursive(string text, ref int position)
    {
        List<ContextPatternElement> elements = [];
        while (position < text.Length)
        {
            char current = text[position];
            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            if (current == '[')
            {
                position++;
                elements.Add(new ContextBranchPattern(ParsePatternElementsRecursive(text, ref position)));
                continue;
            }

            if (current == ']')
            {
                position++;
                break;
            }

            string symbolName = ExtractSymbolName(text, ref position);
            List<string> formalParameters = [];

            if (position < text.Length && text[position] == '(')
            {
                position++;
                int argumentStart = position;
                int depth = 1;
                while (position < text.Length && depth > 0)
                {
                    if (text[position] == '(') depth++;
                    else if (text[position] == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            string argumentText = text[argumentStart..position].Trim();
                            if (argumentText.Length > 0)
                            {
                                formalParameters.AddRange(argumentText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                            }
                            break;
                        }
                    }
                    position++;
                }
                if (position < text.Length && text[position] == ')')
                {
                    position++;
                }
            }

            elements.Add(new ContextSymbolPattern(symbolName, formalParameters));
        }
        return elements;
    }

    private static ContextSymbolPattern ParseContextSymbol(string text)
    {
        string trimmed = text.Trim();
        int openIndex = trimmed.IndexOf('(');
        if (openIndex < 0) return new ContextSymbolPattern(trimmed);

        string name = trimmed[..openIndex].Trim();
        int closeIndex = trimmed.LastIndexOf(')');
        string argumentsInside = trimmed.Substring(openIndex + 1, closeIndex - openIndex - 1);
        string[] parameterNames = argumentsInside.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return new ContextSymbolPattern(name, parameterNames);
    }

    private IReadOnlyList<ParametricSuccessorPattern> ParseSuccessorPatterns(string text)
    {
        List<ParametricSuccessorPattern> patterns = [];
        int position = 0;

        while (position < text.Length)
        {
            char current = text[position];
            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            string symbolName = ExtractSymbolName(text, ref position);
            List<ExpressionNode> expressions = [];

            if (position < text.Length && text[position] == '(')
            {
                position++;
                int depth = 1;
                int argumentStart = position;
                while (position < text.Length && depth > 0)
                {
                    if (text[position] == '(') depth++;
                    else if (text[position] == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            string argumentText = text[argumentStart..position].Trim();
                            if (argumentText.Length > 0)
                            {
                                expressions.Add(_expressionParser.Parse(argumentText));
                            }
                            break;
                        }
                    }
                    else if (text[position] == ',' && depth == 1)
                    {
                        string argumentText = text[argumentStart..position].Trim();
                        expressions.Add(_expressionParser.Parse(argumentText));
                        argumentStart = position + 1;
                    }
                    position++;
                }
                if (position < text.Length && text[position] == ')')
                {
                    position++;
                }
            }
            else if (symbolName is "'" or "," or "!" or ";" or "@" or "~" && position < text.Length && (char.IsDigit(text[position]) || text[position] == '.'))
            {
                int numberStart = position;
                bool hasDot = false;
                while (position < text.Length && (char.IsDigit(text[position]) || (text[position] == '.' && !hasDot)))
                {
                    if (text[position] == '.') hasDot = true;
                    position++;
                }
                string literal = text[numberStart..position];
                expressions.Add(_expressionParser.Parse(literal));
            }

            patterns.Add(new ParametricSuccessorPattern(symbolName, expressions));
        }

        return patterns;
    }

    private static string ExtractSymbolName(string text, ref int position)
    {
        char current = text[position];
        if (char.IsLetter(current))
        {
            int start = position;
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_'))
            {
                position++;
            }
            int length = position - start;
            if (length > 1)
            {
                if (position < text.Length && text[position] == '(')
                {
                    return text[start..position];
                }
                position = start + 1;
                return current.ToString();
            }
            return current.ToString();
        }

        position++;
        return current.ToString();
    }

    private sealed record ParametricSuccessorPattern(string SymbolName, IReadOnlyList<ExpressionNode> ArgumentExpressions);
}