using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlantSim.LSystem;

internal class ParallelRewritingEngine
{
    private const int ParallelThresholdSymbolCount = 1024;
    private static readonly IReadOnlyDictionary<string, double> EmptyParameters = new Dictionary<string, double>();
    private readonly GrowthFunctionAnalyzer _analyzer = new();

    public Word RewriteStep(LindenmayerGrammar grammar, Word currentWord, int stepIndex, int effectiveSeed)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(currentWord);

        if (currentWord.Count == 0)
        {
            return currentWord;
        }

        Dictionary<string, double> contextBindings = new Dictionary<string, double>(grammar.Constants)
        {
            ["n"] = stepIndex
        };

        Dictionary<string, List<IProductionRule>> ruleMap = BuildRuleMap(grammar.Rules);
        Word rewrittenWord;

        if (currentWord.Count < ParallelThresholdSymbolCount)
        {
            List<Symbol> results = new List<Symbol>(currentWord.Count * 2);
            RewriteRange(grammar, currentWord, 0, currentWord.Count, ruleMap, contextBindings, stepIndex, effectiveSeed, results);
            rewrittenWord = new Word(results);
        }
        else
        {
            int processorCount = Environment.ProcessorCount;
            int chunkSize = (currentWord.Count + processorCount - 1) / processorCount;
            List<Symbol>[] partitions = new List<Symbol>[processorCount];

            Parallel.For(0, processorCount, partitionIndex =>
            {
                int startIndex = partitionIndex * chunkSize;
                int endIndex = Math.Min(startIndex + chunkSize, currentWord.Count);

                if (startIndex >= currentWord.Count)
                {
                    partitions[partitionIndex] = [];
                    return;
                }

                List<Symbol> localResults = new List<Symbol>((endIndex - startIndex) * 2);
                RewriteRange(grammar, currentWord, startIndex, endIndex, ruleMap, contextBindings, stepIndex, effectiveSeed, localResults);
                partitions[partitionIndex] = localResults;
            });

            int totalSymbols = 0;
            for (int partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
            {
                totalSymbols += partitions[partitionIndex].Count;
            }

            List<Symbol> combinedResults = new List<Symbol>(totalSymbols);
            for (int partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
            {
                combinedResults.AddRange(partitions[partitionIndex]);
            }

            rewrittenWord = new Word(combinedResults);
        }

        return PruneCutSymbols(rewrittenWord);
    }

    public Word ApplyInterpretationHomomorphism(LindenmayerGrammar grammar, Word derivedWord)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(derivedWord);

        if (grammar.InterpretationRules.Count == 0 || derivedWord.Count == 0)
        {
            return derivedWord;
        }

        Dictionary<string, List<IProductionRule>> interpretationMap = BuildRuleMap(grammar.InterpretationRules);
        List<Symbol> mappedSymbols = new List<Symbol>(derivedWord.Count * 2);
        Dictionary<string, double> contextBindings = new Dictionary<string, double>(grammar.Constants);

        for (int index = 0; index < derivedWord.Count; index++)
        {
            Symbol symbol = derivedWord[index];
            if (interpretationMap.TryGetValue(symbol.Name, out List<IProductionRule>? candidates))
            {
                IProductionRule? matchingRule = null;
                IReadOnlyDictionary<string, double> captured = EmptyParameters;

                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    IProductionRule candidate = candidates[candidateIndex];
                    if (candidate.Matches(derivedWord, index, grammar.IgnoredSymbols, contextBindings, out IReadOnlyDictionary<string, double> parameters))
                    {
                        matchingRule = candidate;
                        captured = parameters;
                        break;
                    }
                }

                if (matchingRule != null)
                {
                    Word successor = matchingRule.ProduceSuccessor(captured);
                    for (int successorIndex = 0; successorIndex < successor.Count; successorIndex++)
                    {
                        mappedSymbols.Add(successor[successorIndex]);
                    }
                    continue;
                }
            }

            mappedSymbols.Add(symbol);
        }

