using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NestSuite.IdeaNest.Commands;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.Views;
using NestSuite.Services;

namespace NestSuite.IdeaNest.ViewModels;

public class IdeaNestWorkspaceViewModel : IdeaNestViewModelBase, IDisposable
{
    /// <summary>ID-6: 直前1操作だけを対象とするUndoの操作種別。</summary>
    private enum IdeaNestUndoOperation
    {
        Delete,
        ArchiveStateChange,
    }

    /// <summary>
    /// ID-6: 直前1操作分のUndo情報。メモリ上だけに保持し、保存対象（<see cref="BuildWorkspaceForSave"/>）
    /// には含めない。<see cref="Card"/>は削除・変更対象と同一インスタンス（新しいIDは発行しない）。
    /// </summary>
    private sealed class IdeaNestUndoState
    {
        public required IdeaNestUndoOperation Operation { get; init; }
        public required IdeaCardViewModel Card { get; init; }
        public int Index { get; init; }
        public bool PreviousArchived { get; init; }
    }

    private Workspace _workspace = new();
    private CardOperationsService _cardOps = null!;
    private TagManagementService _tagMgmt = null!;
    private DispatcherTimer? _statusClearTimer;
    private string _statusMessage = string.Empty;
    private readonly IdeaNestWorkspaceUiService _ui;
    private bool _hasChanges;
    private bool _disposed;
    private IdeaNestUndoState? _undoState;

    public CardDisplayViewModel CardDisplay { get; }
    public FilterViewModel Filter { get; }
    public TagPanelViewModel TagPanel { get; }

    public ObservableCollection<IdeaCardViewModel> AllCards { get; } = new();
    public ObservableCollection<IdeaCardViewModel> VisibleCards { get; } = new();
    public ObservableCollection<string> AvailableTags { get; } = new();

    /// <summary>
    /// Full, unfiltered tag list. Used by the tag management window so that
    /// rename/delete operations always cover every tag regardless of the side
    /// panel's TagSearch filter.
    /// </summary>
    public ObservableCollection<TagItemViewModel> TagItems => TagPanel.AllItems;

    /// <summary>
    /// TagSearch-filtered tag list. Used by the side panel ListBox.
    /// </summary>
    public ObservableCollection<TagItemViewModel> VisibleTagPanelItems => TagPanel.VisibleItems;

    public ObservableCollection<SortOptionViewModel> SortOptions { get; } = new()
    {
        new SortOptionViewModel("UpdatedDesc", "更新日時順"),
        new SortOptionViewModel("CreatedDesc", "作成日時順"),
        new SortOptionViewModel("TitleAsc",    "タイトル順"),
        new SortOptionViewModel("Shuffle",     "シャッフル"),
    };

    public ObservableCollection<ColorFilterItemViewModel> ColorItems { get; } = new()
    {
        new ColorFilterItemViewModel("white",  "白"),
        new ColorFilterItemViewModel("yellow", "黄"),
        new ColorFilterItemViewModel("green",  "緑"),
        new ColorFilterItemViewModel("blue",   "青"),
        new ColorFilterItemViewModel("pink",   "ピンク"),
        new ColorFilterItemViewModel("purple", "紫"),
        new ColorFilterItemViewModel("orange", "オレンジ"),
        new ColorFilterItemViewModel("gray",   "グレー"),
    };

    // ── Filter state: forward to Filter sub-ViewModel ────────────────────────

    public string SearchText      { get => Filter.SearchText;    set => Filter.SearchText = value; }
    public string SelectedTag     { get => Filter.SelectedTag;   set => Filter.SelectedTag = value; }
    public string SelectedColor   { get => Filter.SelectedColor; set => Filter.SelectedColor = value; }
    public ArchiveFilterMode ArchiveFilterMode { get => Filter.ArchiveFilterMode; set => Filter.ArchiveFilterMode = value; }
    public bool   ShowArchived    { get => Filter.ShowArchived;  set => Filter.ShowArchived = value; }
    public bool   IsArchiveActiveOnly => Filter.IsArchiveActiveOnly;
    public bool   IsArchiveIncludeArchived => Filter.IsArchiveIncludeArchived;
    public bool   IsArchiveArchivedOnly => Filter.IsArchiveArchivedOnly;
    public bool   HasActiveFilter => Filter.HasActiveFilter;

