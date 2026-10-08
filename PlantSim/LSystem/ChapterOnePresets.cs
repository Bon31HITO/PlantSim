using System.Collections.Generic;

namespace PlantSim.LSystem;

internal class ChapterOnePresets
{
    private readonly LSystemGrammarParser _grammarParser = new();

    public IReadOnlyList<LSystemPresetItem> GetAllPresets()
    {
        return
        [
            CreateAnabaenaCatenata(),
            CreateThreeDimensionalBush(),
            CreateGravitropicTree(),
            CreateMonopodialTree(),
            CreateSympodialTree(),
            CreateTernaryTree(),
            CreateLilacInflorescence(),
            CreateFlowerWithSurfacePetals(),
            CreateContextSensitiveSignalTree(),
            CreateBranchPruningDemo(),
            CreatePhyllotacticSignalTree(),
            CreateParametricBranching(),
            CreateBracketedPlantA(),
            CreateBracketedPlantB(),
            CreateBracketedPlantC(),
            CreateBracketedPlantD(),
            CreateBracketedPlantE(),
            CreateBracketedPlantF(),
            CreateStochasticPlant(),
            CreateKochSnowflake(),
            CreateQuadraticKochIsland(),
            CreateDragonCurve(),
            CreateSierpinskiGasket(),
            CreateThreeDimensionalHilbertCurve()
        ];
    }

