using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using NestSuite.ChatNest;
using NestSuite.FileAssociation;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Services;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // v2.7.17 TD-15: タブ周辺処理は責務別 partial へ分割。
    // - TabSelection: 選択・アクティブ化・キーボード移動・ステータス同期
    // - TabLifecycle: タブ生成・置換・ViewModel からのタイトル/未保存同期
    // - TabClose: タブクローズと Workspace 破棄
    // - TabContextMenu: 右クリック/中クリックなどタブ操作入口
}
