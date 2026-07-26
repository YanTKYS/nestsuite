using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.15 SH-46: Shellファイルメニュー・タブContextMenuのアクセスキー重複解消の
/// 静的 UI 契約確認。表示文言・Clickハンドラ・Command・メニュー順序・保存/タブ処理ロジックは
/// 一切変更していないため、ここではアクセスキーの一意性・変更前後の対応関係のみを確認する。
/// </summary>
public class SH46MenuAccessKeyTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadShellXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml"));

    private static string ExtractRegion(string xaml, string startAnchor, string endAnchor)
    {
        var start = xaml.IndexOf(startAnchor, System.StringComparison.Ordinal);
        Assert.True(start >= 0, $"start anchor not found: {startAnchor}");
        var end = xaml.IndexOf(endAnchor, start, System.StringComparison.Ordinal);
        Assert.True(end >= 0, $"end anchor not found: {endAnchor}");
        return xaml.Substring(start, end - start + endAnchor.Length);
    }

    /// <summary>
    /// ファイルメニュー直下（新規作成の子階層は除く）のHeaderからアクセスキーを1件ずつ抽出する。
    /// 「新規作成」の子MenuItem群は別階層のため対象外とし、抽出範囲を「新規作成」の閉じタグ以降に限定する。
    /// </summary>
    private static List<char> ExtractFileMenuTopLevelAccessKeys(string xaml)
    {
        var fileMenuRegion = ExtractRegion(xaml, "Header=\"ファイル(_F)\"", "Header=\"終了(_X)\" Click=\"MenuExit_Click\"/>");
        // 新規作成の子階層（新規 NoteNest 等）を除外するため、"新規作成" MenuItem の閉じタグ以降だけを見る。
        var afterNewMenu = fileMenuRegion.Substring(fileMenuRegion.IndexOf("</MenuItem>", System.StringComparison.Ordinal) + "</MenuItem>".Length);
        return ExtractAccessKeys(afterNewMenu);
    }

    private static List<char> ExtractAccessKeys(string region)
    {
        var keys = new List<char>();
        var idx = 0;
        while (true)
        {
            idx = region.IndexOf("(_", idx, System.StringComparison.Ordinal);
            if (idx < 0) break;
            var key = region[idx + 2];
            keys.Add(char.ToUpperInvariant(key));
            idx += 2;
        }
        return keys;
    }

    // ── 1. ファイルメニューの重複解消 ────────────────────────────────────

    [Fact]
    public void FileMenu_NewMenuItem_KeepsExistingN()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("<MenuItem Header=\"新規作成(_N)\"", xaml);
    }

    [Fact]
    public void FileMenu_SaveAsMenuItem_AccessKeyIsNotN()
    {
        var xaml = ReadShellXaml();
        Assert.DoesNotContain("Header=\"名前を付けて保存(_N)...\"", xaml);
        Assert.Contains("Header=\"名前を付けて保存(_V)...\"", xaml);
    }

    // ── 3. ファイルメニュー直下の一意性 ──────────────────────────────────

    [Fact]
    public void FileMenu_TopLevelAccessKeys_AreAllUnique()
    {
        var keys = ExtractFileMenuTopLevelAccessKeys(ReadShellXaml());
        // 開く・最近使ったファイル・上書き保存・すべて保存・名前を付けて保存・終了 の6件を想定する。
        Assert.Equal(6, keys.Count);
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    // ── 2. タブContextMenuの重複解消 ─────────────────────────────────────

    [Fact]
    public void TabContextMenu_ReturnDetached_AccessKeyIsNotR()
    {
        var xaml = ReadShellXaml();
        Assert.DoesNotContain("Header=\"このタブへ戻す(_R)\"", xaml);
        Assert.Contains("Header=\"このタブへ戻す(_B)\"", xaml);
    }

    [Fact]
    public void TabContextMenu_CloseRight_KeepsExistingR()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("Header=\"右側のタブを閉じる(_R)\" Click=\"TabContextCloseRight_Click\"/>", xaml);
    }

    // ── 3. タブContextMenu全体の一意性 ──────────────────────────────────

    [Fact]
    public void TabContextMenu_AllAccessKeys_AreUnique()
    {
        var xaml = ReadShellXaml();
        var region = ExtractRegion(xaml, "<Grid.ContextMenu>", "</Grid.ContextMenu>");
        var keys = ExtractAccessKeys(region);
        // このタブを閉じる・ピン留め・ピン留めを解除・別ウィンドウで表示・このタブへ戻す・
        // 他のタブを閉じる・右側のタブを閉じる の7件を想定する。
        Assert.Equal(7, keys.Count);
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    // ── 4. 新規作成子メニューの非干渉 ────────────────────────────────────

    [Fact]
    public void NewMenu_ChildItems_AreUnchanged()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("Header=\"新規 NoteNest — ノートをプロジェクト単位で管理(_N)\"", xaml);
        Assert.Contains("Header=\"新規 IdeaNest — アイデアをカード形式で整理(_I)\"", xaml);
        Assert.Contains("Header=\"新規 ChatNest — チャット形式でブレスト記録(_C)\"", xaml);
        Assert.Contains("Header=\"新規 テキスト — シンプルなプレーンテキスト編集(_T)\"", xaml);
    }

    // ── 5. 表示文言の維持 ────────────────────────────────────────────────

    [Fact]
    public void DisplayText_ExcludingAccessKeyMarkers_IsUnchanged()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("名前を付けて保存", xaml);
        Assert.Contains("このタブへ戻す", xaml);
        Assert.Contains("右側のタブを閉じる", xaml);
        Assert.Contains("新規作成", xaml);
        // 用語変更・短縮の混入がないことを確認する。
        Assert.DoesNotContain("別名保存", xaml);
        Assert.DoesNotContain("名前保存", xaml);
        Assert.DoesNotContain("タブを戻す", xaml);
        Assert.DoesNotContain("右タブを閉じる", xaml);
    }

    // ── 6. Click・Commandハンドラの維持 ──────────────────────────────────

    [Fact]
    public void ExistingClickHandlers_AreUnchanged()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("Click=\"MenuSaveAs_Click\"", xaml);
        Assert.Contains("Click=\"TabContextReturnDetached_Click\"", xaml);
        Assert.Contains("Click=\"TabContextCloseRight_Click\"", xaml);
    }

    // ── 7. InputGestureText・既存ショートカットの維持 ───────────────────

    [Fact]
    public void ExistingShortcuts_AreUnchanged_AndNoNewOneAdded()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("InputGestureText=\"Ctrl+S\"", xaml);
        Assert.Contains("InputGestureText=\"Ctrl+Shift+S\"", xaml);

        var saveAsRegion = ExtractRegion(xaml, "x:Name=\"SaveAsMenuItem\"", "/>");
        Assert.DoesNotContain("InputGestureText", saveAsRegion);
    }

    // ── 8. メニュー順序の維持 ────────────────────────────────────────────

    [Fact]
    public void FileMenu_SaveItemsOrder_IsUnchanged()
    {
        var xaml = ReadShellXaml();
        var saveIdx = xaml.IndexOf("x:Name=\"SaveMenuItem\"", System.StringComparison.Ordinal);
        var saveAllIdx = xaml.IndexOf("x:Name=\"SaveAllMenuItem\"", System.StringComparison.Ordinal);
        var saveAsIdx = xaml.IndexOf("x:Name=\"SaveAsMenuItem\"", System.StringComparison.Ordinal);
        Assert.True(saveIdx > 0 && saveAllIdx > saveIdx && saveAsIdx > saveAllIdx);
    }

    [Fact]
    public void TabContextMenu_ItemsOrder_IsUnchanged()
    {
        var xaml = ReadShellXaml();
        var returnIdx = xaml.IndexOf("Click=\"TabContextReturnDetached_Click\"", System.StringComparison.Ordinal);
        var closeOthersIdx = xaml.IndexOf("Click=\"TabContextCloseOthers_Click\"", System.StringComparison.Ordinal);
        var closeRightIdx = xaml.IndexOf("Click=\"TabContextCloseRight_Click\"", System.StringComparison.Ordinal);
        Assert.True(returnIdx > 0 && closeOthersIdx > returnIdx && closeRightIdx > closeOthersIdx);
    }

    // ── 9. SH-45 AutomationNameの維持 ────────────────────────────────────

    [Fact]
    public void SH45AutomationNames_AreUnchanged()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"Shell.TabAddButton\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"新規タブを追加\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"Shell.TabListButton\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"タブ一覧を開く\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"このタブを閉じる\"", xaml);
        Assert.Contains("AutomationProperties.Name=\"横断検索を閉じる\"", xaml);
    }

    // ── 10. 非対象メニューの維持 ─────────────────────────────────────────

    [Fact]
    public void UnrelatedAccessKeys_AreUnchanged()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("Header=\"ファイル(_F)\"", xaml);
        Assert.Contains("Header=\"ツール(_T)\"", xaml);
        Assert.Contains("Header=\"開く(_O)...\"", xaml);
        Assert.Contains("Header=\"最近使ったファイル(_R)\"", xaml);
        Assert.Contains("Header=\"上書き保存(_S)\"", xaml);
        Assert.Contains("Header=\"すべて保存(_A)\"", xaml);
        Assert.Contains("Header=\"終了(_X)\"", xaml);
        Assert.Contains("Header=\"このタブを閉じる(_C)\"", xaml);
        Assert.Contains("Header=\"ピン留め(_P)\"", xaml);
        Assert.Contains("Header=\"ピン留めを解除(_U)\"", xaml);
        Assert.Contains("Header=\"別ウィンドウで表示(_D)\"", xaml);
        Assert.Contains("Header=\"他のタブを閉じる(_O)\"", xaml);
    }

    // ── ToolTip・無効理由（SH-30）の維持 ─────────────────────────────────

    [Fact]
    public void SaveMenuItems_ToolTipShowOnDisabled_IsUnchanged()
    {
        var xaml = ReadShellXaml();
        var occurrences = 0;
        var idx = 0;
        while ((idx = xaml.IndexOf("ToolTipService.ShowOnDisabled=\"True\"", idx, System.StringComparison.Ordinal)) >= 0)
        {
            occurrences++;
            idx += 1;
        }
        // SaveMenuItem・SaveAllMenuItem・SaveAsMenuItem・ピン留め・ピン留めを解除・このタブを閉じる の6件を想定する。
        Assert.Equal(6, occurrences);
    }
}
