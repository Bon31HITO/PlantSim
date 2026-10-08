namespace PlantSim.Infrastructure;

using System;
using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Action _executeAction;
    private readonly Func<bool>? _canExecutePredicate;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action executeAction, Func<bool>? canExecutePredicate = null)
    {
        ArgumentNullException.ThrowIfNull(executeAction);
        _executeAction = executeAction;
        _canExecutePredicate = canExecutePredicate;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecutePredicate == null || _canExecutePredicate();
    }

    public void Execute(object? parameter)
    {
        _executeAction();
    }
}