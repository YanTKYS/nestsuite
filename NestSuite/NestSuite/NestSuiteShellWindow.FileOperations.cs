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
    // v2.7.18 TD-16: ファイル操作周辺処理は責務別 partial へ分割。
    // - FileOpen: 開く/読込/起動時読込
    // - FileSave: 上書き保存/保存コマンド
    // - FileSaveAs: 名前を付けて保存
    // - FileSaveStateSync: 保存成功後のタブ・Session 同期
    // - FileCommands: 新規作成・ファイルメニュー入口
}
