using System.Windows.Threading;
using NestSuite.ViewModels;

namespace NestSuite.TempNest;

public class TempNestWorkspaceViewModel : BaseViewModel, IDisposable
{
    private readonly DispatcherTimer _saveTimer;
    private bool _disposed;
    private bool _hasStartedElsewhereThisSession;

    public TempNestSlotViewModel Slot1 { get; } = new();
    public TempNestSlotViewModel Slot2 { get; } = new();
    public TempNestSlotViewModel Slot3 { get; } = new();
    public TempNestSlotViewModel Slot4 { get; } = new();

    /// <summary>AT-5: 4スロットすべてが空か。既存の各スロット空判定をそのまま合成する。</summary>
    public bool IsCompletelyEmpty => Slot1.IsEmpty && Slot2.IsEmpty && Slot3.IsEmpty && Slot4.IsEmpty;

    /// <summary>
    /// AT-5: 初回相当の空状態でだけ真になる、表示専用の導出プロパティ。永続化しない。
    /// 「初回」を直接判定するのではなく、TempNestが完全に空、かつ同一起動中に
    /// 再開対象（通常タブ・session・draft・起動引数等）または本スロットへの入力が
    /// まだ一度もないことから導出する。
    /// </summary>
    public bool ShouldShowGettingStartedHint => IsCompletelyEmpty && !_hasStartedElsewhereThisSession;

    /// <summary>
    /// AT-5: 一行ガイドを同一起動中は再表示しないようにする。永続化はしない
    /// （アプリ再起動で解除される）。一度真になったら偽へは戻さない単発ラッチ。
    /// TempNest自身への入力（<see cref="OnSlotChanged"/>）と、Shell側が把握する
    /// 通常タブ・session・draft・起動引数等の状態の両方から呼ばれる。
    /// public にしているのは、Shell側から呼べるようにするためと、
    /// UIウィンドウを介さずに単体テストできるようにするため（ID-15と同じ理由）。
    /// </summary>
    public void MarkGettingStartedHintDismissed()
    {
        if (_hasStartedElsewhereThisSession) return;
        _hasStartedElsewhereThisSession = true;
        OnPropertyChanged(nameof(ShouldShowGettingStartedHint));
    }

    private string _contentFontFamily = "Yu Gothic UI";

    /// <summary>
    /// L22: 各スロットのタイトル欄・本文欄に適用する Workspace 共通フォント種類。
    /// NestSuite の UI 設定（ui-settings.json の WorkspaceEditorFontFamily）駆動の表示専用値。
    /// TempNest はファイル型 Workspace ではないため <see cref="TempNestStoreService"/> の
    /// 保存データ（<see cref="SaveNow"/>）には含めない。変更しても <see cref="OnSlotChanged"/>
    /// を経由しないため自動保存タイマーも起動しない。
    /// </summary>
    public string ContentFontFamily
    {
        get => _contentFontFamily;
        set => SetProperty(ref _contentFontFamily, value);
    }

    public TempNestWorkspaceViewModel()
    {
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _saveTimer.Tick += SaveTimer_Tick;

        foreach (var slot in Slots)
            slot.Changed += OnSlotChanged;

        Load();
    }

    private IEnumerable<TempNestSlotViewModel> Slots
        => new[] { Slot1, Slot2, Slot3, Slot4 };

    private void OnSlotChanged()
    {
        // AT-5: スロットへの入力・クリアいずれも「利用者がここで作業を始めた」印であり、
        // 同一起動中はガイドを再表示しない（クリアして再び空になっても再表示しない）。
        MarkGettingStartedHintDismissed();
        OnPropertyChanged(nameof(IsCompletelyEmpty));
        OnPropertyChanged(nameof(ShouldShowGettingStartedHint));

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveTimer_Tick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        SaveNow();
    }

    private void Load()
    {
        var data = TempNestStoreService.Load();
        Slot1.LoadFromSlot(data[0]);
        Slot2.LoadFromSlot(data[1]);
        Slot3.LoadFromSlot(data[2]);
        Slot4.LoadFromSlot(data[3]);
    }

    public void SaveNow()
    {
        _saveTimer.Stop();
        TempNestStoreService.Save(new[]
        {
            Slot1.ToSlot(), Slot2.ToSlot(), Slot3.ToSlot(), Slot4.ToSlot()
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _saveTimer.Stop();
        _saveTimer.Tick -= SaveTimer_Tick;
        foreach (var slot in Slots)
        {
            slot.Changed -= OnSlotChanged;
            slot.Dispose();
        }
    }
}
