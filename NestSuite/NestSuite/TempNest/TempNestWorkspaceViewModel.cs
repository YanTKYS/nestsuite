using System.Windows.Threading;
using NestSuite.ViewModels;

namespace NestSuite.TempNest;

public class TempNestWorkspaceViewModel : BaseViewModel, IDisposable
{
    private readonly DispatcherTimer _saveTimer;
    private bool _disposed;
    private bool _hasStartedElsewhereThisSession;
    private bool _continueFromDismissed;

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
    /// SH-40: 「続きから」（<see cref="ShouldShowContinueFrom"/>）が表示される場合は、
    /// 同一領域での二重表示を避けるためAT-5は表示しない（設計レビューの排他方針）。
    /// </summary>
    public bool ShouldShowGettingStartedHint =>
        IsCompletelyEmpty && !_hasStartedElsewhereThisSession && !ShouldShowContinueFrom;

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

    // ── SH-40 (AT-1 フェーズ1): 「続きから」───────────────────────────────

    /// <summary>SH-40: recent filesリンクをクリックしたときの実処理。Shellが配線する（TN-3のPromoteRequestedと同じ委譲パターン）。</summary>
    public Action<string>? OpenContinueFromRecentRequested { get; set; }

    /// <summary>SH-40: 起動時にShellから渡されたrecent files上位項目（既定は空）。</summary>
    public IReadOnlyList<ContinueFromRecentItem> RecentContinueItems { get; private set; } = [];

    public bool HasRecentContinueItems => RecentContinueItems.Count > 0;

    /// <summary>SH-40: 起動時にShellから渡された、保持中（次回起動時に再確認予定）のdraft件数。</summary>
    public int RetainedDraftCount { get; private set; }

    public bool HasRetainedDraftCandidates => RetainedDraftCount > 0;

    /// <summary>SH-40: draft件数案内の文言。操作ボタンは持たない受動的な1行。</summary>
    public string RetainedDraftCountMessage => RetainedDraftCount switch
    {
        <= 0 => "",
        1 => "保存されていない下書きが1件あります。次回起動時に確認できます。",
        _ => $"保存されていない下書きが{RetainedDraftCount}件あります。次回起動時に確認できます。",
    };

    /// <summary>
    /// SH-40: 「続きから」を表示すべきか。recent filesまたは保持中draftが1件以上あり、
    /// かつ同一起動中に通常タブが追加されていない（<see cref="MarkContinueFromDismissed"/>が
    /// まだ呼ばれていない）場合にだけ true。TempNest自身への入力では抑止しない
    /// （設計レビューのフェーズ1方針: recent filesはTempNestへ書き始めた後も有効な情報）。
    /// </summary>
    public bool ShouldShowContinueFrom =>
        (RecentContinueItems.Count > 0 || RetainedDraftCount > 0) && !_continueFromDismissed;

    /// <summary>
    /// SH-40: 起動時に一度だけ、recent files上位項目と保持中draft件数を設定する。
    /// Shellが「通常タブの復元・作成が一切なかった起動」と判定した場合にだけ呼ぶ。
    /// </summary>
    public void SetContinueFromCandidates(IReadOnlyList<string> recentFilePaths, int retainedDraftCount)
    {
        var items = recentFilePaths
            .Select(path => new ContinueFromRecentItem(path, p => OpenContinueFromRecentRequested?.Invoke(p)))
            .ToList();
        for (var i = 0; i < items.Count; i++)
        {
            items[i].LeadingSeparator = i == 0 ? "" : " ・ ";
            items[i].AutomationId = $"TempNest.ContinueFrom.Recent{i + 1}";
        }
        RecentContinueItems = items;
        RetainedDraftCount = retainedDraftCount;
        OnPropertyChanged(nameof(RecentContinueItems));
        OnPropertyChanged(nameof(HasRecentContinueItems));
        OnPropertyChanged(nameof(RetainedDraftCount));
        OnPropertyChanged(nameof(HasRetainedDraftCandidates));
        OnPropertyChanged(nameof(RetainedDraftCountMessage));
        OnPropertyChanged(nameof(ShouldShowContinueFrom));
        OnPropertyChanged(nameof(ShouldShowGettingStartedHint));
    }

    /// <summary>
    /// SH-40: recentリンククリック時にファイル不存在等で最近使ったファイル一覧から
    /// 削除された場合、同じ項目をパネル表示からも取り除く（recent files JSONの再読込はしない）。
    /// </summary>
    public void RemoveRecentContinueItem(string filePath)
    {
        var updated = RecentContinueItems.Where(i => i.FilePath != filePath).ToList();
        if (updated.Count == RecentContinueItems.Count) return;
        RecentContinueItems = updated;
        OnPropertyChanged(nameof(RecentContinueItems));
        OnPropertyChanged(nameof(HasRecentContinueItems));
        OnPropertyChanged(nameof(ShouldShowContinueFrom));
        OnPropertyChanged(nameof(ShouldShowGettingStartedHint));
    }

    /// <summary>
    /// SH-40: 同一起動中に通常タブが1枚でも追加されたら「続きから」を再表示しないようにする、
    /// 単発ラッチ（AT-5の<see cref="MarkGettingStartedHintDismissed"/>と同じパターン、
    /// ただし別ラッチ ― TempNest自身への入力では立てない）。
    /// </summary>
    public void MarkContinueFromDismissed()
    {
        if (_continueFromDismissed) return;
        _continueFromDismissed = true;
        OnPropertyChanged(nameof(ShouldShowContinueFrom));
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
