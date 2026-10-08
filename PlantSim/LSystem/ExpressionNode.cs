using System.Collections.Generic;

namespace PlantSim.LSystem;

public abstract record ExpressionNode;

internal sealed record NumberExpressionNode(double Value) : ExpressionNode;

internal sealed record IdentifierExpressionNode(string IdentifierName) : ExpressionNode;

internal sealed record UnaryExpressionNode(string OperatorSymbol, ExpressionNode Operand) : ExpressionNode;

internal sealed record BinaryExpressionNode(
    string OperatorSymbol,
    ExpressionNode LeftOperand,
    ExpressionNode RightOperand) : ExpressionNode;

internal sealed record FunctionCallExpressionNode(
    string FunctionName,
    IReadOnlyList<ExpressionNode> Arguments) : ExpressionNode;