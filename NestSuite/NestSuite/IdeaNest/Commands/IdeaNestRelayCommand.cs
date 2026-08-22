using System;
using System.Windows.Input;

namespace NestSuite.IdeaNest.Commands;

/// <summary>
/// IdeaNest Workspace 用の RelayCommand。IdeaNest モジュール内に閉じた実装として保持する。
/// Shell の <see cref="NestSuite.ViewModels.RelayCommand"/> と重複して見えるが、Workspace が
/// Shell 実装へ依存しないための意図的な分離であり、重複だけを理由に統一しない。
/// </summary>
public class IdeaNestRelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public IdeaNestRelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public IdeaNestRelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : new Predicate<object?>(_ => canExecute()))
    {
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
