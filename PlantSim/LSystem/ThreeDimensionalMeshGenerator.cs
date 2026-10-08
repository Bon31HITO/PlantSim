using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PlantSim.LSystem;

internal class ThreeDimensionalMeshGenerator
{
    private const int CylinderRadialSegmentCount = 5;
    private const int SphereRadialSegmentCount = 8;
    private const int SurfaceSubdivisionCount = 8;
    private const int BranchChunkSize = 3000;
    private const int PolygonChunkSize = 1000;
    private const int SphereChunkSize = 400;
    private const int SurfaceChunkSize = 1000;

    private readonly ColorPaletteTable _colorPaletteTable;
    private readonly BezierSurfacePatchTable _patchTable;

    public ThreeDimensionalMeshGenerator(
        ColorPaletteTable? colorPaletteTable = null,
        BezierSurfacePatchTable? patchTable = null)
    {
        _colorPaletteTable = colorPaletteTable ?? new ColorPaletteTable();
        _patchTable = patchTable ?? new BezierSurfacePatchTable();
    }

    public Model3DGroup GenerateModelGroup(
        IReadOnlyList<TurtleBranchSegment> branches,
        IReadOnlyList<TurtlePolygon> polygons,
        IReadOnlyList<TurtleSphere> spheres,
        IReadOnlyList<TurtleSurface> surfaces)
    {
        ArgumentNullException.ThrowIfNull(branches);
        ArgumentNullException.ThrowIfNull(polygons);
        ArgumentNullException.ThrowIfNull(spheres);
        ArgumentNullException.ThrowIfNull(surfaces);

        Model3DGroup modelGroup = new Model3DGroup();
        if (branches.Count == 0 && polygons.Count == 0 && spheres.Count == 0 && surfaces.Count == 0)
        {
            modelGroup.Freeze();
            return modelGroup;
        }

        GenerateBranchModels(branches, modelGroup);
        GeneratePolygonModels(polygons, modelGroup);
        GenerateSphereModels(spheres, modelGroup);
        GenerateSurfaceModels(surfaces, modelGroup);

        modelGroup.Freeze();
        return modelGroup;
    }

