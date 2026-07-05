using System.Collections.ObjectModel;
using NestSuite.Services;

namespace NestSuite.ViewModels;

/// <summary>
/// v2.15.0 SH: Shell 横断検索パネルの状態。パネルの表示状態・検索語・検索結果はすべて
/// セッション内のみで保持し、ui-settings.json など永続化ファイルには一切書き込まない。
///
/// <para><see cref="_getTabs"/> は現在開いているタブ一覧（<see cref="ShellSearchTabEntry"/>）を
/// 都度取得するコールバック。NestSuiteShellWindow 側は呼び出すたびに最新の _sessionManager /
/// _tabs の状態を反映したコレクションを返す。</para>
/// </summary>
public sealed class ShellSearchPanelViewModel : BaseViewModel
{
    private readonly Func<IReadOnlyList<ShellSearchTabEntry>> _getTabs;
    private string _searchText = "";
    private string _statusMessage = "";

    public ShellSearchPanelViewModel(Func<IReadOnlyList<ShellSearchTabEntry>> getTabs)
    {
        _getTabs = getTabs;
    }

    public ObservableCollection<ShellSearchResult> Results { get; } = new();

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

    /// <summary>検索結果・状態メッセージを空にする。パネルを閉じる際に呼ぶ。</summary>
    public void Reset()
    {
        _searchText = "";
        OnPropertyChanged(nameof(SearchText));
        Results.Clear();
        StatusMessage = "";
    }

    private void RunSearch()
    {
        Results.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = "検索語を入力してください。";
            return;
        }

        var matches = ShellSearchService.Search(SearchText, _getTabs());
        foreach (var match in matches)
            Results.Add(match);

        StatusMessage = matches.Count >= ShellSearchService.MaxResults
            ? "結果が多すぎるため、先頭100件のみ表示しています。"
            : "";
    }
}
