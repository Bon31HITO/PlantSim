using PlantSim.LSystem;
using System;

namespace PlantSim.Trees;

internal class CanopyLightVoxelGrid
{
    private const int GridResolutionX = 32;
    private const int GridResolutionY = 32;
    private const int GridResolutionZ = 32;

    private readonly double[,,] _foliageDensityGrid = new double[GridResolutionX, GridResolutionY, GridResolutionZ];
    private readonly double[,,] _lightIntensityGrid = new double[GridResolutionX, GridResolutionY, GridResolutionZ];

    private VectorThreeDimensional _minimumBound = new(-50.0, 0.0, -50.0);
    private VectorThreeDimensional _maximumBound = new(50.0, 100.0, 50.0);

    public void ResetBounds(VectorThreeDimensional minimumBound, VectorThreeDimensional maximumBound)
    {
        _minimumBound = minimumBound;
        _maximumBound = maximumBound;
        Clear();
    }

    public void Clear()
    {
        for (int indexX = 0; indexX < GridResolutionX; indexX++)
        {
            for (int indexY = 0; indexY < GridResolutionY; indexY++)
            {
                for (int indexZ = 0; indexZ < GridResolutionZ; indexZ++)
                {
                    _foliageDensityGrid[indexX, indexY, indexZ] = 0.0;
                    _lightIntensityGrid[indexX, indexY, indexZ] = 1.0;
                }
            }
        }
    }

    public void RegisterFoliageDensity(VectorThreeDimensional worldPosition, double densityAmount)
    {
        (int coordinateX, int coordinateY, int coordinateZ) = WorldToVoxelIndices(worldPosition);
        if (IsWithinBounds(coordinateX, coordinateY, coordinateZ))
        {
            _foliageDensityGrid[coordinateX, coordinateY, coordinateZ] += densityAmount;
        }
    }

    public void PropagateLightAttenuation(double lightAttenuationFactor)
    {
        double effectiveAttenuation = Math.Max(0.01, lightAttenuationFactor);

        for (int indexX = 0; indexX < GridResolutionX; indexX++)
        {
            for (int indexZ = 0; indexZ < GridResolutionZ; indexZ++)
            {
                double currentLight = 1.0;
                for (int indexY = GridResolutionY - 1; indexY >= 0; indexY--)
                {
                    _lightIntensityGrid[indexX, indexY, indexZ] = currentLight;
                    double density = _foliageDensityGrid[indexX, indexY, indexZ];
                    if (density > 0.0)
                    {
                        currentLight *= Math.Exp(-density * effectiveAttenuation);
                    }
                }
            }
        }
    }

    public double SampleLightIntensity(VectorThreeDimensional worldPosition)
    {
        (int coordinateX, int coordinateY, int coordinateZ) = WorldToVoxelIndices(worldPosition);
        if (!IsWithinBounds(coordinateX, coordinateY, coordinateZ))
        {
            return 1.0;
        }
        return _lightIntensityGrid[coordinateX, coordinateY, coordinateZ];
    }

    private (int CoordinateX, int CoordinateY, int CoordinateZ) WorldToVoxelIndices(VectorThreeDimensional position)
    {
        double spanX = Math.Max(1.0, _maximumBound.X - _minimumBound.X);
        double spanY = Math.Max(1.0, _maximumBound.Y - _minimumBound.Y);
        double spanZ = Math.Max(1.0, _maximumBound.Z - _minimumBound.Z);

        double normalizedX = (position.X - _minimumBound.X) / spanX;
        double normalizedY = (position.Y - _minimumBound.Y) / spanY;
        double normalizedZ = (position.Z - _minimumBound.Z) / spanZ;

        int coordinateX = Math.Clamp((int)(normalizedX * GridResolutionX), 0, GridResolutionX - 1);
        int coordinateY = Math.Clamp((int)(normalizedY * GridResolutionY), 0, GridResolutionY - 1);
        int coordinateZ = Math.Clamp((int)(normalizedZ * GridResolutionZ), 0, GridResolutionZ - 1);

        return (coordinateX, coordinateY, coordinateZ);
    }

    private static bool IsWithinBounds(int coordinateX, int coordinateY, int coordinateZ)
    {
        return coordinateX >= 0 && coordinateX < GridResolutionX &&
               coordinateY >= 0 && coordinateY < GridResolutionY &&
               coordinateZ >= 0 && coordinateZ < GridResolutionZ;
    }
}