        return new Word(mappedSymbols);
    }

    private static void RewriteRange(
        LindenmayerGrammar grammar,
        Word sourceWord,
        int startIndex,
        int endIndex,
        Dictionary<string, List<IProductionRule>> ruleMap,
        IReadOnlyDictionary<string, double> contextBindings,
        int stepIndex,
        int effectiveSeed,
        List<Symbol> destination)
    {
        for (int index = startIndex; index < endIndex; index++)
        {
            Symbol currentSymbol = sourceWord[index];
            if (!ruleMap.TryGetValue(currentSymbol.Name, out List<IProductionRule>? candidates))
            {
                destination.Add(currentSymbol);
                continue;
            }

            IProductionRule? selectedRule = null;
            IReadOnlyDictionary<string, double> capturedParameters = EmptyParameters;
            List<(IProductionRule Rule, IReadOnlyDictionary<string, double> Parameters)> matchedRules = [];
            int maximumSpecificity = -1;

            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                IProductionRule candidateRule = candidates[candidateIndex];
                if (candidateRule.Matches(sourceWord, index, grammar.IgnoredSymbols, contextBindings, out IReadOnlyDictionary<string, double> parameters))
                {
                    int specificity = CalculateSpecificity(candidateRule);
                    if (specificity > maximumSpecificity)
                    {
                        maximumSpecificity = specificity;
                        matchedRules.Clear();
                        matchedRules.Add((candidateRule, parameters));
                    }
                    else if (specificity == maximumSpecificity)
                    {
                        matchedRules.Add((candidateRule, parameters));
                    }
                }
            }

            if (matchedRules.Count == 1)
            {
                selectedRule = matchedRules[0].Rule;
                capturedParameters = matchedRules[0].Parameters;
            }
            else if (matchedRules.Count > 1)
            {
                double totalProbability = 0.0;
                for (int matchIndex = 0; matchIndex < matchedRules.Count; matchIndex++)
                {
                    totalProbability += matchedRules[matchIndex].Rule.Probability;
                }

                double deterministicProbabilityRoll = GenerateDeterministicDouble(effectiveSeed, stepIndex, index) * totalProbability;
                double accumulatedProbability = 0.0;

                for (int matchIndex = 0; matchIndex < matchedRules.Count; matchIndex++)
                {
                    accumulatedProbability += matchedRules[matchIndex].Rule.Probability;
                    if (deterministicProbabilityRoll <= accumulatedProbability || matchIndex == matchedRules.Count - 1)
                    {
                        selectedRule = matchedRules[matchIndex].Rule;
                        capturedParameters = matchedRules[matchIndex].Parameters;
                        break;
                    }
                }
            }

            if (selectedRule == null)
            {
                destination.Add(currentSymbol);
                continue;
            }

            Word successorWord = selectedRule.ProduceSuccessor(capturedParameters);
            for (int successorIndex = 0; successorIndex < successorWord.Count; successorIndex++)
            {
                destination.Add(successorWord[successorIndex]);
            }
        }
    }

    private static double GenerateDeterministicDouble(int seed, int stepIndex, int symbolIndex)
    {
        ulong state = (ulong)seed ^ ((ulong)stepIndex << 32) ^ ((ulong)symbolIndex * 0x517CC1B727220A95UL);
        state ^= state >> 30;
        state *= 0xBF58476D1CE4E5B9UL;
        state ^= state >> 27;
        state *= 0x94D049BB133111EBUL;
        state ^= state >> 31;

        ulong mantissa = state >> 11;
        return mantissa * (1.0 / (1UL << 53));
    }

    private static Word PruneCutSymbols(Word word)
    {
        bool hasCutSymbol = false;
        for (int index = 0; index < word.Count; index++)
        {
            if (word[index].Name == "%")
            {
                hasCutSymbol = true;
                break;
            }
        }

        if (!hasCutSymbol)
        {
            return word;
        }

        List<Symbol> prunedSymbols = new List<Symbol>(word.Count);
        int traversalIndex = 0;
        PruneBranch(word, ref traversalIndex, -1, prunedSymbols);
        return new Word(prunedSymbols);
    }

    private static void PruneBranch(Word word, ref int index, int currentBranchDepth, List<Symbol> output)
    {
        while (index < word.Count)
        {
            Symbol currentSymbol = word[index];

            if (currentSymbol.Name == "%")
            {
                int bracketDepth = 0;
                while (index < word.Count)
                {
                    if (word[index].Name == "[")
                    {
                        bracketDepth++;
                    }
                    else if (word[index].Name == "]")
                    {
                        if (bracketDepth == 0)
                        {
                            return;
                        }
                        bracketDepth--;
                    }
                    index++;
                }
                return;
            }

            if (currentSymbol.Name == "[")
            {
                output.Add(currentSymbol);
                index++;
                PruneBranch(word, ref index, currentBranchDepth + 1, output);
                if (index < word.Count && word[index].Name == "]")
                {
                    output.Add(word[index]);
                    index++;
                }
                continue;
            }

            if (currentSymbol.Name == "]")
            {
                return;
            }

            output.Add(currentSymbol);
            index++;
        }
    }

    private static int CalculateSpecificity(IProductionRule rule)
    {
        if (rule is ProductionRule standardRule)
        {
            int leftContextCount = standardRule.LeftContextPatterns.Count;
            int rightContextCount = standardRule.RightContextPatterns.Count;

            if (leftContextCount > 0 && rightContextCount > 0)
            {
                return 200 + leftContextCount + rightContextCount;
            }

            if (leftContextCount > 0 || rightContextCount > 0)
            {
                return 100 + leftContextCount + rightContextCount;
            }
        }

        return 0;
    }

    private static Dictionary<string, List<IProductionRule>> BuildRuleMap(IReadOnlyList<IProductionRule> rules)
    {
        Dictionary<string, List<IProductionRule>> map = new(rules.Count);
        for (int index = 0; index < rules.Count; index++)
        {
            IProductionRule rule = rules[index];
            if (!map.TryGetValue(rule.PredecessorName, out List<IProductionRule>? list))
            {
                list = [];
                map[rule.PredecessorName] = list;
            }
            list.Add(rule);
        }
        return map;
    }

    public async Task<(Word RenderableWord, GrowthAnalysisResult GrowthAnalysis)> GenerateWithAnalysisAsynchronously(
        LindenmayerGrammar grammar,
        int derivationStepCount,
        int? randomSeed,
        IProgress<double>? progressReporter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentOutOfRangeException.ThrowIfNegative(derivationStepCount);

        return await Task.Run(() =>
        {
            int effectiveSeed = randomSeed ?? 1337;
            Word currentWord = grammar.Axiom;
            List<GrowthStepRecord> stepRecords = new List<GrowthStepRecord>(derivationStepCount + 1);
            stepRecords.Add(RecordStepStatistics(0, currentWord));

            if (derivationStepCount > 0)
            {
                for (int step = 0; step < derivationStepCount; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentWord = RewriteStep(grammar, currentWord, step, effectiveSeed);
                    stepRecords.Add(RecordStepStatistics(step + 1, currentWord));
                    progressReporter?.Report((double)(step + 1) / derivationStepCount);
                }
            }
            else
            {
                progressReporter?.Report(1.0);
            }

            GrowthAnalysisResult analysisResult = _analyzer.Analyze(stepRecords, grammar);
            Word renderableWord = ApplyInterpretationHomomorphism(grammar, currentWord);
            return (renderableWord, analysisResult);
        }, cancellationToken);
    }

    private static GrowthStepRecord RecordStepStatistics(int stepIndex, Word word)
    {
        Dictionary<string, int> frequencies = [];
        int branchCount = 0;

        for (int index = 0; index < word.Count; index++)
        {
            Symbol symbol = word[index];
            if (symbol.Name is "F" or "G")
            {
                branchCount++;
            }

            if (!frequencies.TryGetValue(symbol.Name, out int count))
            {
                frequencies[symbol.Name] = 1;
            }
            else
            {
                frequencies[symbol.Name] = count + 1;
            }
        }

        return new GrowthStepRecord(stepIndex, word.Count, branchCount, frequencies);
    }
}