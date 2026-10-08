using System;

namespace PlantSim.LSystem;

public readonly record struct VectorThreeDimensional(double X, double Y, double Z)
{
    public static VectorThreeDimensional Zero => new(0.0, 0.0, 0.0);
    public static VectorThreeDimensional UnitX => new(1.0, 0.0, 0.0);
    public static VectorThreeDimensional UnitY => new(0.0, 1.0, 0.0);
    public static VectorThreeDimensional UnitZ => new(0.0, 0.0, 1.0);

    public double LengthSquared => (X * X) + (Y * Y) + (Z * Z);
    public double Length => Math.Sqrt(LengthSquared);

    public VectorThreeDimensional Normalize()
    {
        double length = Length;
        return length < 1e-10 ? Zero : new VectorThreeDimensional(X / length, Y / length, Z / length);
    }

    public static VectorThreeDimensional Add(VectorThreeDimensional left, VectorThreeDimensional right)
    {
        return new VectorThreeDimensional(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static VectorThreeDimensional Subtract(VectorThreeDimensional left, VectorThreeDimensional right)
    {
        return new VectorThreeDimensional(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static VectorThreeDimensional Multiply(VectorThreeDimensional vector, double scalar)
    {
        return new VectorThreeDimensional(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    public static VectorThreeDimensional CrossProduct(VectorThreeDimensional left, VectorThreeDimensional right)
    {
        return new VectorThreeDimensional(
            (left.Y * right.Z) - (left.Z * right.Y),
            (left.Z * right.X) - (left.X * right.Z),
            (left.X * right.Y) - (left.Y * right.X));
    }

    public static double DotProduct(VectorThreeDimensional left, VectorThreeDimensional right)
    {
        return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
    }

    public static VectorThreeDimensional operator +(VectorThreeDimensional left, VectorThreeDimensional right) => Add(left, right);
    public static VectorThreeDimensional operator -(VectorThreeDimensional left, VectorThreeDimensional right) => Subtract(left, right);
    public static VectorThreeDimensional operator *(VectorThreeDimensional vector, double scalar) => Multiply(vector, scalar);
    public static VectorThreeDimensional operator *(double scalar, VectorThreeDimensional vector) => Multiply(vector, scalar);
}