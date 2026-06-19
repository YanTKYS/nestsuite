using System.Windows.Input;

namespace NoteNest.NestSuite.ChatNest;

/// <summary>
/// ChatNest Workspace 用の RelayCommand。参照ソース ChatNest v0.4.1 より取り込み。
/// NoteNest 本体の <see cref="NoteNest.ViewModels.RelayCommand"/> とは異なり、
/// 明示的な <see cref="RaiseCanExecuteChanged"/> を提供する（ChatNest VM の入力可否更新で使用）。
/// ChatNest モジュール内に閉じた実装として保持する。
/// </summary>
public class ChatNestRelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public ChatNestRelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}

/// <summary>
/// 型パラメータ付き ChatNest RelayCommand。参照ソース ChatNest v0.4.1 より取り込み。
/// </summary>
public class ChatNestRelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public ChatNestRelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}
