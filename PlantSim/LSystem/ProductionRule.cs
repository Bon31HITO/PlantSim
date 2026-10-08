using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal class ProductionRule : IProductionRule
{
    private static readonly IReadOnlyDictionary<string, double> EmptyParameters = new Dictionary<string, double>();

    public string PredecessorName { get; }
    public IReadOnlyList<string> FormalParameterNames { get; }
    public IReadOnlyList<ContextPatternElement> LeftContextPatterns { get; }
    public IReadOnlyList<ContextPatternElement> RightContextPatterns { get; }
    public IReadOnlyList<string> SuccessorSymbolNames { get; }
    public ExpressionNode? ConditionExpression { get; }
    public Func<IReadOnlyDictionary<string, double>, Word> SuccessorFactory { get; }
    public double Probability { get; }

    private readonly ExpressionEvaluator _evaluator;

    public ProductionRule(
        string predecessorName,
        IReadOnlyList<string> formalParameterNames,
        IReadOnlyList<ContextPatternElement> leftContextPatterns,
        IReadOnlyList<ContextPatternElement> rightContextPatterns,
        IReadOnlyList<string> successorSymbolNames,
        ExpressionNode? conditionExpression,
        Func<IReadOnlyDictionary<string, double>, Word> successorFactory,
        double probability = 1.0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(predecessorName);
        ArgumentNullException.ThrowIfNull(formalParameterNames);
        ArgumentNullException.ThrowIfNull(leftContextPatterns);
        ArgumentNullException.ThrowIfNull(rightContextPatterns);
        ArgumentNullException.ThrowIfNull(successorSymbolNames);
        ArgumentNullException.ThrowIfNull(successorFactory);

        PredecessorName = predecessorName;
        FormalParameterNames = formalParameterNames;
        LeftContextPatterns = leftContextPatterns;
        RightContextPatterns = rightContextPatterns;
        SuccessorSymbolNames = successorSymbolNames;
        ConditionExpression = conditionExpression;
        SuccessorFactory = successorFactory;
        Probability = Math.Clamp(probability, 0.0, 1.0);
        _evaluator = new ExpressionEvaluator();
    }

    public bool Matches(
        Word word,
        int targetIndex,
        IReadOnlySet<string> ignoredSymbols,
        IReadOnlyDictionary<string, double> globalConstants,
        out IReadOnlyDictionary<string, double> capturedParameters)
    {
        capturedParameters = EmptyParameters;
        Symbol currentSymbol = word[targetIndex];

        if (currentSymbol.Name != PredecessorName || currentSymbol.Parameters.Count != FormalParameterNames.Count)
        {
            return false;
        }

        Dictionary<string, double> parameterBindings = new Dictionary<string, double>(globalConstants);
        for (int parameterIndex = 0; parameterIndex < FormalParameterNames.Count; parameterIndex++)
        {
            parameterBindings[FormalParameterNames[parameterIndex]] = currentSymbol.Parameters[parameterIndex];
        }

        if (LeftContextPatterns.Count > 0 && !MatchLeftContextStrict(word, targetIndex, ignoredSymbols, parameterBindings))
        {
            return false;
        }

        if (RightContextPatterns.Count > 0 && !MatchRightContextSubtree(word, targetIndex + 1, RightContextPatterns, ignoredSymbols, parameterBindings))
        {
            return false;
        }

        if (ConditionExpression != null)
        {
            double evaluationResult = _evaluator.Evaluate(ConditionExpression, parameterBindings);
            if (Math.Abs(evaluationResult) < 1e-10)
            {
                return false;
            }
        }

        capturedParameters = parameterBindings;
        return true;
    }

    public Word ProduceSuccessor(IReadOnlyDictionary<string, double> parameterBindings)
    {
        return SuccessorFactory(parameterBindings);
    }

    private bool MatchLeftContextStrict(
        Word word,
        int targetIndex,
        IReadOnlySet<string> ignoredSymbols,
        Dictionary<string, double> parameterBindings)
    {
        int patternIndex = LeftContextPatterns.Count - 1;
        int searchIndex = targetIndex - 1;

        while (patternIndex >= 0 && searchIndex >= 0)
        {
            ContextPatternElement patternElement = LeftContextPatterns[patternIndex];

            if (patternElement is ContextBranchPattern branchPattern)
            {
                int closeBracketIndex = SkipIgnoredBackward(word, searchIndex, ignoredSymbols);
                if (closeBracketIndex < 0 || word[closeBracketIndex].Name != "]")
                {
                    return false;
                }

                int openBracketIndex = FindMatchingOpenBracketBackward(word, closeBracketIndex);
                if (openBracketIndex < 0)
                {
                    return false;
                }

                if (!MatchBranchContentExact(word, openBracketIndex + 1, closeBracketIndex - 1, branchPattern.Elements, ignoredSymbols, parameterBindings))
                {
                    return false;
                }

                searchIndex = openBracketIndex - 1;
                patternIndex--;
                continue;
            }

            if (patternElement is ContextSymbolPattern symbolPattern)
            {
                bool matchedSymbol = false;
                while (searchIndex >= 0)
                {
                    Symbol candidateSymbol = word[searchIndex];

                    if (candidateSymbol.Name == "]")
                    {
                        int openBracketIndex = FindMatchingOpenBracketBackward(word, searchIndex);
                        if (openBracketIndex < 0)
                        {
                            return false;
                        }
                        searchIndex = openBracketIndex - 1;
                        continue;
                    }

                    if (candidateSymbol.Name == "[")
                    {
                        searchIndex--;
                        continue;
                    }

                    if (ignoredSymbols.Contains(candidateSymbol.Name))
                    {
                        searchIndex--;
                        continue;
                    }

                    if (candidateSymbol.Name == symbolPattern.SymbolName && candidateSymbol.Parameters.Count == symbolPattern.FormalParameterNames.Count)
                    {
                        for (int argumentIndex = 0; argumentIndex < symbolPattern.FormalParameterNames.Count; argumentIndex++)
                        {
                            parameterBindings[symbolPattern.FormalParameterNames[argumentIndex]] = candidateSymbol.Parameters[argumentIndex];
                        }

                        searchIndex--;
                        patternIndex--;
                        matchedSymbol = true;
                        break;
                    }

                    return false;
                }

                if (!matchedSymbol)
                {
                    return false;
                }
            }
        }

        return patternIndex < 0;
    }

    private bool MatchRightContextSubtree(
        Word word,
        int searchIndex,
        IReadOnlyList<ContextPatternElement> patterns,
        IReadOnlySet<string> ignoredSymbols,
        Dictionary<string, double> parameterBindings)
    {
        return MatchRightContextSubtreeRecursive(word, searchIndex, patterns, 0, ignoredSymbols, parameterBindings);
    }

    private bool MatchRightContextSubtreeRecursive(
        Word word,
        int searchIndex,
        IReadOnlyList<ContextPatternElement> patterns,
        int patternIndex,
        IReadOnlySet<string> ignoredSymbols,
        Dictionary<string, double> parameterBindings)
    {
        if (patternIndex >= patterns.Count)
        {
            return true;
        }

        searchIndex = SkipIgnoredForward(word, searchIndex, ignoredSymbols);
        if (searchIndex >= word.Count || word[searchIndex].Name == "]")
        {
            return false;
        }

        List<ContextBranchPattern> consecutiveBranchPatterns = [];
        int scanPatternIndex = patternIndex;
        while (scanPatternIndex < patterns.Count && patterns[scanPatternIndex] is ContextBranchPattern branchPattern)
        {
            consecutiveBranchPatterns.Add(branchPattern);
            scanPatternIndex++;
        }

        if (consecutiveBranchPatterns.Count > 0)
        {
            List<(int ContentStart, int ContentEnd, int CloseBracketIndex)> availableWordBranches = CollectDirectSiblingBranches(word, searchIndex);
            if (availableWordBranches.Count < consecutiveBranchPatterns.Count)
            {
                return false;
            }

            int mainStemStartIndex = searchIndex;
            if (availableWordBranches.Count > 0)
            {
                mainStemStartIndex = availableWordBranches[^1].CloseBracketIndex + 1;
            }

            return TryPermuteBranchMatches(
                word,
                availableWordBranches,
                consecutiveBranchPatterns,
                0,
                new bool[availableWordBranches.Count],
                ignoredSymbols,
                parameterBindings,
                () =>
                {
                    if (scanPatternIndex >= patterns.Count)
                    {
                        return true;
                    }

                    return MatchRightContextSubtreeRecursive(
                        word,
                        mainStemStartIndex,
                        patterns,
                        scanPatternIndex,
                        ignoredSymbols,
                        parameterBindings);
                });
        }

        ContextSymbolPattern currentSymbolPattern = (ContextSymbolPattern)patterns[patternIndex];
        int candidateIndex = searchIndex;

        while (candidateIndex < word.Count)
        {
            Symbol candidateSymbol = word[candidateIndex];

            if (ignoredSymbols.Contains(candidateSymbol.Name))
            {
                candidateIndex++;
                continue;
            }

            if (candidateSymbol.Name == "[")
            {
                int matchingClose = FindMatchingCloseBracketForward(word, candidateIndex);
                if (matchingClose < 0)
                {
                    return false;
                }
                candidateIndex = matchingClose + 1;
                continue;
            }

            if (candidateSymbol.Name == "]")
            {
                return false;
            }

            if (candidateSymbol.Name == currentSymbolPattern.SymbolName &&
                candidateSymbol.Parameters.Count == currentSymbolPattern.FormalParameterNames.Count)
            {
                Dictionary<string, double> trialBindings = new Dictionary<string, double>(parameterBindings);
                for (int argumentIndex = 0; argumentIndex < currentSymbolPattern.FormalParameterNames.Count; argumentIndex++)
                {
                    trialBindings[currentSymbolPattern.FormalParameterNames[argumentIndex]] = candidateSymbol.Parameters[argumentIndex];
                }

                if (MatchRightContextSubtreeRecursive(word, candidateIndex + 1, patterns, patternIndex + 1, ignoredSymbols, trialBindings))
                {
                    foreach (KeyValuePair<string, double> pair in trialBindings)
                    {
                        parameterBindings[pair.Key] = pair.Value;
                    }
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    private bool TryPermuteBranchMatches(
        Word word,
        IReadOnlyList<(int ContentStart, int ContentEnd, int CloseBracketIndex)> availableWordBranches,
        IReadOnlyList<ContextBranchPattern> branchPatterns,
        int patternSubIndex,
        bool[] usedBranchMarkers,
        IReadOnlySet<string> ignoredSymbols,
        Dictionary<string, double> parameterBindings,
        Func<bool> continuationPredicate)
    {
        if (patternSubIndex >= branchPatterns.Count)
        {
            return continuationPredicate();
        }

        ContextBranchPattern currentBranchPattern = branchPatterns[patternSubIndex];

        for (int branchCandidateIndex = 0; branchCandidateIndex < availableWordBranches.Count; branchCandidateIndex++)
        {
            if (usedBranchMarkers[branchCandidateIndex])
            {
                continue;
            }

            (int contentStart, int contentEnd, _) = availableWordBranches[branchCandidateIndex];
            Dictionary<string, double> trialBindings = new Dictionary<string, double>(parameterBindings);

            if (MatchBranchContentExact(word, contentStart, contentEnd, currentBranchPattern.Elements, ignoredSymbols, trialBindings))
            {
                usedBranchMarkers[branchCandidateIndex] = true;

                if (TryPermuteBranchMatches(
                    word,
                    availableWordBranches,
                    branchPatterns,
                    patternSubIndex + 1,
                    usedBranchMarkers,
                    ignoredSymbols,
                    trialBindings,
                    continuationPredicate))
                {
                    foreach (KeyValuePair<string, double> pair in trialBindings)
                    {
                        parameterBindings[pair.Key] = pair.Value;
                    }
                    return true;
                }

                usedBranchMarkers[branchCandidateIndex] = false;
            }
        }

        return false;
    }

    private static List<(int ContentStart, int ContentEnd, int CloseBracketIndex)> CollectDirectSiblingBranches(Word word, int searchIndex)
    {
        List<(int ContentStart, int ContentEnd, int CloseBracketIndex)> branchList = [];
        int index = searchIndex;

        while (index < word.Count)
        {
            Symbol symbol = word[index];

            if (symbol.Name == "[")
            {
                int closeBracket = FindMatchingCloseBracketForward(word, index);
                if (closeBracket > index)
                {
                    branchList.Add((index + 1, closeBracket - 1, closeBracket));
                    index = closeBracket + 1;
                    continue;
                }
            }

            if (symbol.Name == "]")
            {
                break;
            }

            if (symbol.Name != "[" && !char.IsWhiteSpace(symbol.Name[0]))
            {
                break;
            }

            index++;
        }

        return branchList;
    }

    private static bool MatchBranchContentExact(
        Word word,
        int startIndex,
        int endIndex,
        IReadOnlyList<ContextPatternElement> patterns,
        IReadOnlySet<string> ignoredSymbols,
        Dictionary<string, double> parameterBindings)
    {
        int patternIndex = 0;
        int searchIndex = startIndex;

        while (searchIndex <= endIndex && patternIndex < patterns.Count)
        {
            Symbol candidateSymbol = word[searchIndex];

            if (ignoredSymbols.Contains(candidateSymbol.Name))
            {
                searchIndex++;
                continue;
            }

            ContextPatternElement currentPattern = patterns[patternIndex];

            if (currentPattern is ContextSymbolPattern symbolPattern)
            {
                if (candidateSymbol.Name == symbolPattern.SymbolName && candidateSymbol.Parameters.Count == symbolPattern.FormalParameterNames.Count)
                {
                    for (int argumentIndex = 0; argumentIndex < symbolPattern.FormalParameterNames.Count; argumentIndex++)
                    {
                        parameterBindings[symbolPattern.FormalParameterNames[argumentIndex]] = candidateSymbol.Parameters[argumentIndex];
                    }

                    searchIndex++;
                    patternIndex++;
                    continue;
                }

                return false;
            }

            if (currentPattern is ContextBranchPattern nestedPattern)
            {
                if (candidateSymbol.Name == "[")
                {
                    int closeBracketIndex = FindMatchingCloseBracketForward(word, searchIndex);
                    if (closeBracketIndex >= 0 && closeBracketIndex <= endIndex)
                    {
                        if (MatchBranchContentExact(word, searchIndex + 1, closeBracketIndex - 1, nestedPattern.Elements, ignoredSymbols, parameterBindings))
                        {
                            searchIndex = closeBracketIndex + 1;
                            patternIndex++;
                            continue;
                        }
                    }
                }

                return false;
            }
        }

        while (searchIndex <= endIndex && ignoredSymbols.Contains(word[searchIndex].Name))
        {
            searchIndex++;
        }

        return patternIndex >= patterns.Count && searchIndex > endIndex;
    }

    private static int SkipIgnoredBackward(Word word, int index, IReadOnlySet<string> ignoredSymbols)
    {
        while (index >= 0 && ignoredSymbols.Contains(word[index].Name))
        {
            index--;
        }
        return index;
    }

    private static int SkipIgnoredForward(Word word, int index, IReadOnlySet<string> ignoredSymbols)
    {
        while (index < word.Count && ignoredSymbols.Contains(word[index].Name))
        {
            index++;
        }
        return index;
    }

    private static int FindMatchingOpenBracketBackward(Word word, int closeBracketIndex)
    {
        int depth = 0;
        for (int index = closeBracketIndex; index >= 0; index--)
        {
            if (word[index].Name == "]")
            {
                depth++;
            }
            else if (word[index].Name == "[")
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }
        }
        return -1;
    }

    private static int FindMatchingCloseBracketForward(Word word, int openBracketIndex)
    {
        int depth = 0;
        for (int index = openBracketIndex; index < word.Count; index++)
        {
            if (word[index].Name == "[")
            {
                depth++;
            }
            else if (word[index].Name == "]")
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }
        }
        return -1;
    }
}