    public LSystemPresetItem CreateAnabaenaCatenata()
    {
        // ABOP Section 1.10.2: Figure 1.35 Anabaena catenata with heterocysts
        // F(s, m): s = 細胞齢 (1~3), m = 抑制物質レベル (0~4)
        // H(s): ヘテロシスト (異質細胞)
        string axiomText = "-(1)F(1, 0)F(1, 0)H(1)F(1, 0)F(1, 0)";

        // 2L-system 側方抑制・モルフォゲン減衰・周期的分化規則
        // ヘテロシスト近傍は抑制レベルが高く維持され、距離が離れると減衰して m = 0 に到達した成熟細胞が新ヘテロシストへ分化
        string rulesText = """
            H(s) -> H(s)

            H(sh) < F(s, m) > F(sr, mr) : s < 3 -> F(s + 1, 3)
            H(sh) < F(s, m) > F(sr, mr) : s >= 3 -> F(1, 3)F(1, 3)
            F(sl, ml) < F(s, m) > H(sh) : s < 3 -> F(s + 1, 3)
            F(sl, ml) < F(s, m) > H(sh) : s >= 3 -> F(1, 3)F(1, 3)
            H(sl) < F(s, m) > H(sr) : s < 3 -> F(s + 1, 3)
            H(sl) < F(s, m) > H(sr) : s >= 3 -> F(1, 3)F(1, 3)

            H(sh) < F(s, m) : s < 3 -> F(s + 1, 3)
            H(sh) < F(s, m) : s >= 3 -> F(1, 3)F(1, 3)
            F(s, m) > H(sh) : s < 3 -> F(s + 1, 3)
            F(s, m) > H(sh) : s >= 3 -> F(1, 3)F(1, 3)

            F(sl, ml) < F(s, m) > F(sr, mr) : s >= 3 && ml <= 0 && mr <= 0 && m <= 0 -> H(1)
            F(s, m) > F(sr, mr) : s >= 3 && mr <= 0 && m <= 0 -> H(1)
            F(sl, ml) < F(s, m) : s >= 3 && ml <= 0 && m <= 0 -> H(1)

            F(sl, ml) < F(s, m) > F(sr, mr) : s < 3 -> F(s + 1, max(0, max(ml, mr) - 1))
            F(sl, ml) < F(s, m) > F(sr, mr) : s >= 3 -> F(1, max(0, max(ml, mr) - 1))F(1, max(0, max(ml, mr) - 1))

            F(s, m) > F(sr, mr) : s < 3 -> F(s + 1, max(0, mr - 1))
            F(s, m) > F(sr, mr) : s >= 3 -> F(1, max(0, mr - 1))F(1, max(0, mr - 1))

            F(sl, ml) < F(s, m) : s < 3 -> F(s + 1, max(0, ml - 1))
            F(sl, ml) < F(s, m) : s >= 3 -> F(1, max(0, ml - 1))F(1, max(0, ml - 1))

            F(s, m) : s < 3 -> F(s + 1, 0)
            F(s, m) : s >= 3 -> F(1, 0)F(1, 0)
            """;

        // 幾何解釈規則（Homomorphism）：
        // 円柱の前半で膨らみ後半で窄まるテーパー構造により、完全なビーズ状カプセル（樽型細胞）を形成
        // 抑制細胞（若草色 '4）、分化可能成熟細胞（深緑色 '2）、ヘテロシスト（大型黄金球 '8）を表現
        string interpretationRulesText = """
            interpretation:
            F(s, m) : m > 0 -> !(0.40)'4F(s * 0.9, 1.15)F(s * 0.9, 0.40)!(0.40)'4@(0.42)
            F(s, m) : m <= 0 -> !(0.40)'2F(s * 0.9, 1.15)F(s * 0.9, 0.40)!(0.40)'2@(0.42)
            H(s) -> !(0.55)'8F(0.9, 1.85)F(0.9, 0.55)!(0.55)'8@(1.05)
            """;

        string ignoredSymbolsText = "+ - & ^ / \\ ' , ! ; @";

        return new LSystemPresetItem(
            "Anabaena Catenata (2L)",
            "Section 1.10.2",
            "Heterocyst spacing and lateral inhibition 2L-system (ABOP Figure 1.35)",
            8,
            0.0,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText, interpretationRulesText),
            interpretationRulesText);
    }

    public LSystemPresetItem CreateThreeDimensionalBush()
    {
        string axiomText = "A";
        string rulesText = """
            A -> [&FL!A]/////'[&FL!A]///////'[&FL!A]
            F -> S/////F
            S -> FL
            L -> [''^^ {-f+f+f-|-f+f+f}]
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "3D Bush",
            "Section 1.6",
            "A plant modeled using an L-system with foliage (ABOP Figure 1.25, n=7, d=22.5)",
            7,
            22.5,
            2.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText),
            "",
            0.0,
            null,
            ColorPaletteTable.CreateBushPalette());
    }

    public LSystemPresetItem CreateBranchPruningDemo()
    {
        string axiomText = "A(0)";
        string rulesText = """
            #define MaxAge 4
            A(t) : t < MaxAge -> F(10)[+B(0)][-B(0)]A(t + 1)
            A(t) : t >= MaxAge -> F(10)
            B(t) : t < 2 -> F(5)B(t + 1)
            B(t) : t >= 2 -> %
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Branch Pruning (%)",
            "Section 1.10.4",
            "Apical dominance and branch pruning with cut symbol %",
            5,
            30.0,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateGravitropicTree()
    {
        string axiomText = "!(2.2)A";
        string rulesText = """
            A -> F[&(35)$!B]/(137.5)F[&(35)$!B]/(137.5)A
            B -> F[+$!B][-$!B]
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Gravitropic Tree",
            "Section 1.5.2",
            "3D Tree with Tropism (Figure 1.26)",
            5,
            22.5,
            6.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText),
            "",
            0.045,
            new VectorThreeDimensional(0.0, -1.0, 0.0));
    }

    public LSystemPresetItem CreateMonopodialTree()
    {
        string axiomText = "!(2.5)A";
        string rulesText = """
            A -> !(0.85)F[&(38)/(137.5)!B]F[&(38)/(137.5)!B]/(137.5)A
            B -> !(0.8)F[+(28)!B][-(28)!B]
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Monopodial Tree",
            "Section 1.6 / 1.10",
            "Tree with strong apical trunk dominance (Figure 1.29 / 1.40a)",
            5,
            24.0,
            5.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText),
            "",
            0.02,
            new VectorThreeDimensional(0.0, -1.0, 0.0));
    }

    public LSystemPresetItem CreateSympodialTree()
    {
        string axiomText = "!(2.0)A";
        string rulesText = """
            A -> !(0.82)F[&(32)'1/(60)A][&(32)'2/(-120)A]
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Sympodial Tree",
            "Section 1.6 / 1.10",
            "Bifurcating deciduous crown structure (Figure 1.29 / 1.40b)",
            6,
            26.0,
            6.5,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateTernaryTree()
    {
        string axiomText = "!(2.0)A";
        string rulesText = """
            A -> !(0.75)F[&(32)/(0)!A][&(32)/(120)!A][&(32)/(240)!A]
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Ternary Tree",
            "Section 1.6.2",
            "Symmetrical ternary branching tree (Figure 1.28)",
            4,
            30.0,
            6.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateLilacInflorescence()
    {
        string axiomText = "!(1.2)I";
        string rulesText = """
            I -> F[&(45)/(90)!(0.8)B][&(45)/(-90)!(0.8)B]/(137.5)I
            B -> F'6~(1, 2.5)/(90)'6~(1, 2.5)/(90)'6~(1, 2.5)/(90)'6~(1, 2.5)'8@(0.4)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Lilac Inflorescence",
            "Section 1.10.3",
            "Panicle inflorescence with Bezier flower petals (Figure 1.42)",
            5,
            25.0,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateFlowerWithSurfacePetals()
    {
        string axiomText = "!(0.6)'1F(10)[-(50)/(20)'2F(5)][+(50)/(20)'2F(5)]F(8)P";
        string rulesText = """
            P -> [&(60)'5~(1, 8.0)]/(72)[&(60)'5~(1, 8.0)]/(72)[&(60)'5~(1, 8.0)]/(72)[&(60)'5~(1, 8.0)]/(72)[&(60)'5~(1, 8.0)]f(0.4)'8@(1.0)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Flower with Bezier Petals",
            "Section 1.10.3 / App C",
            "Bicubic Bezier petal surface patches with 5-fold symmetry (Figure 1.41/1.43)",
            1,
            72.0,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateContextSensitiveSignalTree()
    {
        string axiomText = "S[-F][+F]";
        string rulesText = """
            S < F -> '10S[-F][+F]
            S -> '1F
            """;
        string ignoredSymbolsText = "+ - & ^ / \\ ' ,";

        return new LSystemPresetItem(
            "Acropetal Signal Tree",
            "Section 1.8",
            "Acropetal signal propagation matching branches (Figure 1.30 / 1.32)",
            8,
            25.7,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreatePhyllotacticSignalTree()
    {
        string axiomText = "!(1.8)S";
        string rulesText = """
            S -> !(0.85)F[&'10F]/(137.5)S
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Phyllotactic Signal Tree",
            "Section 1.10.2",
            "Spiral phyllotaxis with golden divergence angle (Figure 1.37)",
            8,
            25.0,
            3.5,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateParametricBranching()
    {
        string axiomText = "!(1.2)A(12, 1)";
        string rulesText = """
            A(length, order) : order > 0 -> F(length)[+A(length * 0.7, order - 1)][-A(length * 0.7, order - 1)]
            A(length, order) : order <= 0 -> F(length)
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Parametric Branching",
            "Section 1.10",
            "Dynamic math expression evaluation (Figure 1.39 / 1.40)",
            7,
            30.0,
            1.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateBracketedPlantA()
    {
        string axiomText = "F";
        string rulesText = "F -> F[+F]F[-F]F";
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Bracketed Plant A",
            "Section 1.6.3",
            "Bracketed Plant A (Figure 1.24a, n=5, d=25.7)",
            5,
            25.7,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateBracketedPlantB()
    {
        string axiomText = "F";
        string rulesText = "F -> F[+F]F[-F][F]";
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Bracketed Plant B",
            "Section 1.6.3",
            "Bracketed Plant B (Figure 1.24b, n=5, d=20.0)",
            5,
            20.0,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateBracketedPlantC()
    {
        string axiomText = "F";
        string rulesText = "F -> FF-[-F+F+F]+[+F-F-F]";
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Bracketed Plant C",
            "Section 1.6.3",
            "Bracketed Plant C (Figure 1.24c, n=4, d=22.5)",
            4,
            22.5,
            5.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateBracketedPlantD()
    {
        string axiomText = "X";
        string rulesText = """
            X -> F[+X]F[-X]+X
            F -> FF
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Bracketed Plant D",
            "Section 1.6.3",
            "Bracketed Plant D (Figure 1.24d, n=7, d=20.0)",
            7,
            20.0,
            2.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateBracketedPlantE()
    {
        string axiomText = "X";
        string rulesText = """
            X -> F[+X][-X]FX
            F -> FF
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Bracketed Plant E",
            "Section 1.6.3",
            "Bracketed Plant E (Figure 1.24e, n=7, d=25.7)",
            7,
            25.7,
            2.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateBracketedPlantF()
    {
        string axiomText = "X";
        string rulesText = """
            X -> F-[[X]+X]+F[+FX]-X
            F -> FF
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Bracketed Plant F",
            "Section 1.6.3",
            "Bracketed Plant F (Figure 1.24f, n=5, d=22.5)",
            5,
            22.5,
            3.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateStochasticPlant()
    {
        string axiomText = "F";
        string rulesText = """
            F : 0.33 -> F[+F]F[-F]F
            F : 0.33 -> F[+F]F
            F : 0.34 -> F[-F]F
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Stochastic Plant",
            "Section 1.7",
            "Stochastic Plant (Figure 1.27)",
            5,
            25.7,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateKochSnowflake()
    {
        string axiomText = "F--F--F";
        string rulesText = "F -> F+F--F+F";
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Koch Snowflake",
            "Section 1.1 / 1.3",
            "Koch Snowflake (Figure 1.1)",
            4,
            60.0,
            4.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateQuadraticKochIsland()
    {
        string axiomText = "F-F-F-F";
        string rulesText = "F -> F+F-F-FF+F+F-F";
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Quadratic Koch Island",
            "Section 1.3",
            "Quadratic Koch Curve (Figure 1.6 / 1.7a)",
            3,
            90.0,
            6.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateDragonCurve()
    {
        string axiomText = "F";
        string rulesText = """
            F -> F+G+
            G -> -F-G
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Dragon Curve",
            "Section 1.4",
            "Dragon Curve (Figure 1.10a)",
            10,
            90.0,
            5.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateSierpinskiGasket()
    {
        string axiomText = "F";
        string rulesText = """
            F -> G-F-G
            G -> F+G+F
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "Sierpinski Gasket",
            "Section 1.4",
            "Sierpinski Gasket (Figure 1.14)",
            6,
            60.0,
            5.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }

    public LSystemPresetItem CreateThreeDimensionalHilbertCurve()
    {
        string axiomText = "X";
        string rulesText = """
            X -> ^\XF^/XFX-F^//XFX&F+//XFX-F/X-
            """;
        string ignoredSymbolsText = "";

        return new LSystemPresetItem(
            "3D Hilbert Curve",
            "Section 1.5",
            "Space-filling 3D Hilbert curve (Figure 1.23)",
            3,
            90.0,
            5.0,
            axiomText,
            rulesText,
            ignoredSymbolsText,
            () => _grammarParser.ParseGrammar(axiomText, rulesText, ignoredSymbolsText));
    }
}