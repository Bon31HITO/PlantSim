using PlantSim.Infrastructure;
using PlantSim.LSystem;
using PlantSim.Services;
using PlantSim.Trees;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace PlantSim;

public class MainViewModel : ViewModelBase
{
    private readonly IDialogueService _dialogueService;
    private readonly IFileSelectionService _fileSelectionService;
    private readonly ChapterOnePresets _chapterOnePresetsProvider;
    private readonly TreeModelingPresets _treePresetsProvider;
    private readonly ParallelRewritingEngine _rewritingEngine;
    private readonly TurtleInterpreter _turtleInterpreter;
    private readonly LSystemGrammarParser _grammarParser;
    private readonly BezierSurfacePatchTable _patchTable;
    private readonly SynchronizationContext? _synchronizationContext;

    private readonly AonoKuniiTreeSimulator _aonoKuniiSimulator = new();
    private readonly BorchertSladePhysiologicalEngine _borchertEngine = new();
    private readonly TreeInterpretationConverter _treeConverter = new();
    private readonly DispatcherTimer _autoGrowthTimer;

    // Chapter 1 Properties
    private LSystemPresetItem? _selectedChapterOnePreset;
    private string _axiomText;
    private string _rulesText;
    private string _interpretationRulesText;
    private string _ignoredSymbolsText;
    private int _derivationStepCount;
    private double _angleInDegrees;
    private double _stepLength;
    private double _tropismSusceptibility;
    private VectorThreeDimensional _tropismVector;
    private bool _isGenerating;
    private double _progressRatio;
    private string _statusMessage;
    private Model3DGroup _renderedThreeDimensionalModel;
    private int _generatedSymbolCount;
    private int _generatedSegmentCount;
    private int _cameraResetTrigger;
    private bool _isSeedFixed;
    private int _fixedSeedValue;
    private string _growthAnalysisSummary;
    private PointCollection _growthCurvePoints;
    private CancellationTokenSource? _cancellationTokenSource;

    // Chapter 2 Properties
    private TreeSimulationPresetItem? _selectedChapterTwoSimulationPreset;
    private TreeStructureNode? _interactiveRootNode;
    private Model3DGroup _interactiveTreeModel;
    private int _interactiveStepCount;
    private string _interactiveTreeStatistics;
    private int _interactiveCameraResetTrigger;
    private bool _isAutoGrowing;

    private double _interactiveMainAngle = 25.0;
    private double _interactiveLateralAngle = 35.0;
    private double _interactiveDivergenceAngle = 137.5;
    private double _interactiveMainRatio = 0.88;
    private double _interactiveLateralRatio = 0.62;
    private double _interactivePipeExponent = 2.0;
    private double _interactiveGravitropism = 0.04;
    private double _interactiveLightAttenuation = 0.35;
    private double _interactiveShadingThreshold = 0.25;

    public IReadOnlyList<LSystemPresetItem> ChapterOnePresets { get; }
    public IReadOnlyList<TreeSimulationPresetItem> ChapterTwoSimulationPresets { get; }

    public LSystemPresetItem? SelectedChapterOnePreset
    {
        get => _selectedChapterOnePreset;
        set
        {
            SetProperty(ref _selectedChapterOnePreset, value);
            if (value != null)
            {
                AxiomText = value.AxiomText;
                RulesText = value.RulesText;
                InterpretationRulesText = value.InterpretationRulesText;
                IgnoredSymbolsText = value.IgnoredSymbolsText;
                DerivationStepCount = value.RecommendedStepCount;
                AngleInDegrees = value.DefaultAngleInDegrees;
                StepLength = value.DefaultStepLength;
                TropismSusceptibility = value.DefaultTropismSusceptibility;
                TropismVector = value.DefaultTropismVector;
                CameraResetTrigger++;
            }
        }
    }

    public TreeSimulationPresetItem? SelectedChapterTwoSimulationPreset
    {
        get => _selectedChapterTwoSimulationPreset;
        set
        {
            SetProperty(ref _selectedChapterTwoSimulationPreset, value);
            if (value != null)
            {
                InteractiveMainAngle = value.MainBranchAngleInDegrees;
                LateralBranchAngle = value.LateralBranchAngleInDegrees;
                DivergenceAngle = value.DivergenceAngleInDegrees;
                InteractiveMainRatio = value.MainBranchLengthRatio;
                InteractiveLateralRatio = value.LateralBranchLengthRatio;
                InteractivePipeExponent = value.PipeModelExponent;
                InteractiveGravitropism = value.GravitropicSusceptibility;
                InteractiveLightAttenuation = value.LightAttenuationFactor;
                InteractiveShadingThreshold = value.ShadingSurvivalThreshold;
                ExecuteResetInteractiveTree();
            }
        }
    }

