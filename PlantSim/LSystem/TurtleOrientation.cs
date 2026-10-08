using System;

namespace PlantSim.LSystem;

public readonly record struct TurtleOrientation(
    VectorThreeDimensional HeadingDirection,
    VectorThreeDimensional LeftDirection,
    VectorThreeDimensional UpDirection)
{
    public static TurtleOrientation CreateDefaultTwoDimensional()
    {
        return new TurtleOrientation(
            VectorThreeDimensional.UnitY,
            new VectorThreeDimensional(-1.0, 0.0, 0.0),
            VectorThreeDimensional.UnitZ);
    }

    public TurtleOrientation Turn(double angleInRadians)
    {
        // ABOP Table 1.1: RU(alpha) - Heading moves towards Left
        double cosine = Math.Cos(angleInRadians);
        double sine = Math.Sin(angleInRadians);
        VectorThreeDimensional newHeading = (HeadingDirection * cosine) + (LeftDirection * sine);
        VectorThreeDimensional newLeft = (HeadingDirection * -sine) + (LeftDirection * cosine);
        return new TurtleOrientation(newHeading.Normalize(), newLeft.Normalize(), UpDirection);
    }

    public TurtleOrientation Pitch(double angleInRadians)
    {
        // ABOP Table 1.1: RL(alpha) - Pitch down (towards -Up) for positive angle
        double cosine = Math.Cos(angleInRadians);
        double sine = Math.Sin(angleInRadians);
        VectorThreeDimensional newHeading = (HeadingDirection * cosine) - (UpDirection * sine);
        VectorThreeDimensional newUp = (HeadingDirection * sine) + (UpDirection * cosine);
        return new TurtleOrientation(newHeading.Normalize(), LeftDirection, newUp.Normalize());
    }

    public TurtleOrientation Roll(double angleInRadians)
    {
        // ABOP Table 1.1: RH(alpha) - Roll left for positive angle
        double cosine = Math.Cos(angleInRadians);
        double sine = Math.Sin(angleInRadians);
        VectorThreeDimensional newLeft = (LeftDirection * cosine) - (UpDirection * sine);
        VectorThreeDimensional newUp = (LeftDirection * sine) + (UpDirection * cosine);
        return new TurtleOrientation(HeadingDirection, newLeft.Normalize(), newUp.Normalize());
    }

    public TurtleOrientation TurnAround()
    {
        return new TurtleOrientation(HeadingDirection * -1.0, LeftDirection * -1.0, UpDirection);
    }

    public TurtleOrientation RollToHorizontal(VectorThreeDimensional verticalDirection)
    {
        VectorThreeDimensional crossProduct = VectorThreeDimensional.CrossProduct(verticalDirection, HeadingDirection);
        if (crossProduct.LengthSquared < 1e-10)
        {
            return this;
        }
        VectorThreeDimensional newLeft = crossProduct.Normalize();
        VectorThreeDimensional newUp = VectorThreeDimensional.CrossProduct(HeadingDirection, newLeft).Normalize();
        return new TurtleOrientation(HeadingDirection, newLeft, newUp);
    }

    public TurtleOrientation ApplyTropism(VectorThreeDimensional tropismVector, double susceptibility, double stepLength)
    {
        if (susceptibility <= 0.0 || stepLength <= 0.0 || tropismVector.LengthSquared < 1e-10)
        {
            return this;
        }
        VectorThreeDimensional crossProduct = VectorThreeDimensional.CrossProduct(HeadingDirection, tropismVector);
        double magnitude = crossProduct.Length;
        if (magnitude < 1e-10)
        {
            return this;
        }
        double angle = susceptibility * stepLength * magnitude;
        VectorThreeDimensional axis = crossProduct.Normalize();
        return RotateAroundAxis(axis, angle);
    }

    public TurtleOrientation RotateAroundAxis(VectorThreeDimensional axis, double angleInRadians)
    {
        VectorThreeDimensional unitAxis = axis.Normalize();
        if (unitAxis.LengthSquared < 1e-10 || Math.Abs(angleInRadians) < 1e-10)
        {
            return this;
        }
        double cosine = Math.Cos(angleInRadians);
        double sine = Math.Sin(angleInRadians);

        VectorThreeDimensional Rotate(VectorThreeDimensional vector)
        {
            double dot = VectorThreeDimensional.DotProduct(unitAxis, vector);
            VectorThreeDimensional cross = VectorThreeDimensional.CrossProduct(unitAxis, vector);
            return (vector * cosine) + (cross * sine) + (unitAxis * (dot * (1.0 - cosine)));
        }

        return new TurtleOrientation(Rotate(HeadingDirection).Normalize(), Rotate(LeftDirection).Normalize(), Rotate(UpDirection).Normalize());
    }
}