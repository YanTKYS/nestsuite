using System.Collections.ObjectModel;
using System.Threading;
using NestSuite.Services;

namespace NestSuite.ViewModels;

/// <summary>
/// v2.15.0 SH: Shell 横断検索パネルの状態。パネルの表示状態・検索語・検索結果はすべて
/// セッション内のみで保持し、ui-settings.json など永続化ファイルには一切書き込まない。
///
/// <para><see cref="_getTabs"/> は現在開いているタブ一覧（<see cref="ShellSearchTabEntry"/>）を
/// 都度取得するコールバック。NestSuiteShellWindow 側は呼び出すたびに最新の _sessionManager /
/// _tabs の状態を反映したコレクションを返す。</para>
///
/// <para>SH-41 (AT-2 フェーズ1): 「最近のファイルも検索」（<see cref="IncludeRecentFiles"/>、既定OFF）を
/// ONにした場合だけ、<see cref="_getRecentFilePaths"/>・<see cref="_getOpenFilePaths"/> から
/// 現在開いていないrecent files上位件数を1回だけ非同期読込し、以後はキー入力ごとに
/// メモリ内スナップショットへ検索する（キー入力ごとのファイルI/Oはしない）。
/// <see cref="_runInBackground"/>/<see cref="_postToUiThread"/> は既定で同期実行（テスト用）。
/// Shell側は <c>Task.Run</c>/<c>Dispatcher</c> を注入する。</para>
/// </summary>
public sealed class ShellSearchPanelViewModel : BaseViewModel, IDisposable
{
    private readonly Func<IReadOnlyList<ShellSearchTabEntry>> _getTabs;
    private readonly Func<IReadOnlyList<string>> _getRecentFilePaths;
    private readonly Func<IReadOnlyList<string?>> _getOpenFilePaths;
    private readonly Func<string, bool>? _fileExists;
    private readonly Func<string, string>? _readAllText;
    private readonly Action<Action> _runInBackground;
    private readonly Action<Action> _postToUiThread;

    private string _searchText = "";
    private string _statusMessage = "";
    private bool _includeRecentFiles;
    private bool _isLoadingRecentFiles;
    private int _skippedRecentFileCount;
    private IReadOnlyList<UnopenedSearchDocument> _unopenedSnapshot = [];
    private CancellationTokenSource? _loadCts;
    private int _loadGeneration;
    private bool _disposed;

    public ShellSearchPanelViewModel(
        Func<IReadOnlyList<ShellSearchTabEntry>> getTabs,
        Func<IReadOnlyList<string>>? getRecentFilePaths = null,
        Func<IReadOnlyList<string?>>? getOpenFilePaths = null,
        Func<string, bool>? fileExists = null,
        Func<string, string>? readAllText = null,
        Action<Action>? runInBackground = null,
        Action<Action>? postToUiThread = null)
    {
        _getTabs = getTabs;
        _getRecentFilePaths = getRecentFilePaths ?? (() => []);
        _getOpenFilePaths = getOpenFilePaths ?? (() => []);
        _fileExists = fileExists;
        _readAllText = readAllText;
        // 既定は同期実行（xunit等での決定的なテストのため）。Shell実行時は Task.Run / Dispatcher を注入する。
        _runInBackground = runInBackground ?? (action => action());
        _postToUiThread = postToUiThread ?? (action => action());
    }

    /// <summary>開いているタブの検索結果。</summary>
    public ObservableCollection<ShellSearchResult> Results { get; } = new();

    /// <summary>SH-41: 未オープンrecent filesの検索結果（<see cref="IncludeRecentFiles"/> OFF中は常に空）。</summary>
    public ObservableCollection<ShellSearchResult> UnopenedResults { get; } = new();

    public bool HasResults => Results.Count > 0;

    public bool HasUnopenedResults => UnopenedResults.Count > 0;

    public string SearchText
    {
        get => _searchText;
        set
        {
            // SetProperty は値が変化しない限り通知しないため、"" → "" のような
            // 無変化の再設定（初期状態や Reset 直後の再検索）でも必ず結果を再計算するよう
            // SetProperty の戻り値に関わらず RunSearch を呼ぶ。
            SetProperty(ref _searchText, value);
            RunSearch();
        }
    }

    /// <summary>
    /// SH-41: 「最近のファイルも検索」チェック。既定OFF・永続化しない。ONにした時点で
    /// 未オープンrecent files上位件数を1回だけ非同期読込する。OFFにすると読込中の処理を
    /// キャンセルし、スナップショットを破棄する。
    /// </summary>
    public bool IncludeRecentFiles
    {
        get => _includeRecentFiles;
        set
        {
            if (!SetProperty(ref _includeRecentFiles, value)) return;
            if (value)
                BeginLoadRecentFiles();
            else
                CancelAndClearRecentFilesSnapshot();
            RunSearch();
        }
    }

