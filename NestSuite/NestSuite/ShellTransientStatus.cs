using System.Windows.Threading;

namespace NestSuite;

/// <summary>
/// v2.16.5 SH-28: 保存・エクスポート・コピーなど主要操作の完了を、UI の邪魔にならない
/// 一時的な文言（既定 2 秒程度で自動的に消える）で伝えるための小さな共通 helper。
/// モーダル通知にはせず、呼び出し側が用意したテキスト表示先（ステータスバー等）を更新するだけに留める。
/// UI thread からの利用を前提とする。
/// </summary>
public sealed class ShellTransientStatus : IDisposable
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(2);

    private readonly Action<string> _setText;
    private readonly Action _onExpired;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    /// <summary>現在、一時通知を表示中かどうか。呼び出し側が通知中の上書きを避けたい場合に参照する。</summary>
    public bool IsActive { get; private set; }

    /// <param name="setText">通知文言（および満了後の空文字）を反映する処理。</param>
    /// <param name="onExpired">
    /// 表示時間経過後に呼ばれる処理。省略時は setText("") で単純にクリアする。
    /// ステータスバーを通常表示へ戻す等、setText("") 以上の後処理が必要な場合に指定する。
    /// </param>
    public ShellTransientStatus(Action<string> setText, Action? onExpired = null)
    {
        _setText = setText;
        _onExpired = onExpired ?? (() => setText(""));
        _timer = new DispatcherTimer { Interval = DefaultDuration };
        _timer.Tick += Timer_Tick;
    }

    /// <summary>
    /// 一時通知を表示する。1つ前の通知が表示中でも新しい通知で上書きする。
    /// </summary>
    public void Show(string message, TimeSpan? duration = null)
    {
        if (_disposed) return;
        _timer.Stop();
        _setText(message);
        IsActive = true;
        _timer.Interval = duration ?? DefaultDuration;
        _timer.Start();
    }

    /// <summary>
    /// 表示時間経過を待たずに、満了時と同じ後始末（onExpired 呼び出し含む）を行う。
    /// テストや、明示的に通知を打ち切りたい呼び出し元から使う。
    /// </summary>
    public void Clear()
    {
        if (!IsActive) return;
        _timer.Stop();
        IsActive = false;
        _onExpired();
    }

    private void Timer_Tick(object? sender, EventArgs e) => Clear();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
    }
}
