using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PlantSim.LSystem;

public class Word : IReadOnlyList<Symbol>
{
    private static readonly IReadOnlyDictionary<string, double> EmptyBindings = new Dictionary<string, double>();
    private readonly IReadOnlyList<Symbol> _symbols;

    public Word(IReadOnlyList<Symbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        _symbols = symbols;
    }

    public int Count => _symbols.Count;

    public Symbol this[int index] => _symbols[index];

    public IEnumerator<Symbol> GetEnumerator() => _symbols.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < _symbols.Count; index++)
        {
            builder.Append(_symbols[index]);
        }
        return builder.ToString();
    }

    public static Word Parse(string text, IReadOnlyDictionary<string, double>? constants = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        List<Symbol> symbolList = [];
        ExpressionParser parser = new ExpressionParser();
        ExpressionEvaluator evaluator = new ExpressionEvaluator();
        IReadOnlyDictionary<string, double> bindings = constants ?? EmptyBindings;

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
            List<double> parameterValues = [];

            if (position < text.Length && text[position] == '(')
            {
                position++;
                int depth = 1;
                int argumentStart = position;
                while (position < text.Length && depth > 0)
                {
                    if (text[position] == '(')
                    {
                        depth++;
                    }
                    else if (text[position] == ')')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            string argumentText = text[argumentStart..position].Trim();
                            if (argumentText.Length > 0)
                            {
                                parameterValues.Add(EvaluateArgument(argumentText, parser, evaluator, bindings));
                            }
                            break;
                        }
                    }
                    else if (text[position] == ',' && depth == 1)
                    {
                        string argumentText = text[argumentStart..position].Trim();
                        parameterValues.Add(EvaluateArgument(argumentText, parser, evaluator, bindings));
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
                parameterValues.Add(EvaluateArgument(literal, parser, evaluator, bindings));
            }

            symbolList.Add(new Symbol(symbolName, parameterValues));
        }

        return new Word(symbolList);
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

    private static double EvaluateArgument(
        string text,
        ExpressionParser parser,
        ExpressionEvaluator evaluator,
        IReadOnlyDictionary<string, double> bindings)
    {
        if (double.TryParse(text, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }

        try
        {
            ExpressionNode node = parser.Parse(text);
            return evaluator.Evaluate(node, bindings);
        }
        catch
        {
            return 0.0;
        }
    }
}