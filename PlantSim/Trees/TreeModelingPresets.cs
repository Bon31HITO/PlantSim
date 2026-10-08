using PlantSim.LSystem;
using System.Collections.Generic;

namespace PlantSim.Trees;

public class TreeModelingPresets
{
    private readonly LSystemGrammarParser _grammarParser = new();

    public IReadOnlyList<TreeSimulationPresetItem> GetSimulationPresets()
    {
        return
        [
            new TreeSimulationPresetItem(
                "Monopodial Tree (Fig 2.6)",
                "単軸分枝型樹木：強い頂端優勢と円錐形の均整のとれた樹冠 (α1=25°, α2=35°, r1=0.88, r2=0.62)",
                25.0, 35.0, 137.5, 0.88, 0.62, 2.0, 0.040, 0.35, 0.25),
            new TreeSimulationPresetItem(
                "Sympodial Tree (Fig 2.8)",
                "仮軸分枝型樹木：二叉分岐を繰り返す広葉樹型の丸い樹冠 (α1=34°, α2=34°, r1=0.76, r2=0.76)",
                34.0, 34.0, 90.0, 0.76, 0.76, 2.0, 0.025, 0.30, 0.20),
            new TreeSimulationPresetItem(
                "Ternary Tree (Fig 2.7)",
                "三叉分枝型樹木：3次元空間に規則正しく対称展開する樹冠 (α1=32°, α2=32°, r1=0.68, r2=0.68)",
                32.0, 32.0, 120.0, 0.68, 0.68, 2.0, 0.020, 0.30, 0.20),
            new TreeSimulationPresetItem(
                "Aono-Kunii Weeping Tree (Fig 2.9)",
                "枝垂れ樹木：自重モーメントと断面二次モーメントによる力学的たわみ変形",
                25.0, 42.0, 137.5, 0.86, 0.65, 2.2, 0.065, 0.35, 0.25),
            new TreeSimulationPresetItem(
                "Dense Canopy & Self-Pruning (Fig 2.11)",
                "高密度樹冠と自己剪定：上層の相互遮蔽による日陰枝の自然脱落と淘汰",
                28.0, 38.0, 137.5, 0.85, 0.65, 2.0, 0.035, 0.55, 0.30),
            new TreeSimulationPresetItem(
                "Leonardo Allometric Tree (Fig 2.12)",
                "レオナルド樹幹保存則：断面積保存則 Δ=2.49 による太さの配分",
                26.0, 38.0, 137.5, 0.72, 0.72, 2.49, 0.030, 0.35, 0.25)
        ];
    }

    public IReadOnlyList<LSystemPresetItem> GetAllPresets()
    {
        return
        [
            CreateLeonardoBasipetalTree(),
            CreateHondaMonopodialTree(),
            CreateHondaSympodialTree(),
            CreateHondaTernaryTree(),
            CreateAonoKuniiWeepingTree(),
            CreateBorchertSladeApicalDominanceTree(),
            CreateBorchertSladePrunedTree(),
            CreateLeonardoDaVinciAllometricTree()
        ];
    }

