using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// SH-44: Shell横断検索のEscapeクローズ・フォーカス復帰の回帰テスト。
/// フォーカス復帰の可否判定は <see cref="CrossSearchFocusRestoreLogic"/>（純粋関数）として
/// 分離してあるため、実際のWPF Focus()成功可否を不安定なUIテストで固定せずに確認できる。
/// 配線（Escape処理・×ボタンとの共通クローズ・フォールバック順）は
/// <c>NestSuiteShellWindow.CrossSearch.cs</c> / <c>.TabSelection.cs</c> のソースを
/// 静的に確認する（既存 SH41CrossSearchUnopenedTests と同じ方式）。
/// </summary>
public class SH44CrossSearchEscapeFocusTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadCrossSearchSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.CrossSearch.cs"));

    private static string ReadTabSelectionSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.TabSelection.cs"));

    private static string ReadShellXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml"));

    // ── フォーカス復帰判定（純粋関数） ─────────────────────────────────────

    [Fact]
    public void ShouldUseSavedFocus_ValidVisibleEnabledFocusable_IsTrue()
    {
        var state = new FocusRestoreCandidateState(Exists: true, IsVisible: true, IsEnabled: true, IsFocusable: true);
        Assert.True(CrossSearchFocusRestoreLogic.ShouldUseSavedFocus(state));
    }

    [Fact]
    public void ShouldUseSavedFocus_Missing_IsFalse()
    {
        Assert.False(CrossSearchFocusRestoreLogic.ShouldUseSavedFocus(FocusRestoreCandidateState.Missing));
    }

    [Theory]
    [InlineData(false, true, true, true)]   // 存在しない
    [InlineData(true, false, true, true)]   // 非表示
    [InlineData(true, true, false, true)]   // 無効
    [InlineData(true, true, true, false)]   // フォーカス不可
    public void ShouldUseSavedFocus_AnyConditionFalse_IsFalse(bool exists, bool visible, bool enabled, bool focusable)
    {
        var state = new FocusRestoreCandidateState(exists, visible, enabled, focusable);
        Assert.False(CrossSearchFocusRestoreLogic.ShouldUseSavedFocus(state));
    }

    // ── Escape配線 ──────────────────────────────────────────────────────────

    [Fact]
    public void TryHandleCrossSearchEscape_ChecksEscapeKey_PanelVisibility_AndFocusWithin()
    {
        var source = ReadCrossSearchSource();
        var start = source.IndexOf("private bool TryHandleCrossSearchEscape", StringComparison.Ordinal);
        Assert.True(start >= 0, "TryHandleCrossSearchEscape not found");
        var body = ExtractMethodBody(source, start);

        Assert.Contains("e.Key != Key.Escape", body);
        Assert.Contains("CrossSearchPanel.Visibility != Visibility.Visible", body);
        Assert.Contains("CrossSearchPanel.IsKeyboardFocusWithin", body);
        Assert.Contains("CloseCrossSearchPanel()", body);
        Assert.Contains("e.Handled = true", body);
    }

    [Fact]
    public void OnPreviewKeyDown_CallsTryHandleCrossSearchEscape_BeforeOtherShortcuts()
    {
        var source = ReadTabSelectionSource();
        var overrideIdx = source.IndexOf("protected override void OnPreviewKeyDown", StringComparison.Ordinal);
        var escapeCallIdx = source.IndexOf("TryHandleCrossSearchEscape(e)", StringComparison.Ordinal);
        var ctrlTabIdx = source.IndexOf("ctrl && e.Key == Key.Tab", StringComparison.Ordinal);

        Assert.True(overrideIdx >= 0);
        Assert.True(escapeCallIdx > overrideIdx);
        Assert.True(ctrlTabIdx > escapeCallIdx,
            "Escape handling should be checked before the Ctrl+Tab shortcut so it isn't shadowed.");
    }

    // ── ×ボタンとの共通化 ──────────────────────────────────────────────────

    [Fact]
    public void CloseButtonClick_AndEscape_BothCallSameSharedCloseMethod()
    {
        var source = ReadCrossSearchSource();

        // ×ボタン
        var clickHandlerIdx = source.IndexOf("CrossSearchCloseButton_Click(object sender, RoutedEventArgs e)", StringComparison.Ordinal);
        Assert.True(clickHandlerIdx >= 0);
        var clickLineEnd = source.IndexOf('\n', clickHandlerIdx);
        var clickLine = source[clickHandlerIdx..clickLineEnd];
        Assert.Contains("CloseCrossSearchPanel()", clickLine);

        // Escape も同じ CloseCrossSearchPanel() を呼ぶ（TryHandleCrossSearchEscape 内で確認済み）。
        var escapeMethodIdx = source.IndexOf("private bool TryHandleCrossSearchEscape", StringComparison.Ordinal);
        var escapeBody = ExtractMethodBody(source, escapeMethodIdx);
        Assert.Contains("CloseCrossSearchPanel()", escapeBody);

        // クローズ処理の実体（Visibility=Collapsed・Reset）は CloseCrossSearchPanel 内に一箇所だけ。
        var collapsedOccurrences = CountOccurrences(source, "CrossSearchPanel.Visibility = Visibility.Collapsed");
        Assert.Equal(1, collapsedOccurrences);
        var resetOccurrences = CountOccurrences(source, "_crossSearchViewModel?.Reset()");
        Assert.Equal(1, resetOccurrences);
    }

    [Fact]
    public void CloseCrossSearchPanel_HasRestorePreviousFocusParameter()
    {
        var source = ReadCrossSearchSource();
        Assert.Contains("private void CloseCrossSearchPanel(bool restorePreviousFocus = true)", source);
    }

    // ── フォーカス保存 ──────────────────────────────────────────────────────

    [Fact]
    public void ToggleCrossSearchPanel_SavesFocus_OnlyInOpenBranch_BeforeViewModelSetup()
    {
        var source = ReadCrossSearchSource();
        var start = source.IndexOf("private void ToggleCrossSearchPanel", StringComparison.Ordinal);
        var body = ExtractMethodBody(source, start);

        var saveIdx = body.IndexOf("_crossSearchPreviousFocus = Keyboard.FocusedElement;", StringComparison.Ordinal);
        var closeIdx = body.IndexOf("CloseCrossSearchPanel();", StringComparison.Ordinal);
        var viewModelIdx = body.IndexOf("_crossSearchViewModel ??=", StringComparison.Ordinal);

        Assert.True(saveIdx >= 0, "Focus should be saved in ToggleCrossSearchPanel");
        Assert.True(closeIdx >= 0 && closeIdx < saveIdx,
            "Already-open branch (which closes) must be checked before saving focus.");
        Assert.True(saveIdx < viewModelIdx,
            "Focus must be saved before the search ViewModel/panel is set up.");
    }

    [Fact]
    public void ClosePanel_ClearsSavedFocusReference()
    {
        var source = ReadCrossSearchSource();
        var start = source.IndexOf("private void CloseCrossSearchPanel", StringComparison.Ordinal);
        var body = ExtractMethodBody(source, start);

        Assert.Contains("_crossSearchPreviousFocus = null;", body);
    }

    // ── 二重クローズ防止 ────────────────────────────────────────────────────

    [Fact]
    public void CloseCrossSearchPanel_GuardsAgainstDoubleClose()
    {
        var source = ReadCrossSearchSource();
        var start = source.IndexOf("private void CloseCrossSearchPanel", StringComparison.Ordinal);
        var body = ExtractMethodBody(source, start);

        Assert.Contains("if (CrossSearchPanel.Visibility != Visibility.Visible)", body);
    }

    // ── フォールバック順 ────────────────────────────────────────────────────

    [Fact]
    public void RestoreFocus_FollowsFallbackOrder_SavedThenWorkspaceThenTabStripThenWindow()
    {
        var source = ReadCrossSearchSource();
        var start = source.IndexOf("private void RestoreFocusAfterCrossSearchClose", StringComparison.Ordinal);
        var body = ExtractMethodBody(source, start);

        var savedIdx = body.IndexOf("TryFocus(previousFocus as UIElement)", StringComparison.Ordinal);
        var workspaceIdx = body.IndexOf("TryFocusActiveWorkspaceDefault()", StringComparison.Ordinal);
        var tabStripIdx = body.IndexOf("TryFocus(TabStrip)", StringComparison.Ordinal);
        var windowIdx = body.IndexOf("Focus();", StringComparison.Ordinal);

        Assert.True(savedIdx >= 0 && workspaceIdx > savedIdx && tabStripIdx > workspaceIdx && windowIdx > tabStripIdx);
    }

    [Fact]
    public void ActiveWorkspaceDefaultFocus_ReusesExistingIdeaNestApi_NoLargeSwitch()
    {
        var source = ReadCrossSearchSource();
        var start = source.IndexOf("private bool TryFocusActiveWorkspaceDefault", StringComparison.Ordinal);
        var body = ExtractMethodBody(source, start);

        Assert.Contains("IdeaNestWorkspaceView.FocusWorkspace()", body);
        // 大量の Workspace 種別 switch を追加していないことの簡易確認（switch を使っていない）。
        Assert.DoesNotContain("switch", body);
    }

    // ── XAML・アクセシビリティ ──────────────────────────────────────────────

    [Fact]
    public void CrossSearchCloseButton_StillExists_ExactlyOnce()
    {
        var xaml = ReadShellXaml();
        var occurrences = CountOccurrences(xaml, "Click=\"CrossSearchCloseButton_Click\"");
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void NoNewCloseButtonOrRichTextControlsAdded()
    {
        var xaml = ReadShellXaml();
        Assert.DoesNotContain("CrossSearchEscapeButton", xaml);
        Assert.DoesNotContain("RichTextBox", xaml);
        Assert.DoesNotContain("FlowDocument", xaml);
    }

    [Fact]
    public void CloseButtonTooltip_MentionsEscape()
    {
        var xaml = ReadShellXaml();
        var idx = xaml.IndexOf("CrossSearchCloseButton_Click", StringComparison.Ordinal);
        var start = xaml.LastIndexOf("<Button", idx, StringComparison.Ordinal);
        var end = xaml.IndexOf("/>", idx, StringComparison.Ordinal);
        var block = xaml.Substring(start, end - start);
        Assert.Contains("Esc", block);
    }

    // ── dirty・保存形式への非影響（静的契約） ───────────────────────────────

    [Fact]
    public void CrossSearchSource_DoesNotReferenceModifiedOrSaveApis()
    {
        var source = ReadCrossSearchSource();
        Assert.DoesNotContain("IsModified = true", source);
        Assert.DoesNotContain(".Save(", source);
        Assert.DoesNotContain("UiSettings", source);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string ExtractMethodBody(string source, int methodStart)
    {
        var braceStart = source.IndexOf('{', methodStart);
        Assert.True(braceStart >= 0);
        var depth = 0;
        var i = braceStart;
        for (; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0) break;
            }
        }
        return source.Substring(braceStart, i - braceStart + 1);
    }

    private static int CountOccurrences(string source, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
