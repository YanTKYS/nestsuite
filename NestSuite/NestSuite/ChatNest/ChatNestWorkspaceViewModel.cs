using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NoteNest.NestSuite.ChatNest;

/// <summary>
/// ChatNest Workspace の ViewModel。参照ソース ChatNest v0.4.1
/// ViewModels/ChatNestWorkspaceViewModel.cs より、Workspace 部分を中心に取り込み。
///
/// <para><b>v1.7.0 の位置づけ</b><br/>
/// NestSuite 上で 2 つ目の Workspace として ChatNest を表示できるか検証するための最小実装。
/// メッセージ一覧・入力欄・発言者切替・投稿・削除を提供する。
/// 保存／読込（.chatnest ファイル）はメモリ内のみとし、永続化は次段階へ回す。</para>
///
/// <para><b>MessageBox 暫定許容</b><br/>
/// 発言削除確認に <c>MessageBox</c> を直接使用する。これは ChatNest 参照ソースの挙動を
/// 維持した暫定対応であり、本ファイルは ArchitectureBoundaryTests の走査対象外
/// （NestSuite 配下）に置く。IWorkspaceDialogHost 相当への委譲は次段階の課題。
/// 詳細は docs/design-decisions.md を参照。</para>
/// </summary>
public class ChatNestWorkspaceViewModel : INotifyPropertyChanged
{
    private string _inputText = string.Empty;
    private Speaker _selectedSpeaker = Speaker.自分;
    private bool _isDirty;
    private string _copyStatusText = string.Empty;
    private DispatcherTimer? _copyStatusTimer;

    private readonly ChatNestRelayCommand _postCommand;
    private readonly ChatNestRelayCommand _copyNestSuiteCommand;
    private readonly ChatNestRelayCommand _copyMarkdownCommand;

    public ObservableCollection<Message> Messages { get; } = new();
    public Speaker[] Speakers { get; } = Enum.GetValues<Speaker>();

