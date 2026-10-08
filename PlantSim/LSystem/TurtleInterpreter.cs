using System;
using System.Collections.Generic;

namespace PlantSim.LSystem;

internal class TurtleInterpreter
{
    public double DefaultStepLength { get; set; } = 10.0;
    public double DefaultAngleInDegrees { get; set; } = 90.0;
    public double DefaultLineThickness { get; set; } = 1.0;
    public VectorThreeDimensional DefaultTropismVector { get; set; } = VectorThreeDimensional.Zero;
    public double DefaultTropismSusceptibility { get; set; } = 0.0;
    public VectorThreeDimensional VerticalDirection { get; set; } = VectorThreeDimensional.UnitY;

    public (IReadOnlyList<TurtleBranchSegment> Branches,
            IReadOnlyList<TurtlePolygon> Polygons,
            IReadOnlyList<TurtleSphere> Spheres,
            IReadOnlyList<TurtleSurface> Surfaces) Interpret(Word word)
    {
        ArgumentNullException.ThrowIfNull(word);

        List<TurtleBranchSegment> branches = new List<TurtleBranchSegment>(Math.Max(16, word.Count / 2));
        List<TurtlePolygon> polygons = [];
        List<TurtleSphere> spheres = [];
        List<TurtleSurface> surfaces = [];
        Stack<TurtleState> stateStack = new Stack<TurtleState>(64);
        Dictionary<int, VectorThreeDimensional> contactPointTable = [];

        List<VectorThreeDimensional> currentPolygonVertices = [];
        List<VectorThreeDimensional> primaryBoundaryCurve = [];
        List<VectorThreeDimensional> secondaryBoundaryCurve = [];
        List<VectorThreeDimensional> tertiaryBoundaryCurve = [];

        bool explicitVertexDefined = false;
        bool invertPolygonNormal = false;
        bool insidePolygon = false;
        bool insideBoundaryTracing = false;

        TurtleOrientation orientation = TurtleOrientation.CreateDefaultTwoDimensional();
        TurtleState state = new TurtleState(
            VectorThreeDimensional.Zero,
            orientation,
            DefaultStepLength,
            DefaultLineThickness,
            DefaultTropismSusceptibility,
            DefaultTropismVector,
            0);

        double defaultAngleRadians = DefaultAngleInDegrees * (Math.PI / 180.0);

        for (int index = 0; index < word.Count; index++)
        {
            Symbol symbol = word[index];
            switch (symbol.Name)
            {
                case "F":
                case "G":
                    {
                        double length = symbol.Parameters.Count > 0 ? symbol.Parameters[0] : state.StepLength;
                        double thickness = symbol.Parameters.Count > 1 ? symbol.Parameters[1] : state.LineThickness;

                        TurtleOrientation previousOrientation = state.Orientation;
                        TurtleOrientation bentOrientation = state.Orientation;

                        if (state.TropismSusceptibility > 0.0 && state.TropismVector.LengthSquared > 1e-10)
                        {
                            bentOrientation = state.Orientation.ApplyTropism(state.TropismVector, state.TropismSusceptibility, length);
                        }

                        VectorThreeDimensional targetPosition = state.Position + (bentOrientation.HeadingDirection * length);

                        branches.Add(new TurtleBranchSegment(
                            state.Position,
                            targetPosition,
                            previousOrientation,
                            bentOrientation,
                            state.LineThickness,
                            thickness,
                            state.ColorIndex));

                        if (insidePolygon && !explicitVertexDefined)
                        {
                            currentPolygonVertices.Add(targetPosition);
                        }

                        if (insideBoundaryTracing)
                        {
                            if (primaryBoundaryCurve.Count == 0 || secondaryBoundaryCurve.Count == 0)
                            {
                                primaryBoundaryCurve.Add(targetPosition);
                            }
                            else if (tertiaryBoundaryCurve.Count == 0)
                            {
                                secondaryBoundaryCurve.Add(targetPosition);
                            }
                            else
                            {
                                tertiaryBoundaryCurve.Add(targetPosition);
                            }
                        }

                        state = state with { Position = targetPosition, Orientation = bentOrientation, LineThickness = thickness };
                        break;
                    }

                case "f":
                    {
                        double length = symbol.Parameters.Count > 0 ? symbol.Parameters[0] : state.StepLength;
                        VectorThreeDimensional targetPosition = state.Position + (state.Orientation.HeadingDirection * length);

                        if (insidePolygon && !explicitVertexDefined)
                        {
                            currentPolygonVertices.Add(targetPosition);
                        }

                        state = state with { Position = targetPosition };
                        break;
                    }

                case "{":
                    {
                        insidePolygon = true;
                        explicitVertexDefined = false;
                        invertPolygonNormal = false;
                        currentPolygonVertices.Clear();
                        currentPolygonVertices.Add(state.Position);

                        if (symbol.Parameters.Count > 0 && symbol.Parameters[0] > 1.0)
                        {
                            insideBoundaryTracing = true;
                            primaryBoundaryCurve.Clear();
                            secondaryBoundaryCurve.Clear();
                            tertiaryBoundaryCurve.Clear();
                            primaryBoundaryCurve.Add(state.Position);
                        }
                        break;
                    }

                case ".":
                    {
                        if (insidePolygon)
                        {
                            if (!explicitVertexDefined)
                            {
                                explicitVertexDefined = true;
                                currentPolygonVertices.Clear();
                            }

                            VectorThreeDimensional vertexPosition = symbol.Parameters.Count >= 3
                                ? new VectorThreeDimensional(symbol.Parameters[0], symbol.Parameters[1], symbol.Parameters[2])
                                : state.Position;

                            currentPolygonVertices.Add(vertexPosition);
                        }
                        break;
                    }

                case "}":
                    {
                        insidePolygon = false;

                        if (insideBoundaryTracing && primaryBoundaryCurve.Count >= 2 && secondaryBoundaryCurve.Count >= 2)
                        {
                            if (tertiaryBoundaryCurve.Count >= 2)
                            {
                                (BezierSurfacePatch leftPatch, BezierSurfacePatch rightPatch) =
                                    BezierSurfacePatch.CreateBilateralSmoothPatches(
                                        primaryBoundaryCurve,
                                        secondaryBoundaryCurve,
                                        tertiaryBoundaryCurve);

                                surfaces.Add(new TurtleSurface(
                                    0,
                                    primaryBoundaryCurve[0],
                                    state.Orientation,
                                    1.0,
                                    state.ColorIndex,
                                    leftPatch));

                                surfaces.Add(new TurtleSurface(
                                    0,
                                    primaryBoundaryCurve[0],
                                    state.Orientation,
                                    1.0,
                                    state.ColorIndex,
                                    rightPatch));
                            }
                            else
                            {
                                BezierSurfacePatch boundaryPatch = BezierSurfacePatch.CreateFromBoundaryCurves(
                                    primaryBoundaryCurve,
                                    secondaryBoundaryCurve);

                                surfaces.Add(new TurtleSurface(
                                    0,
                                    primaryBoundaryCurve[0],
                                    state.Orientation,
                                    1.0,
                                    state.ColorIndex,
                                    boundaryPatch));
                            }

                            insideBoundaryTracing = false;
                            primaryBoundaryCurve.Clear();
                            secondaryBoundaryCurve.Clear();
                            tertiaryBoundaryCurve.Clear();
                        }
                        else if (currentPolygonVertices.Count >= 3)
                        {
                            if (VectorThreeDimensional.Subtract(currentPolygonVertices[^1], currentPolygonVertices[0]).LengthSquared < 1e-6)
                            {
                                currentPolygonVertices.RemoveAt(currentPolygonVertices.Count - 1);
                            }

                            if (currentPolygonVertices.Count >= 3)
                            {
                                List<VectorThreeDimensional> vertices = [.. currentPolygonVertices];
                                if (invertPolygonNormal)
                                {
                                    vertices.Reverse();
                                }
                                polygons.Add(new TurtlePolygon(vertices, state.ColorIndex));
                            }
                        }

                        currentPolygonVertices.Clear();
                        explicitVertexDefined = false;
                        break;
                    }

                case "~":
                    {
                        if (insidePolygon || symbol.Parameters.Count == 0)
                        {
                            invertPolygonNormal = !invertPolygonNormal;
                        }
                        else
                        {
                            int patchKind = 0;
                            double patchScale = state.StepLength;

                            if (symbol.Parameters.Count == 1)
                            {
                                patchScale = symbol.Parameters[0];
                                surfaces.Add(new TurtleSurface(patchKind, state.Position, state.Orientation, patchScale, state.ColorIndex));
                            }
                            else if (symbol.Parameters.Count == 2)
                            {
                                patchKind = (int)symbol.Parameters[0];
                                patchScale = symbol.Parameters[1];
                                surfaces.Add(new TurtleSurface(patchKind, state.Position, state.Orientation, patchScale, state.ColorIndex));
                            }
                            else if (symbol.Parameters.Count >= 4)
                            {
                                patchKind = (int)symbol.Parameters[0];
                                patchScale = symbol.Parameters[1];
                                int startContactIdentifier = (int)symbol.Parameters[2];
                                int endContactIdentifier = (int)symbol.Parameters[3];

                                if (contactPointTable.TryGetValue(startContactIdentifier, out VectorThreeDimensional startContactPoint) &&
                                    contactPointTable.TryGetValue(endContactIdentifier, out VectorThreeDimensional endContactPoint))
                                {
                                    BezierSurfacePatch basePatch = patchKind == 1
                                        ? BezierSurfacePatch.CreatePetalPatch()
                                        : BezierSurfacePatch.CreateLeafPatch();

                                    BezierSurfacePatch deformedPatch = basePatch.CreateDeformedPatch(
                                        startContactPoint,
                                        endContactPoint,
                                        state.Orientation.UpDirection);

                                    surfaces.Add(new TurtleSurface(
                                        patchKind,
                                        startContactPoint,
                                        state.Orientation,
                                        1.0,
                                        state.ColorIndex,
                                        deformedPatch));
                                }
                                else
                                {
                                    surfaces.Add(new TurtleSurface(patchKind, state.Position, state.Orientation, patchScale, state.ColorIndex));
                                }
                            }
                        }
                        break;
                    }

                case "@":
                    {
                        double radius = symbol.Parameters.Count > 0 ? symbol.Parameters[0] : Math.Max(0.1, state.LineThickness * 0.7);
                        spheres.Add(new TurtleSphere(state.Position, radius, state.ColorIndex));

                        if (symbol.Parameters.Count >= 2)
                        {
                            int contactPointIdentifier = (int)symbol.Parameters[1];
                            contactPointTable[contactPointIdentifier] = state.Position;
                        }
                        break;
                    }

                case "$":
                    {
                        state = state with { Orientation = state.Orientation.RollToHorizontal(VerticalDirection) };
                        break;
                    }

                case "+":
                    {
                        double turnAngle = symbol.Parameters.Count > 0 ? symbol.Parameters[0] * (Math.PI / 180.0) : defaultAngleRadians;
                        state = state with { Orientation = state.Orientation.Turn(turnAngle) };
                        break;
                    }

                case "-":
                    {
                        double turnAngle = symbol.Parameters.Count > 0 ? symbol.Parameters[0] * (Math.PI / 180.0) : defaultAngleRadians;
                        state = state with { Orientation = state.Orientation.Turn(-turnAngle) };
                        break;
                    }

                case "&":
                    {
                        double pitchAngle = symbol.Parameters.Count > 0 ? symbol.Parameters[0] * (Math.PI / 180.0) : defaultAngleRadians;
                        state = state with { Orientation = state.Orientation.Pitch(pitchAngle) };
                        break;
                    }

                case "^":
                    {
                        double pitchAngle = symbol.Parameters.Count > 0 ? symbol.Parameters[0] * (Math.PI / 180.0) : defaultAngleRadians;
                        state = state with { Orientation = state.Orientation.Pitch(-pitchAngle) };
                        break;
                    }

                case "\\":
                    {
                        double rollAngle = symbol.Parameters.Count > 0 ? symbol.Parameters[0] * (Math.PI / 180.0) : defaultAngleRadians;
                        state = state with { Orientation = state.Orientation.Roll(rollAngle) };
                        break;
                    }

                case "/":
                    {
                        double rollAngle = symbol.Parameters.Count > 0 ? symbol.Parameters[0] * (Math.PI / 180.0) : defaultAngleRadians;
                        state = state with { Orientation = state.Orientation.Roll(-rollAngle) };
                        break;
                    }

                case "|":
                    {
                        state = state with { Orientation = state.Orientation.TurnAround() };
                        break;
                    }

                case "[":
                    {
                        stateStack.Push(state);

                        if (insideBoundaryTracing)
                        {
                            if (primaryBoundaryCurve.Count > 0 && secondaryBoundaryCurve.Count == 0)
                            {
                                secondaryBoundaryCurve.Add(state.Position);
                            }
                            else if (secondaryBoundaryCurve.Count > 0 && tertiaryBoundaryCurve.Count == 0)
                            {
                                tertiaryBoundaryCurve.Add(state.Position);
                            }
                        }
                        break;
                    }

                case "]":
                    {
                        if (stateStack.Count > 0)
                        {
                            state = stateStack.Pop();
                        }
                        break;
                    }

                case "!":
                    {
                        double newDiameter = symbol.Parameters.Count > 0
                            ? symbol.Parameters[0]
                            : Math.Max(0.04, state.LineThickness * 0.707);
                        state = state with { LineThickness = newDiameter };
                        break;
                    }

                case ";":
                    {
                        double newDiameter = symbol.Parameters.Count > 0
                            ? (symbol.Parameters[0] > 1.0 ? symbol.Parameters[0] : state.LineThickness * symbol.Parameters[0])
                            : state.LineThickness * 1.3;
                        state = state with { LineThickness = newDiameter };
                        break;
                    }

                case "'":
                    {
                        int color = symbol.Parameters.Count > 0 ? (int)symbol.Parameters[0] : state.ColorIndex + 1;
                        state = state with { ColorIndex = color };
                        break;
                    }

                case ",":
                    {
                        int color = symbol.Parameters.Count > 0 ? (int)symbol.Parameters[0] : Math.Max(0, state.ColorIndex - 1);
                        state = state with { ColorIndex = color };
                        break;
                    }
            }
        }

        return (branches, polygons, spheres, surfaces);
    }
}