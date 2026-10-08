using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PlantSim.LSystem;

internal class ExpressionParser
{
    public ExpressionNode Parse(string expressionText)
    {
        ArgumentNullException.ThrowIfNull(expressionText);
        IReadOnlyList<string> tokenList = Tokenize(expressionText);
        if (tokenList.Count == 0)
        {
            return new NumberExpressionNode(0.0);
        }

        int currentTokenIndex = 0;
        ExpressionNode rootNode = ParseLogicalOr(tokenList, ref currentTokenIndex);
        if (currentTokenIndex < tokenList.Count)
        {
            throw new FormatException($"Unexpected token at end of expression: {tokenList[currentTokenIndex]}");
        }

        return rootNode;
    }

    private ExpressionNode ParseLogicalOr(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        ExpressionNode leftNode = ParseLogicalAnd(tokenList, ref tokenIndex);
        while (tokenIndex < tokenList.Count && tokenList[tokenIndex] == "||")
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode rightNode = ParseLogicalAnd(tokenList, ref tokenIndex);
            leftNode = new BinaryExpressionNode(operatorSymbol, leftNode, rightNode);
        }

        return leftNode;
    }

    private ExpressionNode ParseLogicalAnd(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        ExpressionNode leftNode = ParseEquality(tokenList, ref tokenIndex);
        while (tokenIndex < tokenList.Count && tokenList[tokenIndex] == "&&")
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode rightNode = ParseEquality(tokenList, ref tokenIndex);
            leftNode = new BinaryExpressionNode(operatorSymbol, leftNode, rightNode);
        }

        return leftNode;
    }

    private ExpressionNode ParseEquality(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        ExpressionNode leftNode = ParseRelational(tokenList, ref tokenIndex);
        while (tokenIndex < tokenList.Count && (tokenList[tokenIndex] == "==" || tokenList[tokenIndex] == "!="))
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode rightNode = ParseRelational(tokenList, ref tokenIndex);
            leftNode = new BinaryExpressionNode(operatorSymbol, leftNode, rightNode);
        }

        return leftNode;
    }

    private ExpressionNode ParseRelational(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        ExpressionNode leftNode = ParseAdditive(tokenList, ref tokenIndex);
        while (tokenIndex < tokenList.Count &&
               (tokenList[tokenIndex] == "<" || tokenList[tokenIndex] == "<=" ||
                tokenList[tokenIndex] == ">" || tokenList[tokenIndex] == ">="))
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode rightNode = ParseAdditive(tokenList, ref tokenIndex);
            leftNode = new BinaryExpressionNode(operatorSymbol, leftNode, rightNode);
        }

        return leftNode;
    }

    private ExpressionNode ParseAdditive(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        ExpressionNode leftNode = ParseMultiplicative(tokenList, ref tokenIndex);
        while (tokenIndex < tokenList.Count && (tokenList[tokenIndex] == "+" || tokenList[tokenIndex] == "-"))
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode rightNode = ParseMultiplicative(tokenList, ref tokenIndex);
            leftNode = new BinaryExpressionNode(operatorSymbol, leftNode, rightNode);
        }

        return leftNode;
    }

    private ExpressionNode ParseMultiplicative(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        ExpressionNode leftNode = ParsePower(tokenList, ref tokenIndex);
        while (tokenIndex < tokenList.Count &&
               (tokenList[tokenIndex] == "*" || tokenList[tokenIndex] == "/" || tokenList[tokenIndex] == "%"))
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode rightNode = ParsePower(tokenList, ref tokenIndex);
            leftNode = new BinaryExpressionNode(operatorSymbol, leftNode, rightNode);
        }

        return leftNode;
    }

    private ExpressionNode ParsePower(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        ExpressionNode leftNode = ParseUnary(tokenList, ref tokenIndex);
        if (tokenIndex < tokenList.Count && tokenList[tokenIndex] == "^")
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode rightNode = ParsePower(tokenList, ref tokenIndex);
            return new BinaryExpressionNode(operatorSymbol, leftNode, rightNode);
        }

        return leftNode;
    }

    private ExpressionNode ParseUnary(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        if (tokenIndex < tokenList.Count &&
            (tokenList[tokenIndex] == "+" || tokenList[tokenIndex] == "-" || tokenList[tokenIndex] == "!"))
        {
            string operatorSymbol = tokenList[tokenIndex];
            tokenIndex++;
            ExpressionNode operandNode = ParseUnary(tokenList, ref tokenIndex);
            return new UnaryExpressionNode(operatorSymbol, operandNode);
        }

        return ParsePrimary(tokenList, ref tokenIndex);
    }

    private ExpressionNode ParsePrimary(IReadOnlyList<string> tokenList, ref int tokenIndex)
    {
        if (tokenIndex >= tokenList.Count)
        {
            throw new FormatException("Unexpected end of expression.");
        }

        string currentToken = tokenList[tokenIndex];

        if (currentToken == "(")
        {
            tokenIndex++;
            ExpressionNode groupedNode = ParseLogicalOr(tokenList, ref tokenIndex);
            if (tokenIndex >= tokenList.Count || tokenList[tokenIndex] != ")")
            {
                throw new FormatException("Missing closing parenthesis ')'.");
            }

            tokenIndex++;
            return groupedNode;
        }

        if (double.TryParse(currentToken, CultureInfo.InvariantCulture, out double parsedValue))
        {
            tokenIndex++;
            return new NumberExpressionNode(parsedValue);
        }

        if (char.IsLetter(currentToken[0]) || currentToken[0] == '_')
        {
            string identifierName = currentToken;
            tokenIndex++;

            if (tokenIndex < tokenList.Count && tokenList[tokenIndex] == "(")
            {
                tokenIndex++;
                List<ExpressionNode> argumentsList = [];
                if (tokenIndex < tokenList.Count && tokenList[tokenIndex] != ")")
                {
                    while (true)
                    {
                        argumentsList.Add(ParseLogicalOr(tokenList, ref tokenIndex));
                        if (tokenIndex < tokenList.Count && tokenList[tokenIndex] == ",")
                        {
                            tokenIndex++;
                            continue;
                        }

                        break;
                    }
                }

                if (tokenIndex >= tokenList.Count || tokenList[tokenIndex] != ")")
                {
                    throw new FormatException($"Missing closing parenthesis ')' for function: {identifierName}");
                }

                tokenIndex++;
                return new FunctionCallExpressionNode(identifierName, argumentsList);
            }

            return new IdentifierExpressionNode(identifierName);
        }

        throw new FormatException($"Unrecognized expression token: {currentToken}");
    }

    private static IReadOnlyList<string> Tokenize(string text)
    {
        List<string> tokens = [];
        int characterIndex = 0;

        while (characterIndex < text.Length)
        {
            char currentCharacter = text[characterIndex];
            if (char.IsWhiteSpace(currentCharacter))
            {
                characterIndex++;
                continue;
            }

            if (characterIndex + 1 < text.Length)
            {
                string twoCharacterOperator = text.Substring(characterIndex, 2);
                if (twoCharacterOperator is "==" or "!=" or "<=" or ">=" or "&&" or "||")
                {
                    tokens.Add(twoCharacterOperator);
                    characterIndex += 2;
                    continue;
                }
            }

            if (char.IsDigit(currentCharacter) || (currentCharacter == '.' && characterIndex + 1 < text.Length && char.IsDigit(text[characterIndex + 1])))
            {
                int numberStartIndex = characterIndex;
                bool hasDecimalPoint = currentCharacter == '.';
                characterIndex++;

                while (characterIndex < text.Length)
                {
                    char scanCharacter = text[characterIndex];
                    if (char.IsDigit(scanCharacter))
                    {
                        characterIndex++;
                    }
                    else if (scanCharacter == '.' && !hasDecimalPoint)
                    {
                        hasDecimalPoint = true;
                        characterIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                tokens.Add(text[numberStartIndex..characterIndex]);
                continue;
            }

            if (char.IsLetter(currentCharacter) || currentCharacter == '_')
            {
                int identifierStartIndex = characterIndex;
                characterIndex++;
                while (characterIndex < text.Length && (char.IsLetterOrDigit(text[characterIndex]) || text[characterIndex] == '_'))
                {
                    characterIndex++;
                }

                tokens.Add(text[identifierStartIndex..characterIndex]);
                continue;
            }

            tokens.Add(currentCharacter.ToString());
            characterIndex++;
        }

        return tokens;
    }
}