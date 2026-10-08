using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal class ExpressionEvaluator
{
    public double Evaluate(ExpressionNode expressionNode, IReadOnlyDictionary<string, double> parameterBindings)
    {
        ArgumentNullException.ThrowIfNull(expressionNode);
        ArgumentNullException.ThrowIfNull(parameterBindings);

        return expressionNode switch
        {
            NumberExpressionNode numberNode => numberNode.Value,
            IdentifierExpressionNode identifierNode =>
                parameterBindings.TryGetValue(identifierNode.IdentifierName, out double boundValue)
                    ? boundValue
                    : throw new InvalidOperationException($"Undefined parameter: {identifierNode.IdentifierName}"),
            UnaryExpressionNode unaryNode => EvaluateUnary(unaryNode, parameterBindings),
            BinaryExpressionNode binaryNode => EvaluateBinary(binaryNode, parameterBindings),
            FunctionCallExpressionNode functionNode => EvaluateFunctionCall(functionNode, parameterBindings),
            _ => throw new NotSupportedException($"Unsupported expression node: {expressionNode.GetType().Name}")
        };
    }

    private double EvaluateUnary(UnaryExpressionNode unaryNode, IReadOnlyDictionary<string, double> parameterBindings)
    {
        double operandValue = Evaluate(unaryNode.Operand, parameterBindings);
        return unaryNode.OperatorSymbol switch
        {
            "+" => operandValue,
            "-" => -operandValue,
            "!" => Math.Abs(operandValue) < 1e-10 ? 1.0 : 0.0,
            _ => throw new NotSupportedException($"Unsupported unary operator: {unaryNode.OperatorSymbol}")
        };
    }

    private double EvaluateBinary(BinaryExpressionNode binaryNode, IReadOnlyDictionary<string, double> parameterBindings)
    {
        double leftValue = Evaluate(binaryNode.LeftOperand, parameterBindings);
        double rightValue = Evaluate(binaryNode.RightOperand, parameterBindings);

        return binaryNode.OperatorSymbol switch
        {
            "+" => leftValue + rightValue,
            "-" => leftValue - rightValue,
            "*" => leftValue * rightValue,
            "/" => Math.Abs(rightValue) < 1e-12 ? 0.0 : leftValue / rightValue,
            "%" => leftValue % rightValue,
            "^" => Math.Pow(leftValue, rightValue),
            "==" => Math.Abs(leftValue - rightValue) < 1e-7 ? 1.0 : 0.0,
            "!=" => Math.Abs(leftValue - rightValue) >= 1e-7 ? 1.0 : 0.0,
            "<" => leftValue < rightValue ? 1.0 : 0.0,
            "<=" => leftValue <= rightValue ? 1.0 : 0.0,
            ">" => leftValue > rightValue ? 1.0 : 0.0,
            ">=" => leftValue >= rightValue ? 1.0 : 0.0,
            "&&" => (Math.Abs(leftValue) > 1e-10 && Math.Abs(rightValue) > 1e-10) ? 1.0 : 0.0,
            "||" => (Math.Abs(leftValue) > 1e-10 || Math.Abs(rightValue) > 1e-10) ? 1.0 : 0.0,
            _ => throw new NotSupportedException($"Unsupported binary operator: {binaryNode.OperatorSymbol}")
        };
    }

    private double EvaluateFunctionCall(
        FunctionCallExpressionNode functionNode,
        IReadOnlyDictionary<string, double> parameterBindings)
    {
        double[] argumentValues = new double[functionNode.Arguments.Count];
        for (int argumentIndex = 0; argumentIndex < functionNode.Arguments.Count; argumentIndex++)
        {
            argumentValues[argumentIndex] = Evaluate(functionNode.Arguments[argumentIndex], parameterBindings);
        }

        string functionName = functionNode.FunctionName.ToLowerInvariant();
        return functionName switch
        {
            "sin" when argumentValues.Length == 1 => Math.Sin(argumentValues[0] * (Math.PI / 180.0)),
            "cos" when argumentValues.Length == 1 => Math.Cos(argumentValues[0] * (Math.PI / 180.0)),
            "tan" when argumentValues.Length == 1 => Math.Tan(argumentValues[0] * (Math.PI / 180.0)),
            "asin" when argumentValues.Length == 1 => Math.Asin(argumentValues[0]) * (180.0 / Math.PI),
            "acos" when argumentValues.Length == 1 => Math.Acos(argumentValues[0]) * (180.0 / Math.PI),
            "atan" when argumentValues.Length == 1 => Math.Atan(argumentValues[0]) * (180.0 / Math.PI),
            "sqrt" when argumentValues.Length == 1 => Math.Sqrt(Math.Max(0.0, argumentValues[0])),
            "pow" when argumentValues.Length == 2 => Math.Pow(Math.Max(0.0, argumentValues[0]), argumentValues[1]),
            "abs" when argumentValues.Length == 1 => Math.Abs(argumentValues[0]),
            "min" when argumentValues.Length == 2 => Math.Min(argumentValues[0], argumentValues[1]),
            "max" when argumentValues.Length == 2 => Math.Max(argumentValues[0], argumentValues[1]),
            _ => throw new NotSupportedException($"Unsupported function call: {functionName} with {argumentValues.Length} arguments")
        };
    }
}