    /// <summary>SH-41: 未オープンrecent filesを読込中かどうか。読込中表示のバインド用。</summary>
    public bool IsLoadingRecentFiles
    {
        get => _isLoadingRecentFiles;
        private set => SetProperty(ref _isLoadingRecentFiles, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
                OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    /// <summary>検索結果・状態メッセージ・「最近のファイルも検索」を初期状態へ戻す。パネルを閉じる際に呼ぶ。</summary>
    public void Reset()
    {
        _searchText = "";
        OnPropertyChanged(nameof(SearchText));
        Results.Clear();
        // SH-41: パネルを閉じて再度開いた場合も、原則OFFへ戻す（既定値・永続化しない）。
        _includeRecentFiles = false;
        OnPropertyChanged(nameof(IncludeRecentFiles));
        CancelAndClearRecentFilesSnapshot();
        StatusMessage = "";
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasUnopenedResults));
    }

    private void BeginLoadRecentFiles()
    {
        _loadCts?.Cancel();
        var cts = new CancellationTokenSource();
        _loadCts = cts;
        var generation = ++_loadGeneration;
        var token = cts.Token;

        _unopenedSnapshot = [];
        UnopenedResults.Clear();
        _skippedRecentFileCount = 0;
        IsLoadingRecentFiles = true;

        _runInBackground(() =>
        {
            IReadOnlyList<UnopenedFileLoadResult> loadResults;
            try
            {
                // SH-41: 候補選定（recent files・開いているファイルの取得と除外）も含め、
                // 読込タスク全体を1つの保護区間として扱う（候補選定コールバック自体が
                // 例外を投げる場合も、読込タスク全体の予期しない例外としてErrorLogへ記録する）。
                var candidatePaths = ShellSearchService.SelectUnopenedRecentFilePaths(_getRecentFilePaths(), _getOpenFilePaths());
                loadResults = UnopenedRecentFileLoader.Load(candidatePaths, token, _fileExists, _readAllText);
            }
            catch (OperationCanceledException)
            {
                // SH-41: チェックOFF・パネル閉鎖・再ON等によるキャンセル。エラー表示・ErrorLog記録はしない。
                return;
            }
            catch (Exception ex)
            {
                _postToUiThread(() =>
                {
                    if (generation != _loadGeneration) return;
                    ErrorLogService.Log("CrossSearchUnopenedLoad", ex);
                    IsLoadingRecentFiles = false;
                    RunSearch();
                });
                return;
            }

            _postToUiThread(() =>
            {
                if (generation != _loadGeneration) return; // 古い世代の完了は反映しない
                _unopenedSnapshot = loadResults.Where(r => r.Succeeded).Select(r => r.Document!).ToList();
                _skippedRecentFileCount = loadResults.Count(r => !r.Succeeded);
                IsLoadingRecentFiles = false;
                RunSearch();
            });
        });
    }

    private void CancelAndClearRecentFilesSnapshot()
    {
        _loadCts?.Cancel();
        _loadCts = null;
        _loadGeneration++; // 進行中コールバックを無効化する
        _unopenedSnapshot = [];
        UnopenedResults.Clear();
        IsLoadingRecentFiles = false;
        _skippedRecentFileCount = 0;
    }

    private void RunSearch()
    {
        Results.Clear();
        UnopenedResults.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = IsLoadingRecentFiles ? "最近のファイルを読み込み中…" : "検索語を入力してください。";
            RaiseResultPropertiesChanged();
            return;
        }

        var openMatches = ShellSearchService.Search(SearchText, _getTabs(), out var isTruncated);
        foreach (var match in openMatches)
            Results.Add(match);

        if (_includeRecentFiles && _unopenedSnapshot.Count > 0)
        {
            // SH-41: 未オープン結果からファイルを開いた後は、同じファイルを次回検索で
            // 開いているタブ側へ委ね、未オープン側から除外する（dirty内容の優先・重複防止）。
            // スナップショット自体は直ちに再読込しない。
            var openPaths = _getOpenFilePaths();
            var effectiveDocs = _unopenedSnapshot
                .Where(doc => !openPaths.Any(open => NestSuiteOpenFilePolicy.IsSameFile(open, doc.FilePath)))
                .ToList();

            var remainingBudget = ShellSearchService.MaxResults - Results.Count;
            if (remainingBudget > 0 && effectiveDocs.Count > 0)
            {
                var unopenedMatches = ShellSearchService.SearchUnopened(SearchText, effectiveDocs, remainingBudget, out var unopenedTruncated);
                foreach (var match in unopenedMatches)
                    UnopenedResults.Add(match);
                isTruncated |= unopenedTruncated;
            }
            else if (remainingBudget <= 0)
            {
                isTruncated = true;
            }
        }

        var statusParts = new List<string>();
        if (IsLoadingRecentFiles) statusParts.Add("最近のファイルを読み込み中…");
        if (isTruncated) statusParts.Add("結果が多すぎるため、先頭100件のみ表示しています。");
        if (_skippedRecentFileCount > 0) statusParts.Add($"{_skippedRecentFileCount}件のファイルを検索できませんでした。");
        StatusMessage = string.Join(" ", statusParts);

        RaiseResultPropertiesChanged();
    }

    private void RaiseResultPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasUnopenedResults));
    }

    /// <summary>SH-41: Shell終了時等に読込中の処理をキャンセルする。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _loadCts?.Cancel();
        _loadGeneration++;
    }
}
