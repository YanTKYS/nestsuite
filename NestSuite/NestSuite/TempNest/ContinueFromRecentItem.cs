using System.IO;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.TempNest;

/// <summary>
/// SH-40 (AT-1 フェーズ1): 「続きから」recent filesリンク1件分の表示用データ。
/// ファイル名だけを表示し、フルパスはToolTip限定（設計レビュー方針）。
/// クリック時の実処理はShellが<see cref="TempNestWorkspaceViewModel.OpenContinueFromRecentRequested"/>
/// 経由で配線する既存open経路（<c>ShellFileOpenPlanner.Plan</c>）へ委譲する。
/// </summary>
public sealed class ContinueFromRecentItem
{
    public string FilePath { get; }
    public string FileName { get; }
    public string AutomationName { get; }

    /// <summary>SH-40: 先頭項目は空文字、2件目以降は "・" 区切りをXAML側で描画するための文字列。</summary>
    public string LeadingSeparator { get; set; } = "";

    /// <summary>AutomationId候補（TempNest.ContinueFrom.Recent1〜3）。生成側が連番で設定する。</summary>
    public string AutomationId { get; set; } = "";

    public ICommand OpenCommand { get; }

    public ContinueFromRecentItem(string filePath, Action<string> openHandler)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        AutomationName = $"最近使ったファイル {FileName} を開く";
        OpenCommand = new RelayCommand(_ => openHandler(filePath));
    }
}