    public string AxiomText { get => _axiomText; set => SetProperty(ref _axiomText, value); }
    public string RulesText { get => _rulesText; set => SetProperty(ref _rulesText, value); }
    public string InterpretationRulesText { get => _interpretationRulesText; set => SetProperty(ref _interpretationRulesText, value); }
    public string IgnoredSymbolsText { get => _ignoredSymbolsText; set => SetProperty(ref _ignoredSymbolsText, value); }
    public int DerivationStepCount { get => _derivationStepCount; set => SetProperty(ref _derivationStepCount, Math.Clamp(value, 0, 15)); }
    public double AngleInDegrees { get => _angleInDegrees; set => SetProperty(ref _angleInDegrees, value); }
    public double StepLength { get => _stepLength; set => SetProperty(ref _stepLength, Math.Max(0.1, value)); }
    public double TropismSusceptibility { get => _tropismSusceptibility; set => SetProperty(ref _tropismSusceptibility, Math.Clamp(value, 0.0, 1.0)); }
    public VectorThreeDimensional TropismVector { get => _tropismVector; set => SetProperty(ref _tropismVector, value); }
    public bool IsGenerating { get => _isGenerating; set => SetProperty(ref _isGenerating, value); }
    public double ProgressRatio { get => _progressRatio; set => SetProperty(ref _progressRatio, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public Model3DGroup RenderedThreeDimensionalModel { get => _renderedThreeDimensionalModel; set => SetProperty(ref _renderedThreeDimensionalModel, value); }
    public int GeneratedSymbolCount { get => _generatedSymbolCount; set => SetProperty(ref _generatedSymbolCount, value); }
    public int GeneratedSegmentCount { get => _generatedSegmentCount; set => SetProperty(ref _generatedSegmentCount, value); }
    public int CameraResetTrigger { get => _cameraResetTrigger; set => SetProperty(ref _cameraResetTrigger, value); }
    public bool IsSeedFixed { get => _isSeedFixed; set => SetProperty(ref _isSeedFixed, value); }
    public int FixedSeedValue { get => _fixedSeedValue; set => SetProperty(ref _fixedSeedValue, value); }
    public string GrowthAnalysisSummary { get => _growthAnalysisSummary; set => SetProperty(ref _growthAnalysisSummary, value); }
    public PointCollection GrowthCurvePoints { get => _growthCurvePoints; set => SetProperty(ref _growthCurvePoints, value); }

    public Model3DGroup InteractiveTreeModel { get => _interactiveTreeModel; set => SetProperty(ref _interactiveTreeModel, value); }
    public int InteractiveStepCount { get => _interactiveStepCount; set => SetProperty(ref _interactiveStepCount, value); }
    public string InteractiveTreeStatistics { get => _interactiveTreeStatistics; set => SetProperty(ref _interactiveTreeStatistics, value); }
    public int InteractiveCameraResetTrigger { get => _interactiveCameraResetTrigger; set => SetProperty(ref _interactiveCameraResetTrigger, value); }
    public bool IsAutoGrowing
    {
        get => _isAutoGrowing;
        set
        {
            SetProperty(ref _isAutoGrowing, value);
            RaisePropertyChanged(nameof(AutoGrowthButtonText));
        }
    }
    public string AutoGrowthButtonText => IsAutoGrowing ? "Stop Auto" : "Auto Grow";

    public double InteractiveMainAngle { get => _interactiveMainAngle; set => SetProperty(ref _interactiveMainAngle, value); }
    public double LateralBranchAngle { get => _interactiveLateralAngle; set => SetProperty(ref _interactiveLateralAngle, value); }
    public double DivergenceAngle { get => _interactiveDivergenceAngle; set => SetProperty(ref _interactiveDivergenceAngle, value); }
    public double InteractiveMainRatio { get => _interactiveMainRatio; set => SetProperty(ref _interactiveMainRatio, value); }
    public double InteractiveLateralRatio { get => _interactiveLateralRatio; set => SetProperty(ref _interactiveLateralRatio, value); }
    public double InteractivePipeExponent { get => _interactivePipeExponent; set => SetProperty(ref _interactivePipeExponent, value); }
    public double InteractiveGravitropism { get => _interactiveGravitropism; set => SetProperty(ref _interactiveGravitropism, value); }
    public double InteractiveLightAttenuation { get => _interactiveLightAttenuation; set => SetProperty(ref _interactiveLightAttenuation, value); }
    public double InteractiveShadingThreshold { get => _interactiveShadingThreshold; set => SetProperty(ref _interactiveShadingThreshold, value); }

    public RelayCommand GenerateCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ResetCameraCommand { get; }
    public RelayCommand LoadSurfacePatchCommand { get; }

    public RelayCommand StepInteractiveTreeCommand { get; }
    public RelayCommand ToggleAutoGrowthCommand { get; }
    public RelayCommand ResetInteractiveTreeCommand { get; }
    public RelayCommand ResetInteractiveCameraCommand { get; }

    public MainViewModel(IDialogueService dialogueService, IFileSelectionService fileSelectionService)
    {
        ArgumentNullException.ThrowIfNull(dialogueService);
        ArgumentNullException.ThrowIfNull(fileSelectionService);

        _dialogueService = dialogueService;
        _fileSelectionService = fileSelectionService;
        _chapterOnePresetsProvider = new ChapterOnePresets();
        _treePresetsProvider = new TreeModelingPresets();
        _rewritingEngine = new ParallelRewritingEngine();
        _turtleInterpreter = new TurtleInterpreter();
        _grammarParser = new LSystemGrammarParser();
        _patchTable = new BezierSurfacePatchTable();
        _synchronizationContext = SynchronizationContext.Current;

        ChapterOnePresets = _chapterOnePresetsProvider.GetAllPresets();
        ChapterTwoSimulationPresets = _treePresetsProvider.GetSimulationPresets();

        _axiomText = string.Empty;
        _rulesText = string.Empty;
        _interpretationRulesText = string.Empty;
        _ignoredSymbolsText = string.Empty;
        _statusMessage = "Ready to generate L-System plant.";
        _growthAnalysisSummary = "No growth data analyzed yet.";
        _growthCurvePoints = new PointCollection();
        _renderedThreeDimensionalModel = new Model3DGroup();
        _interactiveTreeModel = new Model3DGroup();
        _interactiveTreeStatistics = "Tree simulator ready. Click 'Step (+1)' or 'Auto Grow'.";
        _isSeedFixed = true;
        _fixedSeedValue = 1337;

        GenerateCommand = new RelayCommand(ExecuteGenerate, () => !IsGenerating);
        CancelCommand = new RelayCommand(ExecuteCancel, () => IsGenerating);
        ResetCameraCommand = new RelayCommand(() => CameraResetTrigger++);
        LoadSurfacePatchCommand = new RelayCommand(ExecuteLoadSurfacePatch);

        StepInteractiveTreeCommand = new RelayCommand(ExecuteStepInteractiveTree);
        ToggleAutoGrowthCommand = new RelayCommand(ExecuteToggleAutoGrowth);
        ResetInteractiveTreeCommand = new RelayCommand(ExecuteResetInteractiveTree);
        ResetInteractiveCameraCommand = new RelayCommand(() => InteractiveCameraResetTrigger++);

        _autoGrowthTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _autoGrowthTimer.Tick += (senderObject, eventArguments) =>
        {
            if (InteractiveStepCount >= 45)
            {
                IsAutoGrowing = false;
                _autoGrowthTimer.Stop();
                return;
            }
            ExecuteStepInteractiveTree();
        };

        if (ChapterOnePresets.Count > 0)
        {
            SelectedChapterOnePreset = ChapterOnePresets[0];
        }

        if (ChapterTwoSimulationPresets.Count > 0)
        {
            SelectedChapterTwoSimulationPreset = ChapterTwoSimulationPresets[0];
        }
    }

    private void ExecuteToggleAutoGrowth()
    {
        if (IsAutoGrowing)
        {
            IsAutoGrowing = false;
            _autoGrowthTimer.Stop();
        }
        else
        {
            IsAutoGrowing = true;
            _autoGrowthTimer.Start();
        }
    }

    private void ExecuteResetInteractiveTree()
    {
        if (IsAutoGrowing)
        {
            IsAutoGrowing = false;
            _autoGrowthTimer.Stop();
        }

        TreeBranchingParameters parameters = CreateActiveParameters();
        _interactiveRootNode = new TreeStructureNode(
            1,
            0,
            parameters.InitialBranchLength,
            parameters.TerminalBranchRadius,
            VectorThreeDimensional.Zero,
            TurtleOrientation.CreateDefaultTwoDimensional());

        InteractiveStepCount = 0;
        RefreshInteractiveTreeMesh(parameters);
    }

    private void ExecuteStepInteractiveTree()
    {
        if (_interactiveRootNode == null)
        {
            ExecuteResetInteractiveTree();
            return;
        }

        TreeBranchingParameters parameters = CreateActiveParameters();
        _borchertEngine.SimulateGrowthStep(
            _interactiveRootNode,
            parameters,
            InteractiveLightAttenuation,
            InteractiveShadingThreshold);

        _aonoKuniiSimulator.ApplyEnvironmentalDeformations(_interactiveRootNode, parameters);

        InteractiveStepCount++;
        RefreshInteractiveTreeMesh(parameters);
    }

    private void RefreshInteractiveTreeMesh(TreeBranchingParameters parameters)
    {
        if (_interactiveRootNode == null)
        {
            return;
        }

        (IReadOnlyList<TurtleBranchSegment> branches,
         IReadOnlyList<TurtlePolygon> foliagePolygons,
         IReadOnlyList<TurtleSphere> buds) = _treeConverter.ConvertToTurtleGeometry(_interactiveRootNode);

        ThreeDimensionalMeshGenerator meshGenerator = new ThreeDimensionalMeshGenerator();
        Model3DGroup modelGroup = meshGenerator.GenerateModelGroup(branches, foliagePolygons, buds, Array.Empty<TurtleSurface>());

        (int totalCount, int aliveCount, int prunedCount, double maximumHeight) = CollectTreeStatistics(_interactiveRootNode);

        SafeDispatch(() =>
        {
            InteractiveTreeModel = modelGroup;
            InteractiveTreeStatistics = $"Step: {InteractiveStepCount} | Branches: {totalCount:N0} (Active: {aliveCount:N0}, Pruned: {prunedCount:N0}) | Height: {maximumHeight:F1}";
        });
    }

    private static (int Total, int Alive, int Pruned, double Height) CollectTreeStatistics(TreeStructureNode rootNode)
    {
        int total = 0;
        int alive = 0;
        int pruned = 0;
        double maximumY = 0.0;

        void Traverse(TreeStructureNode node)
        {
            total++;
            if (node.IsPruned) pruned++; else alive++;
            if (node.EndPosition.Y > maximumY) maximumY = node.EndPosition.Y;
            for (int index = 0; index < node.Children.Count; index++)
            {
                Traverse(node.Children[index]);
            }
        }

        Traverse(rootNode);
        return (total, alive, pruned, maximumY);
    }

    private TreeBranchingParameters CreateActiveParameters()
    {
        return new TreeBranchingParameters
        {
            MainBranchAngleInDegrees = InteractiveMainAngle,
            LateralBranchAngleInDegrees = LateralBranchAngle,
            DivergenceAngleInDegrees = DivergenceAngle,
            MainBranchLengthRatio = InteractiveMainRatio,
            LateralBranchLengthRatio = InteractiveLateralRatio,
            PipeModelExponent = InteractivePipeExponent,
            GravitropicSusceptibility = InteractiveGravitropism,
            InitialBranchLength = 10.0,
            TerminalBranchRadius = 0.18
        };
    }

    private void ExecuteLoadSurfacePatch()
    {
        string? selectedFilePath = _fileSelectionService.SelectOpenFilePath(
            "Patch Files (*.patch;*.txt;*.dat)|*.patch;*.txt;*.dat|All Files (*.*)|*.*",
            "Load Bicubic Bezier Patch (4x4 Points)");

        if (string.IsNullOrWhiteSpace(selectedFilePath))
        {
            return;
        }

        try
        {
            const int externalPatchIdentifier = 2;
            _patchTable.RegisterFromFile(externalPatchIdentifier, selectedFilePath);
            _dialogueService.ShowInformation(
                $"Bezier patch successfully registered as Surface Kind {externalPatchIdentifier}.\nUse '~({externalPatchIdentifier}, scale)' in rules.",
                "Patch Loaded");
        }
        catch (Exception exception)
        {
            _dialogueService.ShowError($"Failed to load patch file:\n{exception.Message}", "Loading Error");
        }
    }

    private void ExecuteCancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    private async void ExecuteGenerate()
    {
        if (string.IsNullOrWhiteSpace(AxiomText))
        {
            _dialogueService.ShowError("Axiom cannot be empty.", "Grammar Error");
            return;
        }

        LindenmayerGrammar grammar;
        try
        {
            grammar = _grammarParser.ParseGrammar(AxiomText, RulesText, IgnoredSymbolsText, InterpretationRulesText);
        }
        catch (Exception exception)
        {
            _dialogueService.ShowError($"Failed to parse grammar:\n{exception.Message}", "Syntax Error");
            return;
        }

        IsGenerating = true;
        ProgressRatio = 0.0;
        StatusMessage = "Simulating derivation and growth kinetics...";
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _cancellationTokenSource.Token;

        int steps = DerivationStepCount;
        double currentAngle = AngleInDegrees;
        double currentStepLength = StepLength;
        double currentTropismSusceptibility = TropismSusceptibility;
        VectorThreeDimensional currentTropismVector = TropismVector;
        ColorPaletteTable? currentPalette = SelectedChapterOnePreset?.CustomPalette;
        int? effectiveSeed = IsSeedFixed ? FixedSeedValue : null;

        Progress<double> progressReporter = new Progress<double>(ratio =>
        {
            ProgressRatio = ratio;
        });

        try
        {
            (Word renderableWord, GrowthAnalysisResult growthAnalysis) = await _rewritingEngine.GenerateWithAnalysisAsynchronously(
                grammar,
                steps,
                effectiveSeed,
                progressReporter,
                cancellationToken);

            StatusMessage = "Interpreting turtle geometry...";
            _turtleInterpreter.DefaultAngleInDegrees = currentAngle;
            _turtleInterpreter.DefaultStepLength = currentStepLength;
            _turtleInterpreter.DefaultTropismSusceptibility = currentTropismSusceptibility;
            _turtleInterpreter.DefaultTropismVector = currentTropismVector;
            _turtleInterpreter.VerticalDirection = VectorThreeDimensional.UnitY;

            (IReadOnlyList<TurtleBranchSegment> branches,
             IReadOnlyList<TurtlePolygon> polygons,
             IReadOnlyList<TurtleSphere> spheres,
             IReadOnlyList<TurtleSurface> surfaces) = await Task.Run(
                () => _turtleInterpreter.Interpret(renderableWord),
                cancellationToken);

            StatusMessage = "Generating 3D mesh...";
            ThreeDimensionalMeshGenerator activeMeshGenerator = new ThreeDimensionalMeshGenerator(currentPalette, _patchTable);
            Model3DGroup modelGroup = await Task.Run(
                () => activeMeshGenerator.GenerateModelGroup(branches, polygons, spheres, surfaces),
                cancellationToken);

            PointCollection normalizedPoints = BuildNormalizedPlotPoints(growthAnalysis.StepRecords);

            SafeDispatch(() =>
            {
                RenderedThreeDimensionalModel = modelGroup;
                GeneratedSymbolCount = renderableWord.Count;
                GeneratedSegmentCount = branches.Count;
                GrowthAnalysisSummary = $"{growthAnalysis.SummaryDescription}";
                GrowthCurvePoints = normalizedPoints;
                StatusMessage = $"Completed (Steps: {steps}, Rendered Symbols: {renderableWord.Count:N0}, Branches: {branches.Count:N0}, Foliage: {polygons.Count:N0})";
            });
        }
        catch (OperationCanceledException)
        {
            SafeDispatch(() => StatusMessage = "Generation cancelled.");
        }
        catch (Exception exception)
        {
            SafeDispatch(() =>
            {
                StatusMessage = "Error occurred.";
                _dialogueService.ShowError(exception.Message, "Execution Error");
            });
        }
        finally
        {
            SafeDispatch(() =>
            {
                IsGenerating = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            });
        }
    }

    private static PointCollection BuildNormalizedPlotPoints(IReadOnlyList<GrowthStepRecord> records)
    {
        PointCollection collection = [];
        if (records.Count == 0)
        {
            return collection;
        }

        int maximumStep = records[^1].StepIndex;
        int maximumCount = 1;
        for (int index = 0; index < records.Count; index++)
        {
            if (records[index].TotalSymbolCount > maximumCount)
            {
                maximumCount = records[index].TotalSymbolCount;
            }
        }

        const double canvasWidth = 600.0;
        const double canvasHeight = 300.0;
        const double padding = 30.0;
        double availableWidth = canvasWidth - (padding * 2.0);
        double availableHeight = canvasHeight - (padding * 2.0);

        for (int index = 0; index < records.Count; index++)
        {
            double ratioX = maximumStep > 0 ? (double)records[index].StepIndex / maximumStep : 0.0;
            double ratioY = (double)records[index].TotalSymbolCount / maximumCount;
            double pointX = padding + (ratioX * availableWidth);
            double pointY = canvasHeight - padding - (ratioY * availableHeight);
            collection.Add(new Point(pointX, pointY));
        }

        collection.Freeze();
        return collection;
    }

    private void SafeDispatch(Action action)
    {
        if (_synchronizationContext != null && SynchronizationContext.Current != _synchronizationContext)
        {
            _synchronizationContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }
}