    // ── Tag panel: forward to TagPanel sub-ViewModel ─────────────────────────

    public bool IsTagPanelOpen
    {
        get => TagPanel.IsTagPanelOpen;
        set => TagPanel.IsTagPanelOpen = value;
    }

    public string TagPanelButtonLabel => TagPanel.TagPanelButtonLabel;
    public string TagPanelButtonTip   => TagPanel.TagPanelButtonTip;

    // ── Card display: forward to CardDisplay sub-ViewModel ────────────────────

    public string CardSize       { get => CardDisplay.CardSize;       set => CardDisplay.CardSize = value; }
    public string CardHeightMode { get => CardDisplay.CardHeightMode; set => CardDisplay.CardHeightMode = value; }
    public string SortMode       { get => CardDisplay.SortMode;       set => CardDisplay.SortMode = value; }

    public double CardWidth     => CardDisplay.CardWidth;
    public double CardHeight    => CardDisplay.CardHeight;
    public double CardMinHeight => CardDisplay.CardMinHeight;
    public double CardMaxHeight => CardDisplay.CardMaxHeight;

    public bool IsCardSizeSmall  => CardDisplay.IsCardSizeSmall;
    public bool IsCardSizeMedium => CardDisplay.IsCardSizeMedium;
    public bool IsCardSizeLarge  => CardDisplay.IsCardSizeLarge;
    public bool IsCardHeightFixed => CardDisplay.IsCardHeightFixed;
    public bool IsCardHeightAuto  => CardDisplay.IsCardHeightAuto;
    public bool IsShuffleMode     => CardDisplay.IsShuffleMode;
    public int  BodyPreviewMaxLines => CardDisplay.BodyPreviewMaxLines;

    public WorkspaceSettings Settings => _workspace.Settings;

    private string _contentFontFamily = "Yu Gothic UI";

    /// <summary>
    /// L22: カード本文・カード編集欄に適用する Workspace 共通フォント種類。
    /// NestSuite の UI 設定（ui-settings.json の WorkspaceEditorFontFamily）駆動の表示専用値であり、
    /// カードの枠・ボタン・タグ・ツールバーなどの UI フォントには適用しない。
    /// .ideanest（<see cref="BuildWorkspaceForSave"/>）へは保存しないため、変更しても <see cref="HasChanges"/> は立たない。
    /// </summary>
    public string ContentFontFamily
    {
        get => _contentFontFamily;
        set => SetField(ref _contentFontFamily, value);
    }

    public bool HasChanges
    {
        get => _hasChanges;
        private set => SetField(ref _hasChanges, value);
    }

    /// <summary>ID-6: 直前1操作（削除・アーカイブ・アーカイブ解除）をUndoできる状態か。</summary>
    public bool CanUndo => _undoState != null;

    public ICommand AddIdeaCommand { get; }
    public ICommand EditIdeaCommand { get; }
    public ICommand PreviewIdeaCommand { get; }
    public IdeaNestRelayCommand RandomPreviewCommand { get; }
    public ICommand DeleteIdeaCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand ToggleArchiveCommand { get; }
    public ICommand SetArchiveFilterModeCommand { get; }
    public ICommand SelectTagCommand { get; }
    public ICommand ClearTagCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ClearColorCommand { get; }
    public ICommand ManageTagsCommand { get; }
    public IdeaNestRelayCommand ExportMarkdownCommand { get; }
    public ICommand CopyCardMarkdownCommand { get; }
    public IdeaNestRelayCommand CopyAllMarkdownCommand { get; }
    public ICommand ExportNoteNestCommand { get; }
    public ICommand CopyNoteNestCommand { get; }
    public ICommand ToggleTagPanelCommand { get; }
    public ICommand SetCardSizeCommand { get; }
    public ICommand SetCardHeightModeCommand { get; }
    public ICommand ReshuffleCommand { get; }
    public IdeaNestRelayCommand UndoCommand { get; }

