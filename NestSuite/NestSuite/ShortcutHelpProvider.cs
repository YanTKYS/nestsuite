namespace NestSuite;

/// <summary>
/// v2.16.4 SH-19: ヘルプメニューに表示するキーボードショートカット一覧。
/// 現行コードに存在するショートカットのみを静的に列挙する。
/// </summary>
public static class ShortcutHelpProvider
{
    public static IReadOnlyList<ShortcutHelpItem> GetItems() =>
    [
        new("Shell共通", "保存", "Ctrl+S", "現在選択中の Workspace を保存します。"),
        new("Shell共通", "すべて保存", "Ctrl+Shift+S", "保存できる開いているタブをまとめて保存します。"),
        new("Shell共通", "横断検索", "Ctrl+Shift+F", "開いているタブを横断検索するパネルを開閉します。"),

        new("タブ操作", "次のタブへ移動", "Ctrl+Tab", "次のタブを選択します。"),
        new("タブ操作", "前のタブへ移動", "Ctrl+Shift+Tab", "前のタブを選択します。"),
        new("タブ操作", "番号でタブを選択", "Ctrl+1 ～ Ctrl+9", "左から 1～9 番目のタブを選択します。"),

        new("NoteNest", "検索 / 置換", "Ctrl+F", "NoteNest タブ選択中に検索 / 置換ダイアログを開きます。"),
        new("NoteNest", "検索結果の次へ", "Enter", "検索 / 置換ダイアログの検索欄で次の一致へ移動します。"),
        new("NoteNest", "検索結果の前へ", "Shift+Enter", "検索 / 置換ダイアログの検索欄で前の一致へ移動します。"),
        new("NoteNest", "ノートリンク候補の選択", "↑ / ↓ / Tab / Esc", "ノートリンク候補表示中に候補移動、確定、または閉じる操作をします。"),

        new("IdeaNest", "検索", "Ctrl+F", "カードのタイトル・本文・タグ検索欄へ移動します。"),
        new("IdeaNest", "新規カード追加", "Ctrl+Shift+N", "新しいアイデアカードを追加します。"),
        new("IdeaNest", "表示中カードをコピー", "Ctrl+Shift+C", "表示中カードを Markdown 形式でコピーします。"),
        new("IdeaNest", "ランダムプレビュー", "Ctrl+Shift+R", "カードをランダムに 1 件プレビューします。"),
        new("IdeaNest", "検索を閉じる", "Esc", "検索欄の入力中に検索を閉じます。"),
        new("IdeaNest", "プレビューを保存", "Ctrl+S", "カードプレビュー / 編集ウィンドウの変更を確定します。"),
        new("IdeaNest", "プレビューを閉じる", "Esc", "カードプレビュー / 編集ウィンドウを閉じます。"),
        new("IdeaNest", "前後のカードへ移動", "← / →", "カードプレビューで前後のカードへ移動します（テキスト入力中を除く）。"),

        new("ChatNest", "検索", "Ctrl+F", "会話内検索バーを開きます。"),
        new("ChatNest", "検索を閉じる", "Esc", "会話内検索バーを閉じます。"),
        new("ChatNest", "検索結果の次へ", "Enter", "検索欄で次の一致へ移動します。"),
        new("ChatNest", "検索結果の前へ", "Shift+Enter", "検索欄で前の一致へ移動します。"),
        new("ChatNest", "投稿", "Ctrl+Enter", "入力欄の本文を投稿します。通常の Enter は改行です。"),
        new("ChatNest", "発言者切り替え", "Ctrl+← / Ctrl+→", "入力欄で発言者を前後に切り替えます。"),
        new("ChatNest", "編集中メッセージを閉じる", "Esc", "インライン編集中のメッセージ編集を閉じます。"),
    ];

    public static IReadOnlyList<ShortcutHelpGroup> GetGroups() =>
        GetItems()
            .GroupBy(item => item.Category)
            .Select(group => new ShortcutHelpGroup(group.Key, group.ToList()))
            .ToList();
}
