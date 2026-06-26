using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NestSuite.ViewModels;

namespace NestSuite.TempNest;

public class TempNestSlotViewModel : BaseViewModel, IDisposable
{
    private string _title = "";
    private string _body  = "";
    private bool _showCopyFeedback;
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

    public bool ShowCopyFeedback
    {
        get => _showCopyFeedback;
        private set => SetProperty(ref _showCopyFeedback, value);
    }

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
                    StartFeedback();
                }
            },
            _ => !string.IsNullOrEmpty(Body));
        ClearCommand = new RelayCommand(
            _ => { if (ConfirmClear?.Invoke() != false) Clear(); },
            _ => !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Body));
    }

    private void StartFeedback()
    {
        if (_disposed) return;
        _feedbackTimer.Stop();
        ShowCopyFeedback = true;
        _feedbackTimer.Start();
    }

    private void FeedbackTimer_Tick(object? sender, EventArgs e)
    {
        _feedbackTimer.Stop();
        ShowCopyFeedback = false;
    }

    public void StopFeedback()
    {
        _feedbackTimer.Stop();
        ShowCopyFeedback = false;
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