    public string InputText
    {
        get => _inputText;
        set
        {
            _inputText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedChanges));
            _postCommand.RaiseCanExecuteChanged();
        }
    }

    public Speaker SelectedSpeaker
    {
        get => _selectedSpeaker;
        set { _selectedSpeaker = value; OnPropertyChanged(); }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set { _isDirty = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUnsavedChanges)); }
    }

    /// <summary>
    /// 破棄確認の対象となる未保存状態。投稿・削除による <see cref="IsDirty"/> に加え、
    /// 投稿前の入力欄テキスト（空白のみを除く）も対象に含める。ChatNest は保存手段を
    /// 持たないため、終了時にこの値が真なら破棄確認を表示する想定。
    /// </summary>
    public bool HasUnsavedChanges => IsDirty || !string.IsNullOrWhiteSpace(InputText);

    /// <summary>v1.16.5: コピー操作後に一時表示するステータスメッセージ。</summary>
    public string CopyStatusText
    {
        get => _copyStatusText;
        private set { _copyStatusText = value; OnPropertyChanged(); }
    }

    public ICommand PostCommand => _postCommand;
    public ICommand DeleteMessageCommand { get; }

    // v1.16.5: クリップボードコピーコマンド
    public ICommand CopyNestSuiteCommand => _copyNestSuiteCommand;
    public ICommand CopyMarkdownCommand => _copyMarkdownCommand;

    public event EventHandler? WorkspaceModified;

    public ChatNestWorkspaceViewModel()
    {
        _postCommand = new ChatNestRelayCommand(Post, () => !string.IsNullOrWhiteSpace(InputText));
        DeleteMessageCommand = new ChatNestRelayCommand<Message>(DeleteMessage);

        _copyNestSuiteCommand = new ChatNestRelayCommand(ExecuteCopyNestSuite, () => Messages.Count > 0);
        _copyMarkdownCommand = new ChatNestRelayCommand(ExecuteCopyMarkdown, () => Messages.Count > 0);

        Messages.CollectionChanged += (_, _) =>
        {
            _copyNestSuiteCommand.RaiseCanExecuteChanged();
            _copyMarkdownCommand.RaiseCanExecuteChanged();
        };
    }

    public void CycleSpeaker(bool forward)
    {
        var speakers = Enum.GetValues<Speaker>();
        int idx = Array.IndexOf(speakers, SelectedSpeaker);
        idx = forward
            ? (idx + 1) % speakers.Length
            : (idx - 1 + speakers.Length) % speakers.Length;
        SelectedSpeaker = speakers[idx];
    }

    public void MarkSaved() => IsDirty = false;

    public void Clear()
    {
        Messages.Clear();
        InputText = string.Empty;
        IsDirty = false;
    }

    public void LoadMessages(IEnumerable<Message> messages)
    {
        Messages.Clear();
        InputText = string.Empty;
        foreach (var m in messages)
            Messages.Add(m);
        IsDirty = false;
    }

    /// <summary>
    /// v1.16.7: NoteNest への貼り付けに適した形式を生成する。
    /// 先頭に [NOTE] マーカーと転記日時を付け、発言者を ## 見出しで表現する。
    /// 連続する同一発言者のメッセージは 1 ブロックに集約する。
    /// </summary>
    public string BuildNestSuiteText()
    {
        if (Messages.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"[NOTE] ChatNestからの転記: {DateTime.Now:yyyy-MM-dd HH:mm}");
        int i = 0;
        while (i < Messages.Count)
        {
            var speaker = Messages[i].Speaker;
            var groupTexts = new List<string>();
            while (i < Messages.Count && Messages[i].Speaker == speaker)
            {
                groupTexts.Add(Messages[i].Text);
                i++;
            }
            sb.AppendLine();
            sb.AppendLine($"## {speaker}");
            sb.AppendLine();
            sb.Append(string.Join(Environment.NewLine, groupTexts));
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// v1.16.5: Markdown 形式のエクスポートテキストを生成する。
    /// 発言者名を ## 見出しとして本文を続ける。
    /// 連続する同一発言者のメッセージは 1 ブロックに集約する。
    /// </summary>
    public string BuildMarkdownText()
    {
        if (Messages.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine("# ChatNest Export");
        int i = 0;
        while (i < Messages.Count)
        {
            var speaker = Messages[i].Speaker;
            var groupTexts = new List<string>();
            while (i < Messages.Count && Messages[i].Speaker == speaker)
            {
                groupTexts.Add(Messages[i].Text);
                i++;
            }
            sb.AppendLine();
            sb.AppendLine($"## {speaker}");
            sb.Append(string.Join(Environment.NewLine, groupTexts));
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private void ExecuteCopyNestSuite()
    {
        if (Messages.Count == 0) return;
        CopyToClipboard(BuildNestSuiteText());
    }

    private void ExecuteCopyMarkdown()
    {
        if (Messages.Count == 0) return;
        CopyToClipboard(BuildMarkdownText());
    }

    private void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            ShowCopyStatus("コピーしました");
        }
        catch
        {
            ShowCopyStatus("コピーに失敗しました");
        }
    }

    private void ShowCopyStatus(string message)
    {
        CopyStatusText = message;
        _copyStatusTimer?.Stop();
        _copyStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _copyStatusTimer.Tick += (_, _) =>
        {
            CopyStatusText = string.Empty;
            _copyStatusTimer?.Stop();
        };
        _copyStatusTimer.Start();
    }

    private void Post()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;
        Messages.Add(new Message { Speaker = SelectedSpeaker, Text = text });
        InputText = string.Empty;
        IsDirty = true;
        WorkspaceModified?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteMessage(Message? message)
    {
        if (message == null) return;
        // v1.7.0 暫定許容: 発言削除確認に MessageBox を直接使用（NestSuite 配下のため境界テスト対象外）
        if (MessageBox.Show("この発言を削除しますか？", "削除の確認",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
        {
            Messages.Remove(message);
            IsDirty = true;
            WorkspaceModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
