using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NestSuite.IdeaNest.Commands;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.Views;

namespace NestSuite.IdeaNest.ViewModels;

public class IdeaNestWorkspaceViewModel : IdeaNestViewModelBase, IDisposable
{
    private Workspace _workspace = new();
    private CardOperationsService _cardOps = null!;
    private TagManagementService _tagMgmt = null!;
    private DispatcherTimer? _statusClearTimer;
    private string _statusMessage = string.Empty;
    private readonly IdeaNestWorkspaceUiService _ui;
    private bool _hasChanges;
    private bool _disposed;

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
    public ICommand ExportMarkdownCommand { get; }
    public ICommand CopyCardMarkdownCommand { get; }
    public ICommand CopyAllMarkdownCommand { get; }
    public ICommand ExportNoteNestCommand { get; }
    public ICommand CopyNoteNestCommand { get; }
    public ICommand ToggleTagPanelCommand { get; }
    public ICommand SetCardSizeCommand { get; }
    public ICommand SetCardHeightModeCommand { get; }
    public ICommand ReshuffleCommand { get; }

    public string DisplayName => _workspace.WorkspaceName;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public int TotalCount => AllCards.Count;
    public int VisibleCount => VisibleCards.Count;
    public int VisibleCardCount => VisibleCount;

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

        // No-op export commands
        ExportMarkdownCommand   = new IdeaNestRelayCommand(_ => { });
        CopyCardMarkdownCommand = new IdeaNestRelayCommand(_ => { });
        CopyAllMarkdownCommand  = new IdeaNestRelayCommand(_ => { });
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
        CardDisplay.PropertyChanged -= OnSubVmPropertyChanged;
        Filter.PropertyChanged     -= OnSubVmPropertyChanged;
        TagPanel.PropertyChanged   -= OnSubVmPropertyChanged;
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
            $"「{card.DisplayTitle}」を削除します。削除すると元に戻せません。\n\n" +
            "不要な場合は、削除ではなくアーカイブ (📥) も検討してください。",
            primaryText: "削除",
            cancelText: "キャンセル");
        if (ok != ConfirmResult.Primary) return;
        _cardOps.CommitDelete(card);
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
        _cardOps.ToggleArchive(card);
        ShowStatus(card.IsArchived ? "アーカイブしました" : "アーカイブを解除しました");
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

        RaiseCountAndEmptyStateChanged();
        RandomPreviewCommand.RaiseCanExecuteChanged();
    }
}
