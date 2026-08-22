namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // タブ周辺処理は責務別 partial へ分割。
    // - TabSelection: 選択・アクティブ化・キーボード移動・ステータス同期
    // - TabLifecycle: タブ生成・置換・ViewModel からのタイトル/未保存同期
    // - TabClose: タブクローズと Workspace 破棄
    // - TabContextMenu: 右クリック/中クリックなどタブ操作入口
}