    public LSystemPresetItem CreateLeonardoBasipetalTree()
    {
        string axiomText = "!(0.1)A(4)";
        string rulesText = """
            #define MainAngle 26.0
            #define LateralAngle 38.0
            #define DivergenceAngle 137.5

            A(order) : order > 0 -> I(8.0, 0.0)[&(LateralAngle)/(DivergenceAngle)A(order - 1)]&(MainAngle)A(order - 1)
            A(order) : order <= 0 -> I(4.0, 0.2)'2@(0.5)

            I(length, width) > [ I(lateralLength, lateralWidth) ] I(mainLength, mainWidth) : width < 0.05 && lateralWidth > 0.05 && mainWidth > 0.05 -> !(pow(pow(lateralWidth, 2.0) + pow(mainWidth, 2.0), 0.5))I(length, pow(pow(lateralWidth, 2.0) + pow(mainWidth, 2.0), 0.5))
            """;
        string ignoredSymbolsText = "+ - & ^ / \\ ' ,";

        return new LSystemPresetItem(
            "Leonardo Basipetal 2L-Tree",
            "Chapter 2: Section 2.5 (pp.67-68)",
            "Pure 2L-system basipetal diameter accumulation without procedural tree traversal",
            8,
            26.0,
            8.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateHondaMonopodialTree()
    {
        string axiomText = "!(1.6)A(12, 6)";
        string rulesText = """
            #define MainRatio 0.88
            #define LateralRatio 0.62
            #define MainAngle 25.0
            #define LateralAngle 35.0
            #define DivergenceAngle 137.5

            A(length, order) : order > 0 -> !(order * 0.25 + 0.08)F(length)[&(LateralAngle)/(DivergenceAngle)A(length * LateralRatio, order - 1)]&(MainAngle)A(length * MainRatio, order - 1)
            A(length, order) : order <= 0 -> !(0.08)F(length)'2@(0.45)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Honda Monopodial Tree",
            "Chapter 2: Section 2.2",
            "Monopodial tree structure with apical trunk dominance (ABOP Figure 2.6)",
            6,
            25.0,
            12.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText),
            "",
            0.015,
            new VectorThreeDimensional(0.0, -1.0, 0.0));
    }

    public LSystemPresetItem CreateHondaSympodialTree()
    {
        string axiomText = "!(1.8)A(10, 6)";
        string rulesText = """
            #define ContractionRatio 0.76
            #define BranchAngle 34.0
            #define DivergenceAngle 90.0

            A(length, order) : order > 0 -> !(order * 0.28 + 0.1)F(length)[&(BranchAngle)/(DivergenceAngle)A(length * ContractionRatio, order - 1)][&(-BranchAngle)/(-DivergenceAngle)A(length * ContractionRatio, order - 1)]
            A(length, order) : order <= 0 -> !(0.1)F(length)'2@(0.5)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Honda Sympodial Tree",
            "Chapter 2: Section 2.2",
            "Sympodial bifurcating deciduous crown structure (ABOP Figure 2.8)",
            6,
            34.0,
            10.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateHondaTernaryTree()
    {
        string axiomText = "!(2.0)A(11, 4)";
        string rulesText = """
            #define Ratio 0.68
            #define BranchAngle 32.0

            A(length, order) : order > 0 -> !(order * 0.4 + 0.1)F(length)[&(BranchAngle)/(0)A(length * Ratio, order - 1)][&(BranchAngle)/(120)A(length * Ratio, order - 1)][&(BranchAngle)/(240)A(length * Ratio, order - 1)]
            A(length, order) : order <= 0 -> !(0.1)F(length)'2@(0.6)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Honda Ternary Tree",
            "Chapter 2: Section 2.2",
            "Symmetrical ternary branching tree (ABOP Figure 2.7)",
            4,
            32.0,
            11.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateAonoKuniiWeepingTree()
    {
        string axiomText = "!(2.2)A(12, 5)";
        string rulesText = """
            A(length, order) : order > 0 -> !(order * 0.35 + 0.1)F(length)[&(42)$/(137.5)B(length * 0.72, order - 1)]/(137.5)A(length * 0.88, order - 1)
            A(length, order) : order <= 0 -> !(0.1)F(length)
            B(length, order) : order > 0 -> !(order * 0.25 + 0.08)F(length)[&(30)/(85)B(length * 0.75, order - 1)]B(length * 0.85, order - 1)
            B(length, order) : order <= 0 -> !(0.08)F(length)'2@(0.4)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Aono-Kunii Weeping Tree",
            "Chapter 2: Section 2.3",
            "Physical bending deformation under gravity load (ABOP Figure 2.9)",
            5,
            30.0,
            12.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText),
            "",
            0.055,
            new VectorThreeDimensional(0.0, -1.0, 0.0));
    }

    public LSystemPresetItem CreateBorchertSladeApicalDominanceTree()
    {
        string axiomText = "!(1.8)A(10, 100)";
        string rulesText = """
            #define Threshold 18.0
            #define LossRate 0.85

            A(length, flux) : flux >= Threshold -> !(flux * 0.015 + 0.12)F(length)[&(36)/(137.5)A(length * 0.7, flux * 0.35)]/(137.5)A(length * 0.9, flux * LossRate)
            A(length, flux) : flux < Threshold && flux >= 5.0 -> !(0.12)F(length)'2@(0.45)
            A(length, flux) : flux < 5.0 -> %
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Borchert-Slade Signal Tree",
            "Chapter 2: Section 2.5",
            "Apical dominance and hormonal flux transport (ABOP Figure 2.10)",
            7,
            35.0,
            10.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateBorchertSladePrunedTree()
    {
        string axiomText = "!(2.0)A(9, 120)";
        string rulesText = """
            #define VitalThreshold 22.0

            A(length, flux) : flux >= VitalThreshold -> !(flux * 0.016 + 0.1)F(length)[&(45)/(120)B(length * 0.65, flux * 0.28)][&(45)/(-120)B(length * 0.65, flux * 0.28)]A(length * 0.86, flux * 0.88)
            A(length, flux) : flux < VitalThreshold -> !(0.15)F(length)'2@(0.5)
            B(length, flux) : flux >= 15.0 -> !(flux * 0.014 + 0.08)F(length)[&(28)B(length * 0.7, flux * 0.4)]B(length * 0.75, flux * 0.5)
            B(length, flux) : flux < 15.0 -> %
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Borchert-Slade Pruned Tree",
            "Chapter 2: Section 2.5",
            "Natural self-pruning and branch shedding due to low flux (ABOP Figure 2.11)",
            6,
            40.0,
            9.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateLeonardoDaVinciAllometricTree()
    {
        string axiomText = "!(2.4)A(12, 5)";
        string rulesText = """
            #define Ratio 0.72
            #define Angle1 26.0
            #define Angle2 38.0

            A(length, order) : order > 0 -> !(order * 0.45 + 0.15)F(length)[&(Angle2)/(137.5)A(length * Ratio, order - 1)]&(Angle1)A(length * Ratio, order - 1)
            A(length, order) : order <= 0 -> !(0.12)F(length)'2@(0.4)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Leonardo da Vinci Pipe Tree",
            "Chapter 2: Section 2.5",
            "Cross-sectional area conservation based on Leonardo's postulate (ABOP Figure 2.12)",
            5,
            30.0,
            12.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }
}