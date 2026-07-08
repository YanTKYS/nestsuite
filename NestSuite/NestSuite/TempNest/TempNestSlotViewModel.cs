using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NestSuite.ViewModels;

namespace NestSuite.TempNest;

public class TempNestSlotViewModel : BaseViewModel, IDisposable
{
    private string _title = "";
    private string _body  = "";
    private string _feedbackMessage = "";
    private readonly DispatcherTimer _feedbackTimer;
    private bool _disposed;

    public string Title
    {
        get => _title;
        set { if (SetProperty(ref _title, value)) Changed?.Invoke(); }
    }

    public string Body
    {
        get => _body;
        set { if (SetProperty(ref _body, value)) Changed?.Invoke(); }
    }

    /// <summary>v2.16.5 SH-28: コピー/クリア完了などの一時通知の文言。空文字なら非表示。</summary>
    public string FeedbackMessage => _feedbackMessage;

    public bool HasFeedback => !string.IsNullOrEmpty(_feedbackMessage);

    public event Action? Changed;

    public ICommand CopyBodyCommand { get; }
    public ICommand ClearCommand    { get; }

    // Set by the View to show a confirmation dialog before clearing a non-empty slot.
    // Return true to proceed, false to cancel. When null, clears without confirmation.
    public Func<bool>? ConfirmClear { get; set; }

    public TempNestSlotViewModel()
    {
        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _feedbackTimer.Tick += FeedbackTimer_Tick;

        CopyBodyCommand = new RelayCommand(
            _ =>
            {
                if (!string.IsNullOrEmpty(Body))
                {
                    Clipboard.SetText(Body);
                    ShowFeedback("コピーしました");
                }
            },
            _ => !string.IsNullOrEmpty(Body));
        ClearCommand = new RelayCommand(
            _ =>
            {
                if (ConfirmClear?.Invoke() != false)
                {
                    Clear();
                    ShowFeedback("クリアしました");
                }
            },
            _ => !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Body));
    }

    /// <summary>v2.16.5 SH-28: コピー/クリア完了などの一時通知を表示する。1つ前の通知は上書きする。</summary>
    private void ShowFeedback(string message)
    {
        if (_disposed) return;
        _feedbackTimer.Stop();
        SetFeedbackMessage(message);
        _feedbackTimer.Start();
    }

    private void FeedbackTimer_Tick(object? sender, EventArgs e)
    {
        _feedbackTimer.Stop();
        SetFeedbackMessage("");
    }

    public void StopFeedback()
    {
        _feedbackTimer.Stop();
        SetFeedbackMessage("");
    }

    private void SetFeedbackMessage(string message)
    {
        if (!SetProperty(ref _feedbackMessage, message, nameof(FeedbackMessage))) return;
        OnPropertyChanged(nameof(HasFeedback));
    }

    private void Clear()
    {
        Title = "";
        Body  = "";
    }

    public TempNestSlot ToSlot()
        => new() { Title = Title, Body = Body, UpdatedAt = DateTimeOffset.Now };

    public void LoadFromSlot(TempNestSlot slot)
    {
        _title = slot.Title;
        _body  = slot.Body;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Body));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _feedbackTimer.Stop();
        _feedbackTimer.Tick -= FeedbackTimer_Tick;
        Changed = null;
    }
}