    private void GenerateBranchModels(IReadOnlyList<TurtleBranchSegment> branches, Model3DGroup modelGroup)
    {
        if (branches.Count == 0)
        {
            return;
        }

        Dictionary<int, List<TurtleBranchSegment>> branchesByColor = GroupItemsByColor(
            branches,
            branch => branch.ColorIndex);

        foreach (KeyValuePair<int, List<TurtleBranchSegment>> pair in branchesByColor)
        {
            Material branchMaterial = _colorPaletteTable.GetMaterial(pair.Key);
            List<TurtleBranchSegment> coloredBranches = pair.Value;
            int chunkCount = (coloredBranches.Count + BranchChunkSize - 1) / BranchChunkSize;

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int startIndex = chunkIndex * BranchChunkSize;
                int count = Math.Min(BranchChunkSize, coloredBranches.Count - startIndex);
                MeshGeometry3D chunkMesh = BuildBranchChunkMesh(coloredBranches, startIndex, count);
                GeometryModel3D branchModel = new GeometryModel3D(chunkMesh, branchMaterial)
                {
                    BackMaterial = branchMaterial
                };
                branchModel.Freeze();
                modelGroup.Children.Add(branchModel);
            }
        }
    }

    private void GeneratePolygonModels(IReadOnlyList<TurtlePolygon> polygons, Model3DGroup modelGroup)
    {
        if (polygons.Count == 0)
        {
            return;
        }

        Dictionary<int, List<TurtlePolygon>> polygonsByColor = GroupItemsByColor(
            polygons,
            polygon => polygon.ColorIndex == 0 ? 2 : polygon.ColorIndex);

        foreach (KeyValuePair<int, List<TurtlePolygon>> pair in polygonsByColor)
        {
            Material polygonMaterial = _colorPaletteTable.GetMaterial(pair.Key);
            List<TurtlePolygon> coloredPolygons = pair.Value;
            int chunkCount = (coloredPolygons.Count + PolygonChunkSize - 1) / PolygonChunkSize;

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int startIndex = chunkIndex * PolygonChunkSize;
                int count = Math.Min(PolygonChunkSize, coloredPolygons.Count - startIndex);
                MeshGeometry3D chunkMesh = BuildPolygonChunkMesh(coloredPolygons, startIndex, count);
                GeometryModel3D polygonModel = new GeometryModel3D(chunkMesh, polygonMaterial)
                {
                    BackMaterial = polygonMaterial
                };
                polygonModel.Freeze();
                modelGroup.Children.Add(polygonModel);
            }
        }
    }

    private void GenerateSphereModels(IReadOnlyList<TurtleSphere> spheres, Model3DGroup modelGroup)
    {
        if (spheres.Count == 0)
        {
            return;
        }

        Dictionary<int, List<TurtleSphere>> spheresByColor = GroupItemsByColor(
            spheres,
            sphere => sphere.ColorIndex);

        foreach (KeyValuePair<int, List<TurtleSphere>> pair in spheresByColor)
        {
            Material sphereMaterial = _colorPaletteTable.GetMaterial(pair.Key);
            List<TurtleSphere> coloredSpheres = pair.Value;
            int chunkCount = (coloredSpheres.Count + SphereChunkSize - 1) / SphereChunkSize;

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int startIndex = chunkIndex * SphereChunkSize;
                int count = Math.Min(SphereChunkSize, coloredSpheres.Count - startIndex);
                MeshGeometry3D chunkMesh = BuildSphereChunkMesh(coloredSpheres, startIndex, count);
                GeometryModel3D sphereModel = new GeometryModel3D(chunkMesh, sphereMaterial);
                sphereModel.Freeze();
                modelGroup.Children.Add(sphereModel);
            }
        }
    }

    private void GenerateSurfaceModels(IReadOnlyList<TurtleSurface> surfaces, Model3DGroup modelGroup)
    {
        if (surfaces.Count == 0)
        {
            return;
        }

        Dictionary<int, List<TurtleSurface>> surfacesByColor = GroupItemsByColor(
            surfaces,
            surface => surface.ColorIndex);

        foreach (KeyValuePair<int, List<TurtleSurface>> pair in surfacesByColor)
        {
            Material surfaceMaterial = _colorPaletteTable.GetMaterial(pair.Key);
            List<TurtleSurface> coloredSurfaces = pair.Value;
            int chunkCount = (coloredSurfaces.Count + SurfaceChunkSize - 1) / SurfaceChunkSize;

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                int startIndex = chunkIndex * SurfaceChunkSize;
                int count = Math.Min(SurfaceChunkSize, coloredSurfaces.Count - startIndex);
                MeshGeometry3D chunkMesh = BuildSurfaceChunkMesh(coloredSurfaces, startIndex, count);
                GeometryModel3D surfaceModel = new GeometryModel3D(chunkMesh, surfaceMaterial)
                {
                    BackMaterial = surfaceMaterial
                };
                surfaceModel.Freeze();
                modelGroup.Children.Add(surfaceModel);
            }
        }
    }

    private static Dictionary<int, List<TItem>> GroupItemsByColor<TItem>(
        IReadOnlyList<TItem> items,
        Func<TItem, int> colorIndexSelector)
    {
        Dictionary<int, List<TItem>> groupedDictionary = [];
        for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            TItem item = items[itemIndex];
            int colorIndex = colorIndexSelector(item);
            if (!groupedDictionary.TryGetValue(colorIndex, out List<TItem>? itemList))
            {
                itemList = [];
                groupedDictionary[colorIndex] = itemList;
            }
            itemList.Add(item);
        }
        return groupedDictionary;
    }

    private static MeshGeometry3D BuildBranchChunkMesh(
        IReadOnlyList<TurtleBranchSegment> branches,
        int startIndex,
        int count)
    {
        int vertexCount = count * CylinderRadialSegmentCount * 2;
        int triangleIndexCount = count * CylinderRadialSegmentCount * 6;

        Point3D[] positions = new Point3D[vertexCount];
        Vector3D[] normals = new Vector3D[vertexCount];
        int[] triangleIndices = new int[triangleIndexCount];

        for (int localIndex = 0; localIndex < count; localIndex++)
        {
            TurtleBranchSegment branch = branches[startIndex + localIndex];
            int baseVertexIndex = localIndex * CylinderRadialSegmentCount * 2;
            int baseTriangleIndex = localIndex * CylinderRadialSegmentCount * 6;

            double startRadius = Math.Max(0.04, branch.StartThickness * 0.5);
            double endRadius = Math.Max(0.03, branch.EndThickness * 0.5);

            for (int sliceIndex = 0; sliceIndex < CylinderRadialSegmentCount; sliceIndex++)
            {
                double angle = (2.0 * Math.PI * sliceIndex) / CylinderRadialSegmentCount;
                double cosine = Math.Cos(angle);
                double sine = Math.Sin(angle);

                VectorThreeDimensional startPerpendicular =
                    (branch.StartOrientation.LeftDirection * cosine) +
                    (branch.StartOrientation.UpDirection * sine);

                VectorThreeDimensional endPerpendicular =
                    (branch.EndOrientation.LeftDirection * cosine) +
                    (branch.EndOrientation.UpDirection * sine);

                VectorThreeDimensional startRingPoint = branch.StartPosition + (startPerpendicular * startRadius);
                VectorThreeDimensional endRingPoint = branch.EndPosition + (endPerpendicular * endRadius);

                int bottomVertexPosition = baseVertexIndex + (sliceIndex * 2);
                int topVertexPosition = bottomVertexPosition + 1;

                positions[bottomVertexPosition] = new Point3D(startRingPoint.X, startRingPoint.Y, startRingPoint.Z);
                positions[topVertexPosition] = new Point3D(endRingPoint.X, endRingPoint.Y, endRingPoint.Z);

                VectorThreeDimensional unitNormalStart = startPerpendicular.Normalize();
                VectorThreeDimensional unitNormalEnd = endPerpendicular.Normalize();

                normals[bottomVertexPosition] = new Vector3D(unitNormalStart.X, unitNormalStart.Y, unitNormalStart.Z);
                normals[topVertexPosition] = new Vector3D(unitNormalEnd.X, unitNormalEnd.Y, unitNormalEnd.Z);
            }

            for (int sliceIndex = 0; sliceIndex < CylinderRadialSegmentCount; sliceIndex++)
            {
                int nextSliceIndex = (sliceIndex + 1) % CylinderRadialSegmentCount;

                int bottomCurrent = baseVertexIndex + (sliceIndex * 2);
                int topCurrent = bottomCurrent + 1;
                int bottomNext = baseVertexIndex + (nextSliceIndex * 2);
                int topNext = bottomNext + 1;

                int triangleOffset = baseTriangleIndex + (sliceIndex * 6);
                triangleIndices[triangleOffset] = bottomCurrent;
                triangleIndices[triangleOffset + 1] = bottomNext;
                triangleIndices[triangleOffset + 2] = topCurrent;

                triangleIndices[triangleOffset + 3] = bottomNext;
                triangleIndices[triangleOffset + 4] = topNext;
                triangleIndices[triangleOffset + 5] = topCurrent;
            }
        }

        Point3DCollection positionCollection = new Point3DCollection(positions);
        positionCollection.Freeze();
        Vector3DCollection normalCollection = new Vector3DCollection(normals);
        normalCollection.Freeze();
        Int32Collection triangleIndexCollection = new Int32Collection(triangleIndices);
        triangleIndexCollection.Freeze();

        MeshGeometry3D mesh = new MeshGeometry3D
        {
            Positions = positionCollection,
            Normals = normalCollection,
            TriangleIndices = triangleIndexCollection
        };
        mesh.Freeze();
        return mesh;
    }

    private static MeshGeometry3D BuildPolygonChunkMesh(
        IReadOnlyList<TurtlePolygon> polygons,
        int startIndex,
        int count)
    {
        List<Point3D> positions = [];
        List<Vector3D> normals = [];
        List<int> triangleIndices = [];

        for (int localIndex = 0; localIndex < count; localIndex++)
        {
            TurtlePolygon polygon = polygons[startIndex + localIndex];
            int vertexCount = polygon.Vertices.Count;
            if (vertexCount < 3)
            {
                continue;
            }

            int baseVertexOffset = positions.Count;
            VectorThreeDimensional accumulatedNormal = VectorThreeDimensional.Zero;

            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
            {
                VectorThreeDimensional currentVertex = polygon.Vertices[vertexIndex];
                VectorThreeDimensional nextVertex = polygon.Vertices[(vertexIndex + 1) % vertexCount];

                accumulatedNormal = new VectorThreeDimensional(
                    accumulatedNormal.X + ((currentVertex.Y - nextVertex.Y) * (currentVertex.Z + nextVertex.Z)),
                    accumulatedNormal.Y + ((currentVertex.Z - nextVertex.Z) * (currentVertex.X + nextVertex.X)),
                    accumulatedNormal.Z + ((currentVertex.X - nextVertex.X) * (currentVertex.Y + nextVertex.Y)));
            }

            VectorThreeDimensional surfaceNormal = accumulatedNormal.Normalize();
            if (surfaceNormal.LengthSquared < 1e-6)
            {
                surfaceNormal = VectorThreeDimensional.UnitY;
            }

            Vector3D normalVector = new Vector3D(surfaceNormal.X, surfaceNormal.Y, surfaceNormal.Z);
            for (int polygonVertexIndex = 0; polygonVertexIndex < vertexCount; polygonVertexIndex++)
            {
                VectorThreeDimensional vertex = polygon.Vertices[polygonVertexIndex];
                positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
                normals.Add(normalVector);
            }

            TessellatePolygonIndices(polygon.Vertices, surfaceNormal, baseVertexOffset, triangleIndices);
        }

        Point3DCollection positionCollection = new Point3DCollection(positions);
        positionCollection.Freeze();
        Vector3DCollection normalCollection = new Vector3DCollection(normals);
        normalCollection.Freeze();
        Int32Collection triangleIndexCollection = new Int32Collection(triangleIndices);
        triangleIndexCollection.Freeze();

        MeshGeometry3D mesh = new MeshGeometry3D
        {
            Positions = positionCollection,
            Normals = normalCollection,
            TriangleIndices = triangleIndexCollection
        };
        mesh.Freeze();
        return mesh;
    }

    private static void TessellatePolygonIndices(
        IReadOnlyList<VectorThreeDimensional> vertices,
        VectorThreeDimensional normal,
        int baseVertexOffset,
        List<int> destinationIndices)
    {
        int count = vertices.Count;
        if (count < 3)
        {
            return;
        }

        if (count == 3)
        {
            destinationIndices.Add(baseVertexOffset);
            destinationIndices.Add(baseVertexOffset + 1);
            destinationIndices.Add(baseVertexOffset + 2);
            return;
        }

        VectorThreeDimensional axisU = VectorThreeDimensional.CrossProduct(
            Math.Abs(normal.X) > 0.9 ? VectorThreeDimensional.UnitY : VectorThreeDimensional.UnitX,
            normal).Normalize();
        VectorThreeDimensional axisV = VectorThreeDimensional.CrossProduct(normal, axisU).Normalize();

        List<int> remainingIndices = new List<int>(count);
        double[] projectedX = new double[count];
        double[] projectedY = new double[count];

        for (int index = 0; index < count; index++)
        {
            remainingIndices.Add(index);
            projectedX[index] = VectorThreeDimensional.DotProduct(vertices[index], axisU);
            projectedY[index] = VectorThreeDimensional.DotProduct(vertices[index], axisV);
        }

        double signedArea = 0.0;
        for (int index = 0; index < count; index++)
        {
            int nextIndex = (index + 1) % count;
            signedArea += (projectedX[index] * projectedY[nextIndex]) - (projectedX[nextIndex] * projectedY[index]);
        }

        if (signedArea < 0.0)
        {
            remainingIndices.Reverse();
        }

        int iterationsWithoutEarRemoval = 0;
        while (remainingIndices.Count > 2)
        {
            bool earFound = false;
            int remainingVertexCount = remainingIndices.Count;

            for (int index = 0; index < remainingVertexCount; index++)
            {
                int previousIndex = remainingIndices[(index + remainingVertexCount - 1) % remainingVertexCount];
                int currentIndex = remainingIndices[index];
                int nextIndex = remainingIndices[(index + 1) % remainingVertexCount];

                double pointAX = projectedX[previousIndex];
                double pointAY = projectedY[previousIndex];
                double pointBX = projectedX[currentIndex];
                double pointBY = projectedY[currentIndex];
                double pointCX = projectedX[nextIndex];
                double pointCY = projectedY[nextIndex];

                double crossProductZ = ((pointBX - pointAX) * (pointCY - pointAY)) - ((pointBY - pointAY) * (pointCX - pointAX));
                if (crossProductZ <= 1e-9)
                {
                    continue;
                }

                bool hasPointInside = false;
                for (int testPosition = 0; testPosition < remainingVertexCount; testPosition++)
                {
                    int testIndex = remainingIndices[testPosition];
                    if (testIndex == previousIndex || testIndex == currentIndex || testIndex == nextIndex)
                    {
                        continue;
                    }

                    if (IsPointInsideTriangleTwoDimensional(projectedX[testIndex], projectedY[testIndex], pointAX, pointAY, pointBX, pointBY, pointCX, pointCY))
                    {
                        hasPointInside = true;
                        break;
                    }
                }

                if (!hasPointInside)
                {
                    destinationIndices.Add(baseVertexOffset + previousIndex);
                    destinationIndices.Add(baseVertexOffset + currentIndex);
                    destinationIndices.Add(baseVertexOffset + nextIndex);

                    remainingIndices.RemoveAt(index);
                    earFound = true;
                    iterationsWithoutEarRemoval = 0;
                    break;
                }
            }

            if (!earFound)
            {
                iterationsWithoutEarRemoval++;
                if (iterationsWithoutEarRemoval > remainingIndices.Count)
                {
                    for (int fallbackIndex = 1; fallbackIndex < remainingIndices.Count - 1; fallbackIndex++)
                    {
                        destinationIndices.Add(baseVertexOffset + remainingIndices[0]);
                        destinationIndices.Add(baseVertexOffset + remainingIndices[fallbackIndex]);
                        destinationIndices.Add(baseVertexOffset + remainingIndices[fallbackIndex + 1]);
                    }
                    break;
                }
            }
        }
    }

    private static bool IsPointInsideTriangleTwoDimensional(
        double pointX, double pointY,
        double pointAX, double pointAY,
        double pointBX, double pointBY,
        double pointCX, double pointCY)
    {
        double cross1 = ((pointBX - pointAX) * (pointY - pointAY)) - ((pointBY - pointAY) * (pointX - pointAX));
        double cross2 = ((pointCX - pointBX) * (pointY - pointBY)) - ((pointCY - pointBY) * (pointX - pointBX));
        double cross3 = ((pointAX - pointCX) * (pointY - pointCY)) - ((pointAY - pointCY) * (pointX - pointCX));

        bool hasNegative = (cross1 < 0.0) || (cross2 < 0.0) || (cross3 < 0.0);
        bool hasPositive = (cross1 > 0.0) || (cross2 > 0.0) || (cross3 > 0.0);
        return !(hasNegative && hasPositive);
    }

    private static MeshGeometry3D BuildSphereChunkMesh(
        IReadOnlyList<TurtleSphere> spheres,
        int startIndex,
        int count)
    {
        int verticesPerSphere = (SphereRadialSegmentCount + 1) * (SphereRadialSegmentCount + 1);
        int trianglesPerSphere = SphereRadialSegmentCount * SphereRadialSegmentCount * 6;
        int totalVertexCount = count * verticesPerSphere;
        int totalIndexCount = count * trianglesPerSphere;

        Point3D[] positions = new Point3D[totalVertexCount];
        Vector3D[] normals = new Vector3D[totalVertexCount];
        int[] triangleIndices = new int[totalIndexCount];

        int vertexOffset = 0;
        int indexOffset = 0;

        for (int localIndex = 0; localIndex < count; localIndex++)
        {
            TurtleSphere sphere = spheres[startIndex + localIndex];
            int baseVertexIndex = vertexOffset;

            for (int latitudeIndex = 0; latitudeIndex <= SphereRadialSegmentCount; latitudeIndex++)
            {
                double latitudeAngle = (Math.PI * latitudeIndex) / SphereRadialSegmentCount;
                double sineLatitude = Math.Sin(latitudeAngle);
                double cosineLatitude = Math.Cos(latitudeAngle);

                for (int longitudeIndex = 0; longitudeIndex <= SphereRadialSegmentCount; longitudeIndex++)
                {
                    double longitudeAngle = (2.0 * Math.PI * longitudeIndex) / SphereRadialSegmentCount;
                    double sineLongitude = Math.Sin(longitudeAngle);
                    double cosineLongitude = Math.Cos(longitudeAngle);

                    Vector3D normal = new Vector3D(sineLatitude * cosineLongitude, cosineLatitude, sineLatitude * sineLongitude);
                    Point3D position = new Point3D(
                        sphere.CenterPosition.X + (normal.X * sphere.Radius),
                        sphere.CenterPosition.Y + (normal.Y * sphere.Radius),
                        sphere.CenterPosition.Z + (normal.Z * sphere.Radius));

                    positions[vertexOffset] = position;
                    normals[vertexOffset] = normal;
                    vertexOffset++;
                }
            }

            int stride = SphereRadialSegmentCount + 1;
            for (int latitudeIndex = 0; latitudeIndex < SphereRadialSegmentCount; latitudeIndex++)
            {
                for (int longitudeIndex = 0; longitudeIndex < SphereRadialSegmentCount; longitudeIndex++)
                {
                    int current = baseVertexIndex + (latitudeIndex * stride) + longitudeIndex;
                    int next = current + stride;

                    triangleIndices[indexOffset++] = current;
                    triangleIndices[indexOffset++] = next;
                    triangleIndices[indexOffset++] = current + 1;

                    triangleIndices[indexOffset++] = current + 1;
                    triangleIndices[indexOffset++] = next;
                    triangleIndices[indexOffset++] = next + 1;
                }
            }
        }

        Point3DCollection positionCollection = new Point3DCollection(positions);
        positionCollection.Freeze();
        Vector3DCollection normalCollection = new Vector3DCollection(normals);
        normalCollection.Freeze();
        Int32Collection triangleIndexCollection = new Int32Collection(triangleIndices);
        triangleIndexCollection.Freeze();

        MeshGeometry3D mesh = new MeshGeometry3D
        {
            Positions = positionCollection,
            Normals = normalCollection,
            TriangleIndices = triangleIndexCollection
        };
        mesh.Freeze();
        return mesh;
    }

    private MeshGeometry3D BuildSurfaceChunkMesh(
        IReadOnlyList<TurtleSurface> surfaces,
        int startIndex,
        int count)
    {
        const int subdivisions = SurfaceSubdivisionCount;
        int verticesPerSurface = (subdivisions + 1) * (subdivisions + 1);
        int trianglesPerSurface = subdivisions * subdivisions * 6;
        int totalVertexCount = count * verticesPerSurface;
        int totalIndexCount = count * trianglesPerSurface;

        Point3D[] positions = new Point3D[totalVertexCount];
        Vector3D[] normals = new Vector3D[totalVertexCount];
        int[] triangleIndices = new int[totalIndexCount];

        int vertexOffset = 0;
        int indexOffset = 0;

        for (int localIndex = 0; localIndex < count; localIndex++)
        {
            TurtleSurface surface = surfaces[startIndex + localIndex];
            BezierSurfacePatch patch = surface.CustomPatch ?? _patchTable.GetPatch(surface.SurfaceKind);
            int baseVertexIndex = vertexOffset;
            double scale = surface.Scale;

            VectorThreeDimensional leftDirection = surface.Orientation.LeftDirection;
            VectorThreeDimensional headingDirection = surface.Orientation.HeadingDirection;
            VectorThreeDimensional upDirection = surface.Orientation.UpDirection;
            VectorThreeDimensional basePosition = surface.Position;

            for (int stepU = 0; stepU <= subdivisions; stepU++)
            {
                double parameterU = (double)stepU / subdivisions;
                for (int stepV = 0; stepV <= subdivisions; stepV++)
                {
                    double parameterV = (double)stepV / subdivisions;
                    (VectorThreeDimensional localPosition, VectorThreeDimensional localNormal) = patch.Evaluate(parameterU, parameterV);

                    VectorThreeDimensional worldPosition;
                    VectorThreeDimensional worldNormal;

                    if (surface.CustomPatch != null)
                    {
                        worldPosition = localPosition;
                        worldNormal = localNormal;
                    }
                    else
                    {
                        worldPosition = basePosition +
                            (leftDirection * (localPosition.X * scale)) +
                            (headingDirection * (localPosition.Y * scale)) +
                            (upDirection * (localPosition.Z * scale));

                        worldNormal =
                            (leftDirection * localNormal.X) +
                            (headingDirection * localNormal.Y) +
                            (upDirection * localNormal.Z);
                    }

                    VectorThreeDimensional normalizedNormal = worldNormal.Normalize();
                    positions[vertexOffset] = new Point3D(worldPosition.X, worldPosition.Y, worldPosition.Z);
                    normals[vertexOffset] = new Vector3D(normalizedNormal.X, normalizedNormal.Y, normalizedNormal.Z);
                    vertexOffset++;
                }
            }

            int stride = subdivisions + 1;
            for (int stepU = 0; stepU < subdivisions; stepU++)
            {
                for (int stepV = 0; stepV < subdivisions; stepV++)
                {
                    int current = baseVertexIndex + (stepU * stride) + stepV;
                    int next = current + stride;

                    triangleIndices[indexOffset++] = current;
                    triangleIndices[indexOffset++] = next;
                    triangleIndices[indexOffset++] = current + 1;

                    triangleIndices[indexOffset++] = current + 1;
                    triangleIndices[indexOffset++] = next;
                    triangleIndices[indexOffset++] = next + 1;
                }
            }
        }

        Point3DCollection positionCollection = new Point3DCollection(positions);
        positionCollection.Freeze();
        Vector3DCollection normalCollection = new Vector3DCollection(normals);
        normalCollection.Freeze();
        Int32Collection triangleIndexCollection = new Int32Collection(triangleIndices);
        triangleIndexCollection.Freeze();

        MeshGeometry3D mesh = new MeshGeometry3D
        {
            Positions = positionCollection,
            Normals = normalCollection,
            TriangleIndices = triangleIndexCollection
        };
        mesh.Freeze();
        return mesh;
    }
}