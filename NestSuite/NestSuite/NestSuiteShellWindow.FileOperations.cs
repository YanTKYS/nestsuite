namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // v2.7.18 TD-16: ファイル操作周辺処理は責務別 partial へ分割。
    // - FileOpen: 開く/読込/起動時読込
    // - FileSave: 上書き保存/保存コマンド
    // - FileSaveAs: 名前を付けて保存
    // - FileSaveStateSync: 保存成功後のタブ・Session 同期
    // - FileCommands: 新規作成・ファイルメニュー入口
    // - SaveAll: Ctrl+Shift+S 全タブ一括保存（SH-20）
}
