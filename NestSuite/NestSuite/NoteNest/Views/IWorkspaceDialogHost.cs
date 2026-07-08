using System.Windows;
using NestSuite.NoteNest.Editor;
using NestSuite.ViewModels;

namespace NestSuite.Views;

/// <summary>
/// v1.5.x で導入した AppShell と Workspace View の間のダイアログ操作橋渡しインターフェース。
/// v1.11.0 以降、実装者は <c>NestSuiteShellWindow</c>（AppShell）のみ。
///
/// <para>
/// <see cref="NoteNestWorkspaceView"/> はダイアログの生成・Owner 管理・ファイル選択を
/// 直接行わない。代わりにこのインターフェースを通じて AppShell（<c>NestSuiteShellWindow</c>）へ委譲する。
/// </para>
///
/// <para><b>設計制約</b></para>
/// <list type="bullet">
///   <item>WorkspaceView は AppShell 側のダイアログサービスを直接保持しない</item>
///   <item>WorkspaceView は GetWindow を使った親 Window 参照を行わない</item>
///   <item><c>NestSuiteShellWindow</c> がこのインターフェースを実装し、AppShell 側のダイアログ操作へ委譲する</item>
///   <item>メソッドは WorkspaceView が実際に必要とする最小限の操作に絞る</item>
/// </list>
/// </summary>
public interface IWorkspaceDialogHost
{
    /// <summary>テキスト入力ダイアログを表示し、入力値を返す。キャンセル時は null。</summary>
    string? ShowInput(string title, string prompt, string initialText = "");

    /// <summary>確認ダイアログを表示し、「はい」なら true を返す。</summary>
    bool Confirm(string message, string title = "確認", MessageBoxImage icon = MessageBoxImage.Warning);

    /// <summary>エラーメッセージダイアログを表示する。</summary>
    void ShowError(string message, string title = "エラー");

    /// <summary>情報メッセージダイアログを表示する。</summary>
    void ShowInfo(string message, string title = "情報");

    /// <summary>ノート選択ダイアログを表示し、選択されたノートを返す。キャンセル時は null。</summary>
    NoteViewModel? PickNote(IEnumerable<(string NotebookTitle, NoteViewModel Note)> notes);

    /// <summary>リンク切れチェックを実行してダイアログを表示する。「このノートへ移動」が選択されたときソースノートを返す。それ以外は null。</summary>
    NoteViewModel? CheckBrokenLinks(IEnumerable<NoteViewModel> allNotes);

    /// <summary>
    /// 検索・置換ダイアログを表示する。
    /// <paramref name="editor"/> は WorkspaceView が所有するエディタ TextBox で、
    /// ダイアログが直接操作する。Owner 設定・ダイアログ管理は AppShell 側が担う。
    /// </summary>
    void ShowFindReplace(ITextEditorAdapter editor, IEnumerable<NoteViewModel>? allNotes,
        Action<NoteViewModel>? navigateToNote, string lastSearch, string lastReplace, double? left, double? top);

    /// <summary>検索・置換ダイアログの現在の状態（検索文字列・位置）を返す。</summary>
    (string LastSearchText, string LastReplaceText, double? Left, double? Top) GetFindReplaceState(
        string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop);

    /// <summary>検索・置換ダイアログをアプリ終了前に閉じる。</summary>
    void CloseFindReplace();

    /// <summary>Markdown ファイルの保存先ダイアログを表示し、選択パスを返す。キャンセル時は null。</summary>
    string? SelectMarkdownSavePath(string defaultFileName);

    /// <summary>
    /// v2.16.5 SH-28: 操作完了を伝える一時通知（1〜2 秒程度で自動的に消える）を表示する。
    /// モーダルダイアログではなく、AppShell 側のステータス表示領域を更新する想定。
    /// </summary>
    void ShowTransientStatus(string message);
}
