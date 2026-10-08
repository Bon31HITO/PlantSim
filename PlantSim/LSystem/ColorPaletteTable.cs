using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PlantSim.LSystem;

public class ColorPaletteTable
{
    private readonly List<Color> _colors;
    private readonly Dictionary<int, Material> _materialCache;

    public ColorPaletteTable()
        : this(CreateDefaultPaletteColors())
    {
    }

    public ColorPaletteTable(IEnumerable<Color> initialColors)
    {
        ArgumentNullException.ThrowIfNull(initialColors);
        _colors = [.. initialColors];
        if (_colors.Count == 0)
        {
            _colors.Add(Color.FromRgb(45, 175, 60));
        }

        _materialCache = new Dictionary<int, Material>(_colors.Count);
    }

    public int ColorCount => _colors.Count;

    public static ColorPaletteTable CreateBushPalette()
    {
        List<Color> bushColors =
        [
            Color.FromRgb(110, 70, 35),     // 0: Main Trunk Bark Brown (主幹1: 濃い茶色)
            Color.FromRgb(125, 85, 45),     // 1: Medium Stem Wood Brown (主幹2: 木質茶色)
            Color.FromRgb(115, 100, 50),    // 2: Young Stem Olive Wood (主幹3: オリーブがかった木質色)
            Color.FromRgb(45, 135, 50),     // 3: Deep Forest Green (初期の葉・太い枝先)
            Color.FromRgb(55, 155, 55),     // 4: Mid Foliage Green
            Color.FromRgb(65, 175, 55),     // 5: Rich Leaf Green
            Color.FromRgb(80, 190, 50),     // 6: Fresh Leaf Green
            Color.FromRgb(95, 200, 50),     // 7: Sunlit Leaf Green
            Color.FromRgb(115, 210, 55),    // 8: Sprout Bright Green
            Color.FromRgb(130, 215, 60),    // 9: Young Sprout Green
            Color.FromRgb(70, 165, 50),     // 10: Canopy Green
            Color.FromRgb(50, 145, 50),     // 11: Shaded Green
            Color.FromRgb(60, 155, 55),     // 12: Inner Foliage Green
            Color.FromRgb(85, 180, 50),     // 13: Outer Foliage Green
            Color.FromRgb(100, 195, 50),    // 14: Tip Foliage Green
            Color.FromRgb(40, 130, 45)      // 15: Deep Base Green
        ];

        return new ColorPaletteTable(bushColors);
    }

    private static List<Color> CreateDefaultPaletteColors()
    {
        return
        [
            Color.FromRgb(140, 90, 45),    // 0: Trunk Bark Brown
            Color.FromRgb(105, 135, 55),   // 1: Stem Olive Green
            Color.FromRgb(45, 175, 60),    // 2: Leaf Green
            Color.FromRgb(25, 110, 40),    // 3: Dark Forest Green
            Color.FromRgb(140, 200, 40),   // 4: Sprout Yellow Green
            Color.FromRgb(225, 45, 55),    // 5: Flower Petal Red
            Color.FromRgb(240, 130, 160),  // 6: Petal Pink
            Color.FromRgb(245, 245, 240),  // 7: Off White
            Color.FromRgb(250, 200, 30),   // 8: Sunflower Yellow
            Color.FromRgb(140, 70, 200),   // 9: Flower Purple
            Color.FromRgb(40, 140, 230),   // 10: Signal Cyan Blue
            Color.FromRgb(240, 120, 30),   // 11: Vibrant Orange
            Color.FromRgb(80, 50, 30),     // 12: Deep Bark Brown
            Color.FromRgb(160, 160, 160),  // 13: Light Gray
            Color.FromRgb(70, 70, 70),     // 14: Slate Dark Gray
            Color.FromRgb(25, 25, 25)      // 15: Black
        ];
    }

    public Color GetColor(int colorIndex)
    {
        int normalizedIndex = NormalizeIndex(colorIndex);
        return _colors[normalizedIndex];
    }

    public Material GetMaterial(int colorIndex)
    {
        int normalizedIndex = NormalizeIndex(colorIndex);
        if (_materialCache.TryGetValue(normalizedIndex, out Material? existingMaterial))
        {
            return existingMaterial;
        }

        Color targetColor = _colors[normalizedIndex];
        Material createdMaterial = CreateMaterial(targetColor);
        _materialCache[normalizedIndex] = createdMaterial;
        return createdMaterial;
    }

    public void SetColor(int colorIndex, Color color)
    {
        int normalizedIndex = NormalizeIndex(colorIndex);
        _colors[normalizedIndex] = color;
        _materialCache.Remove(normalizedIndex);
    }

    private int NormalizeIndex(int colorIndex)
    {
        if (colorIndex < 0)
        {
            int modulo = colorIndex % _colors.Count;
            return modulo < 0 ? modulo + _colors.Count : modulo;
        }

        return colorIndex % _colors.Count;
    }

    private Material CreateMaterial(Color color)
    {
        SolidColorBrush diffuseBrush = new SolidColorBrush(color);
        diffuseBrush.Freeze();
        DiffuseMaterial diffuseMaterial = new DiffuseMaterial(diffuseBrush);
        diffuseMaterial.Freeze();

        SolidColorBrush specularBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        specularBrush.Freeze();
        SpecularMaterial specularMaterial = new SpecularMaterial(specularBrush, 24.0);
        specularMaterial.Freeze();

        MaterialGroup materialGroup = new MaterialGroup();
        materialGroup.Children.Add(diffuseMaterial);
        materialGroup.Children.Add(specularMaterial);
        materialGroup.Freeze();
        return materialGroup;
    }
}