    public string DisplayName => _workspace.WorkspaceName;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public int TotalCount => AllCards.Count;
    public int VisibleCount => VisibleCards.Count;
    public int VisibleCardCount => VisibleCount;

    private IdeaCardViewModel? _selectedCard;

    /// <summary>
    /// ID-15: 新規カード作成後の位置フィードバック専用の一時選択。
    /// 複数選択（ID-5）の先行実装ではなく、保存対象にも含めない単一選択。
    /// </summary>
    public IdeaCardViewModel? SelectedCard
    {
        get => _selectedCard;
        private set
        {
            if (ReferenceEquals(_selectedCard, value)) return;
            if (_selectedCard != null) _selectedCard.IsSelected = false;
            _selectedCard = value;
            if (_selectedCard != null) _selectedCard.IsSelected = true;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// ID-15: 新規カード作成直後の一時的なスクロール要求。View がコンテナ生成後に
    /// BringIntoView を実行するために購読する。一覧の再構築だけでは発火しない。
    /// </summary>
    public event EventHandler<IdeaCardViewModel>? ScrollRequested;

    public string CountText
    {
        get
        {
            if (VisibleCount == TotalCount)
            {
                return $"{TotalCount}件";
            }
            return $"{VisibleCount}件 / 全{TotalCount}件";
        }
    }

    public bool ShowEmptyState => VisibleCount == 0;

    public string EmptyStateTitle
    {
        get
        {
            if (TotalCount == 0) return "まだアイデアがありません";
            if (HasActiveFilter) return "条件に一致するカードがありません";
            return "表示できるカードがありません";
        }
    }

    public string EmptyStateMessage
    {
        get
        {
            var hasActiveFilter = HasActiveFilter;
            return (TotalCount, hasActiveFilter, ArchiveFilterMode) switch
            {
                (0, _, _) => "右下の「＋」ボタン (または Ctrl+Shift+N) から最初のアイデアを追加できます。",
                (_, true, _) => "検索語やタグを変更してください。",
                (_, _, ArchiveFilterMode.ArchivedOnly) => "アーカイブ済みカードはありません。",
                _ => "アーカイブを含める表示に切り替えると、アーカイブ済みカードが見られます。",
            };
        }
    }

    public IdeaNestWorkspaceViewModel() : this(new IdeaNestWorkspaceUiService()) { }

    public IdeaNestWorkspaceViewModel(IdeaNestWorkspaceUiService ui)
    {
        _ui = ui;
        CardDisplay = new CardDisplayViewModel(RefreshVisible, OnCardDisplayChanged);
        CardDisplay.PropertyChanged += OnSubVmPropertyChanged;

        Filter = new FilterViewModel(RefreshVisible, OnFilterChanged);
        Filter.PropertyChanged += OnSubVmPropertyChanged;

        TagPanel = new TagPanelViewModel(OnTagPanelChanged, tag => SelectedTag = tag);
        TagPanel.PropertyChanged += OnSubVmPropertyChanged;

        // ID-10: 表示中カードのMarkdown出力（保存・コピー）。表示中カードが1件もない場合は無効。
        ExportMarkdownCommand   = new IdeaNestRelayCommand(_ => ExportVisibleCardsAsMarkdown(), _ => VisibleCards.Count > 0);
        CopyCardMarkdownCommand = new IdeaNestRelayCommand(_ => { });
        CopyAllMarkdownCommand  = new IdeaNestRelayCommand(_ => CopyVisibleCardsAsMarkdown(), _ => VisibleCards.Count > 0);
        // No-op export commands（NoteNest形式は今回のID-10対象外）
        ExportNoteNestCommand   = new IdeaNestRelayCommand(_ => { });
        CopyNoteNestCommand     = new IdeaNestRelayCommand(_ => { });

        AddIdeaCommand         = new IdeaNestRelayCommand(_ => AddIdea());
        EditIdeaCommand        = new IdeaNestRelayCommand(p => PreviewIdea(p as IdeaCardViewModel));
        PreviewIdeaCommand     = new IdeaNestRelayCommand(p => PreviewIdea(p as IdeaCardViewModel));
        RandomPreviewCommand   = new IdeaNestRelayCommand(_ => RandomPreview(), _ => VisibleCards.Count > 0);
        DeleteIdeaCommand      = new IdeaNestRelayCommand(p => DeleteIdea(p as IdeaCardViewModel));
        TogglePinCommand       = new IdeaNestRelayCommand(p => TogglePin(p as IdeaCardViewModel));
        ToggleArchiveCommand   = new IdeaNestRelayCommand(p => ToggleArchive(p as IdeaCardViewModel));
        SetArchiveFilterModeCommand = new IdeaNestRelayCommand(p => SetArchiveFilterMode(p));
        SelectTagCommand       = new IdeaNestRelayCommand(p => TagPanel.SelectTag(p as string ?? string.Empty));
        ClearTagCommand        = new IdeaNestRelayCommand(_ => SelectedTag = string.Empty);
        ClearSearchCommand     = new IdeaNestRelayCommand(_ => SearchText = string.Empty);
        ClearColorCommand      = new IdeaNestRelayCommand(_ => SelectedColor = string.Empty);
        ManageTagsCommand      = new IdeaNestRelayCommand(_ => OpenTagManagement());
        ToggleTagPanelCommand  = new IdeaNestRelayCommand(_ => TagPanel.Toggle());
        SetCardSizeCommand     = new IdeaNestRelayCommand(p => CardDisplay.CardSize = p as string ?? "medium");
        SetCardHeightModeCommand = new IdeaNestRelayCommand(p => CardDisplay.CardHeightMode = p as string ?? "fixed");
        ReshuffleCommand       = new IdeaNestRelayCommand(_ =>
            CardDisplay.Reshuffle(AllCards.Where(c => !c.IsPinned).Select(c => c.Id)));
        UndoCommand            = new IdeaNestRelayCommand(_ => ExecuteUndo(), _ => CanUndo);

        _cardOps = CreateCardOps();
        _tagMgmt = new TagManagementService(
            AllCards,
            getSelectedTag: () => SelectedTag,
            setSelectedTag: t => SelectedTag = t,
            onDirty: MarkDirty,
            onRefreshTags: RefreshTags,
            onRefreshVisible: RefreshVisible);
    }

    private CardOperationsService CreateCardOps() => new(
        _workspace.Ideas,
        AllCards,
        MarkDirty,
        RefreshTags,
        RefreshVisible);

    private void OnSubVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => OnPropertyChanged(e.PropertyName);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_statusClearTimer != null)
        {
            _statusClearTimer.Stop();
            _statusClearTimer.Tick -= StatusClearTimer_Tick;
            _statusClearTimer = null;
        }
        _undoState = null; // ID-6: Workspaceを閉じた後に古いUndoが実行されないようにする
        CardDisplay.PropertyChanged -= OnSubVmPropertyChanged;
        Filter.PropertyChanged     -= OnSubVmPropertyChanged;
        TagPanel.PropertyChanged   -= OnSubVmPropertyChanged;
        ScrollRequested = null;
    }

    private void RaiseCountAndEmptyStateChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(VisibleCardCount));
        OnPropertyChanged(nameof(HasActiveFilter));
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
    }

    // ── カード操作 ────────────────────────────────────────────────────────────

    private void AddIdea()
    {
        var dlg = new PreviewIdeaWindow(
            onCommitAdd: vm =>
            {
                var draft = new Idea();
                vm.ApplyTo(draft);
                return _cardOps.CommitAdd(draft);
            },
            onCommitEdit: c => _cardOps.CommitEdit(c),
            contentFontFamily: ContentFontFamily)
        {
            Owner = _ui.Owner,
        };
        dlg.ShowDialog();

        ApplyNewCardPositionFeedback(dlg.AddedCard);
    }

    /// <summary>
    /// ID-15: 新規カード作成後の位置フィードバック。作成カードは <see cref="PreviewIdeaWindow.AddedCard"/>
    /// （<see cref="CardOperationsService.CommitAdd"/> の戻り値そのもの）で明示的に識別し、
    /// タイトルや一覧位置からは推測しない。表示対象判定は既存の <see cref="VisibleCards"/>
    /// （<see cref="RefreshVisible"/> 済み）をそのまま基準にし、フィルター条件を再判定しない。
    /// UI（PreviewIdeaWindow のモーダル表示）を介さずに単体テストできるよう public にしている。
    /// </summary>
    public void ApplyNewCardPositionFeedback(IdeaCardViewModel? created)
    {
        // キャンセル（内容未入力）または作成失敗時は、選択・スクロール・通知のいずれも行わない。
        if (created == null) return;

        if (VisibleCards.Contains(created))
        {
            SelectedCard = created;
            ScrollRequested?.Invoke(this, created);
        }
        else
        {
            // 表示対象外: フィルター・検索・アーカイブ条件を変更せず、その旨だけ短く通知する。
            ShowStatus("カードを追加しました。現在の絞り込み条件では表示されていません");
        }
    }

    private void PreviewIdea(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var cards = VisibleCards.ToList();
        var index = cards.IndexOf(card);
        if (index < 0) index = 0;

        var dlg = new PreviewIdeaWindow(
            cards,
            index,
            onCommitEdit: c => _cardOps.CommitEdit(c),
            contentFontFamily: ContentFontFamily)
        {
            Owner = _ui.Owner,
        };
        dlg.ShowDialog();
    }

    private void RandomPreview()
    {
        if (VisibleCards.Count == 0) return;
        var card = VisibleCards[new Random().Next(VisibleCards.Count)];
        PreviewIdea(card);
    }

    private void DeleteIdea(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var ok = IdeaConfirmWindow.ShowOkCancel(
            _ui.Owner,
            "このカードを削除しますか？",
            $"「{card.DisplayTitle}」を削除します。削除直後なら「元に戻す」で取り消せますが、" +
            "他の操作を行うと元に戻せません。\n\n" +
            "不要な場合は、削除ではなくアーカイブ (📥) も検討してください。",
            primaryText: "削除",
            cancelText: "キャンセル");
        if (ok != ConfirmResult.Primary) return;
        CommitDeleteWithUndo(card);
    }

    /// <summary>
    /// ID-6: 削除確認ダイアログ（WPF Window）を介さずに、削除＋Undo登録の一連を単体テストできるよう
    /// 分離した部分。<see cref="DeleteIdea"/>は確認後にこのメソッドを呼ぶ。
    /// </summary>
    public void CommitDeleteWithUndo(IdeaCardViewModel card)
    {
        var index = AllCards.IndexOf(card);
        _cardOps.CommitDelete(card);
        RegisterDeletedCard(card, index);
        ShowStatus("削除しました");
    }

    private void TogglePin(IdeaCardViewModel? card)
    {
        if (card == null) return;
        _cardOps.TogglePin(card);
    }

    private void SetArchiveFilterMode(object? parameter)
    {
        if (parameter is ArchiveFilterMode mode)
        {
            ArchiveFilterMode = mode;
            return;
        }

        if (parameter is string text && Enum.TryParse(text, ignoreCase: false, out ArchiveFilterMode parsed))
        {
            ArchiveFilterMode = parsed;
        }
    }

    private void ToggleArchive(IdeaCardViewModel? card)
    {
        if (card == null) return;
        var previousArchived = card.IsArchived;
        _cardOps.ToggleArchive(card);
        RegisterArchiveChange(card, previousArchived);
        ShowStatus(card.IsArchived ? "アーカイブしました" : "アーカイブを解除しました");
    }

    // ── ID-6: 削除・アーカイブのUndo ─────────────────────────────────────────

    private void RegisterDeletedCard(IdeaCardViewModel card, int index)
    {
        _undoState = new IdeaNestUndoState
        {
            Operation = IdeaNestUndoOperation.Delete,
            Card = card,
            Index = index,
        };
        RaiseUndoChanged();
    }

    private void RegisterArchiveChange(IdeaCardViewModel card, bool previousArchived)
    {
        _undoState = new IdeaNestUndoState
        {
            Operation = IdeaNestUndoOperation.ArchiveStateChange,
            Card = card,
            PreviousArchived = previousArchived,
        };
        RaiseUndoChanged();
    }

    private void ClearUndo()
    {
        if (_undoState == null) return;
        _undoState = null;
        RaiseUndoChanged();
    }

    private void RaiseUndoChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        UndoCommand.RaiseCanExecuteChanged();
    }

    private void ExecuteUndo()
    {
        var state = _undoState;
        if (state == null) return;
        // 単発Undo。Redoは保持しないため、実行前に必ずクリアする。
        _undoState = null;

        switch (state.Operation)
        {
            case IdeaNestUndoOperation.Delete:
                // 対象カードが既に一覧へ存在する場合は重複させず、静かに終了する。
                if (!AllCards.Contains(state.Card))
                {
                    _cardOps.RestoreDeleted(state.Card, state.Index);
                    ShowStatus("カードを元に戻しました");
                    SelectRestoredCardIfVisible(state.Card);
                }
                break;

            case IdeaNestUndoOperation.ArchiveStateChange:
                // 対象カードが既に削除・入れ替え等で存在しない場合は、安全に対応付けられないため何もしない。
                if (AllCards.Contains(state.Card))
                {
                    _cardOps.SetArchived(state.Card, state.PreviousArchived);
                    ShowStatus("アーカイブを元に戻しました");
                    SelectRestoredCardIfVisible(state.Card);
                }
                break;
        }

        RaiseUndoChanged();
    }

    /// <summary>
    /// ID-6: ID-15と同じ「表示中の場合だけ選択・スクロールする」既存パターンを再利用する。
    /// フィルター条件を変更してまで対象カードを強制表示することはしない。
    /// </summary>
    private void SelectRestoredCardIfVisible(IdeaCardViewModel card)
    {
        if (!VisibleCards.Contains(card)) return;
        SelectedCard = card;
        ScrollRequested?.Invoke(this, card);
    }

    // ── 状態管理・設定同期 ────────────────────────────────────────────────────

    public void MarkDirty()
    {
        HasChanges = true;
    }

    public void SetOwnerResolver(Func<Window?> resolver) => _ui.SetOwnerResolver(resolver);

    private void OnFilterChanged()
    {
        Filter.SyncToSettings(_workspace.Settings);
        MarkDirty();
    }

    private void OnTagPanelChanged()
    {
        TagPanel.SyncToSettings(_workspace.Settings);
        MarkDirty();
    }

    private void OnCardDisplayChanged()
    {
        CardDisplay.SyncToSettings(_workspace.Settings);
        MarkDirty();
    }

    public void LoadFromWorkspace(Workspace workspace)
    {
        _workspace = workspace ?? new Workspace();
        HasChanges = false;
        ReloadFromWorkspace();
        OnPropertyChanged(nameof(DisplayName));
    }

    /// <summary>
    /// SH-36b: 下書き復元専用。通常 Open の clean 契約は維持しつつ、
    /// 復元後の無題タブが次 tick の下書き候補になるよう dirty として扱う。
    /// </summary>
    public void LoadFromWorkspaceAsDraft(Workspace workspace)
    {
        LoadFromWorkspace(workspace);
        MarkDirty();
    }

    public Workspace BuildWorkspaceForSave()
    {
        SyncSettings();
        return new Workspace
        {
            Version = IdeaNestSchema.CurrentVersion,
            WorkspaceName = _workspace.WorkspaceName,
            Ideas = _workspace.Ideas.ToList(),
            Settings = new WorkspaceSettings
            {
                CardSize = _workspace.Settings.CardSize,
                CardHeightMode = _workspace.Settings.CardHeightMode,
                SortMode = _workspace.Settings.SortMode,
            },
        };
    }

    public void MarkSaved() => HasChanges = false;

    public void SyncSettings()
    {
        Filter.SyncToSettings(_workspace.Settings);
        TagPanel.SyncToSettings(_workspace.Settings);
        CardDisplay.SyncToSettings(_workspace.Settings);
    }

    private void ReloadFromWorkspace()
    {
        // ID-15: 再構築で AllCards が総入れ替えされるため、旧カードへの選択・スクロール要求を残さない。
        SelectedCard = null;
        // ID-6: 別ファイル読み込み・再読込時は、古いUndo情報（別の正本コレクションを指す）を残さない。
        ClearUndo();
        _cardOps = CreateCardOps();
        AllCards.Clear();
        foreach (var idea in _workspace.Ideas)
        {
            AllCards.Add(new IdeaCardViewModel(idea));
        }
        Filter.LoadFromSettings(_workspace.Settings);
        TagPanel.LoadFromSettings(_workspace.Settings);
        CardDisplay.LoadFromSettings(_workspace.Settings);
        RefreshTags();
        RefreshVisible();
    }

    private void RefreshTags()
    {
        var tagItems = TagSyncService.ComputeTagItems(AllCards);
        AvailableTags.Clear();
        foreach (var item in tagItems) AvailableTags.Add(item.Name);
        TagPanel.SetAllItems(tagItems);
    }

    private void OpenTagManagement()
    {
        var dlg = new TagManagementWindow(this)
        {
            Owner = _ui.Owner,
        };
        dlg.ShowDialog();
    }

    // ── ステータス表示 ────────────────────────────────────────────────────────

    private void ShowStatus(string message)
    {
        StatusMessage = message;
        if (_statusClearTimer != null)
        {
            _statusClearTimer.Stop();
            _statusClearTimer.Tick -= StatusClearTimer_Tick;
        }
        _statusClearTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };
        _statusClearTimer.Tick += StatusClearTimer_Tick;
        _statusClearTimer.Start();
    }

    private void StatusClearTimer_Tick(object? sender, EventArgs e)
    {
        StatusMessage = string.Empty;
        _statusClearTimer?.Stop();
    }

    // ── タグ管理（TagManagementWindow 用） ────────────────────────────────────

    public void RenameTag(string oldName, string newName) => _tagMgmt.RenameTag(oldName, newName);

    public void DeleteTag(string tagName) => _tagMgmt.DeleteTag(tagName);

    // ── クリップボード・ファイルインポート ────────────────────────────────────

    // ── ID-10: 表示中カードのMarkdown出力 ─────────────────────────────────────

    /// <summary>
    /// ID-10: 表示中カード（<see cref="VisibleCards"/>）を、現在の表示順のまま Markdown へ変換し
    /// クリップボードへコピーする。フィルタ・ソート・アーカイブ表示条件の再判定はせず、
    /// 表示中コレクションをそのままスナップショット化するだけの読み取り専用操作（dirty化しない）。
    /// </summary>
    private void CopyVisibleCardsAsMarkdown()
    {
        var cards = VisibleCards.ToList();
        if (cards.Count == 0)
        {
            ShowStatus("出力するカードがありません");
            return;
        }

        var markdown = IdeaNestMarkdownExporter.Build(cards);
        try
        {
            _ui.SetClipboardText(markdown);
            ShowStatus($"表示中の{cards.Count}件をMarkdownとしてコピーしました");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("IdeaNestMarkdownCopy", ex, "IdeaNest");
            _ui.ShowError("Markdownのコピーに失敗しました。");
        }
    }

    /// <summary>
    /// ID-10: 表示中カードを Markdown ファイルとして保存する。保存先はSaveFileDialogで選択させ、
    /// キャンセル時は何もしない（通知もErrorLog記録もしない）。読み取り専用操作でdirty化しない。
    /// </summary>
    private void ExportVisibleCardsAsMarkdown()
    {
        var cards = VisibleCards.ToList();
        if (cards.Count == 0)
        {
            ShowStatus("出力するカードがありません");
            return;
        }

        var path = _ui.ShowSaveMarkdownDialog("IdeaNest-export.md");
        if (path == null) return;

        var markdown = IdeaNestMarkdownExporter.Build(cards);
        try
        {
            AtomicFileWriter.WriteAllText(path, markdown, Encoding.UTF8);
            ShowStatus($"表示中の{cards.Count}件をMarkdownへ保存しました");
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("IdeaNestMarkdownExport", ex, "IdeaNest", path);
            _ui.ShowError("Markdownの保存に失敗しました。");
        }
    }

    public bool PasteAsNewCard()
    {
        string text;
        try
        {
            text = _ui.GetClipboardText() ?? string.Empty;
        }
        catch
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(text)) return false;
        var ok = _cardOps.CommitAddFromText(text);
        if (ok) ShowStatus("クリップボードのテキストからカードを作成しました");
        return ok;
    }

    public int CreateCardsFromFiles(IEnumerable<string> filePaths)
    {
        var created = 0;
        var errors = new List<string>();
        foreach (var path in filePaths)
        {
            try
            {
                var body = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var title = Path.GetFileNameWithoutExtension(path);
                if (_cardOps.CommitAddFromFileContent(title, body)) created++;
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            _ui.ShowWarning("次のファイルを読み込めませんでした:\n\n" + string.Join("\n", errors));
        }
        if (created > 0) ShowStatus($"{created}件のファイルからカードを作成しました");
        return created;
    }

    private void RefreshVisible()
    {
        var items = Filter.Apply(AllCards);

        var pinned = items.Where(c => c.IsPinned)
                          .OrderByDescending(c => c.UpdatedAt);

        var rest = items.Where(c => !c.IsPinned);
        rest = CardDisplay.SortMode switch
        {
            "CreatedDesc" => rest.OrderByDescending(c => c.CreatedAt),
            "TitleAsc"    => rest.OrderBy(c => c.DisplayTitle, StringComparer.CurrentCulture),
            "Shuffle"     => CardDisplay.OrderByShuffle(rest, AllCards),
            _             => rest.OrderByDescending(c => c.UpdatedAt),
        };

        var ordered = pinned.Concat(rest).ToList();

        VisibleCards.Clear();
        foreach (var c in ordered) VisibleCards.Add(c);

        RefreshColorCounts();
        RaiseCountAndEmptyStateChanged();
        RandomPreviewCommand.RaiseCanExecuteChanged();
        ExportMarkdownCommand.RaiseCanExecuteChanged();
        CopyAllMarkdownCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// ID-14: 色フィルタチップの件数を、現在の検索・タグ・アーカイブ条件を反映し
    /// 色フィルタ自身は除外した集合から再計算する。カード追加・削除・色変更・アーカイブ・
    /// 検索語やタグの変更・Workspace再読込など、<see cref="RefreshVisible"/> が呼ばれる
    /// すべての経路で自動的に更新される。
    /// </summary>
    private void RefreshColorCounts()
    {
        var counts = Filter.ComputeColorCounts(AllCards);
        foreach (var item in ColorItems)
            item.Count = counts.TryGetValue(item.Name, out var count) ? count : 0;
    }
}
