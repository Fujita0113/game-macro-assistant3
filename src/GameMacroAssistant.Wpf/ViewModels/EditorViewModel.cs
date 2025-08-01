using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Wpf.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly Stack<EditorAction> _undoStack = new();
    private readonly Stack<EditorAction> _redoStack = new();
    private const double DEFAULT_COMPOUND_OPERATION_THRESHOLD = 2.0;
    
    [ObservableProperty]
    private Macro? _currentMacro;
    
    [ObservableProperty]
    private Step? _selectedStep;
    
    [ObservableProperty]
    private double _compoundOperationThresholdSeconds = DEFAULT_COMPOUND_OPERATION_THRESHOLD;
    
    [ObservableProperty]
    private bool _canUndo;
    
    [ObservableProperty]
    private bool _canRedo;
    
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    public ObservableCollection<StepViewModel> Steps { get; } = new();
    
    public EditorViewModel()
    {
        PropertyChanged += OnPropertyChanged;
    }
    
    public void LoadMacro(Macro macro)
    {
        CurrentMacro = macro;
        Steps.Clear();
        
        foreach (var step in macro.Steps.OrderBy(s => s.Order))
        {
            Steps.Add(new StepViewModel(step));
        }
        
        StatusMessage = $"Loaded macro: {macro.Name}";
        ClearUndoRedoStacks();
    }
    
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        
        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
        
        UpdateUndoRedoStates();
        StatusMessage = "Undo completed";
    }
    
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        
        var action = _redoStack.Pop();
        action.Execute();
        _undoStack.Push(action);
        
        UpdateUndoRedoStates();
        StatusMessage = "Redo completed";
    }
    
    [RelayCommand]
    private void DeleteStep()
    {
        if (SelectedStep == null) return;
        
        var stepViewModel = Steps.FirstOrDefault(s => s.Step == SelectedStep);
        if (stepViewModel == null) return;
        
        var action = new DeleteStepAction(Steps, stepViewModel);
        ExecuteAction(action);
        
        StatusMessage = "Step deleted";
    }
    
    [RelayCommand]
    private void MoveStepUp()
    {
        if (SelectedStep == null) return;
        
        var index = Steps.ToList().FindIndex(s => s.Step == SelectedStep);
        if (index <= 0) return;
        
        var action = new MoveStepAction(Steps, index, index - 1);
        ExecuteAction(action);
        
        StatusMessage = "Step moved up";
    }
    
    [RelayCommand]
    private void MoveStepDown()
    {
        if (SelectedStep == null) return;
        
        var index = Steps.ToList().FindIndex(s => s.Step == SelectedStep);
        if (index < 0 || index >= Steps.Count - 1) return;
        
        var action = new MoveStepAction(Steps, index, index + 1);
        ExecuteAction(action);
        
        StatusMessage = "Step moved down";
    }
    
    [RelayCommand]
    private void DuplicateStep()
    {
        if (SelectedStep == null) return;
        
        // TODO: Implement step duplication with deep copy
        StatusMessage = "Step duplicated";
    }
    
    [RelayCommand]
    private void EditStepParameters()
    {
        if (SelectedStep == null) return;
        
        // TODO: Open parameter editor dialog
        StatusMessage = "Opening parameter editor...";
    }
    
    [RelayCommand]
    private void SaveMacro()
    {
        if (CurrentMacro == null) return;
        
        // Update macro steps from view models
        CurrentMacro.Steps.Clear();
        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i].Step;
            step.Order = i;
            CurrentMacro.Steps.Add(step);
        }
        
        CurrentMacro.ModifiedAt = DateTime.UtcNow;
        
        // TODO: Save to file system
        StatusMessage = "Macro saved";
    }
    
    public void HandleStepDrop(int sourceIndex, int targetIndex)
    {
        if (sourceIndex == targetIndex) return;
        
        var action = new MoveStepAction(Steps, sourceIndex, targetIndex);
        ExecuteAction(action);
        
        StatusMessage = "Step reordered";
    }
    
    private void ExecuteAction(EditorAction action)
    {
        action.Execute();
        _undoStack.Push(action);
        _redoStack.Clear();
        
        UpdateUndoRedoStates();
    }
    
    private void UpdateUndoRedoStates()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }
    
    private void ClearUndoRedoStacks()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoStates();
    }
    
    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CompoundOperationThresholdSeconds))
        {
            // TODO: Validate range (0.5s - 5.0s as per R-009)
            if (CompoundOperationThresholdSeconds < 0.5) CompoundOperationThresholdSeconds = 0.5;
            if (CompoundOperationThresholdSeconds > 5.0) CompoundOperationThresholdSeconds = 5.0;
        }
    }
}

public class StepViewModel : ObservableObject
{
    public Step Step { get; }
    
    public string DisplayName => Step.Type switch
    {
        StepType.Mouse => $"Mouse {((MouseStep)Step).Action}",
        StepType.Keyboard => $"Key {((KeyboardStep)Step).VirtualKeyCode}",
        StepType.Delay => $"Wait {((DelayStep)Step).DelayMs}ms",
        StepType.Conditional => "Image Match",
        _ => "Unknown"
    };
    
    public string Description => Step.Description ?? GenerateDescription();
    
    public StepViewModel(Step step)
    {
        Step = step;
    }
    
    private string GenerateDescription()
    {
        return Step switch
        {
            MouseStep mouse => $"{mouse.Action} at ({mouse.AbsolutePosition.X}, {mouse.AbsolutePosition.Y})",
            KeyboardStep keyboard => $"Key {keyboard.VirtualKeyCode} {keyboard.Action}",
            DelayStep delay => $"Wait for {delay.DelayMs}ms",
            ConditionalStep conditional => $"Check image match (threshold: {conditional.MatchThreshold:P0})",
            _ => "Step"
        };
    }
}

public abstract class EditorAction
{
    public abstract void Execute();
    public abstract void Undo();
}

public class DeleteStepAction : EditorAction
{
    private readonly ObservableCollection<StepViewModel> _steps;
    private readonly StepViewModel _stepToDelete;
    private readonly int _originalIndex;
    
    public DeleteStepAction(ObservableCollection<StepViewModel> steps, StepViewModel stepToDelete)
    {
        _steps = steps;
        _stepToDelete = stepToDelete;
        _originalIndex = steps.IndexOf(stepToDelete);
    }
    
    public override void Execute()
    {
        _steps.Remove(_stepToDelete);
    }
    
    public override void Undo()
    {
        _steps.Insert(_originalIndex, _stepToDelete);
    }
}

public class MoveStepAction : EditorAction
{
    private readonly ObservableCollection<StepViewModel> _steps;
    private readonly int _sourceIndex;
    private readonly int _targetIndex;
    
    public MoveStepAction(ObservableCollection<StepViewModel> steps, int sourceIndex, int targetIndex)
    {
        _steps = steps;
        _sourceIndex = sourceIndex;
        _targetIndex = targetIndex;
    }
    
    public override void Execute()
    {
        var item = _steps[_sourceIndex];
        _steps.RemoveAt(_sourceIndex);
        _steps.Insert(_targetIndex, item);
    }
    
    public override void Undo()
    {
        var item = _steps[_targetIndex];
        _steps.RemoveAt(_targetIndex);
        _steps.Insert(_sourceIndex, item);
    }
}