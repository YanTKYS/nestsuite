using System.IO;
using System.Reflection;
using System.Windows.Input;
using NestSuite.IdeaNest.Views;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.1 ID-4 (TD-88必須範囲): IdeaNest カード一覧で、フォーカス中のカードに対する
/// Enter キーがプレビューを開くことの確認。実際のプレビュー表示処理
/// （<see cref="NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel.PreviewIdeaCommand"/>）は
/// 既存のマウスクリック経路（<c>OnCardMouseLeftButtonUp</c>）と共有しており、
/// 新しいプレビュー処理・新しい選択モデル・矢印キー移動・Space ピン留め・Delete 削除は
/// 追加していないことを、XAML の静的内容確認とメソッドシグネチャ確認で固定する
/// （既存 <c>IdeaNestNewCardPositionFeedbackTests</c> の XAML 確認パターンに合わせる）。
/// </summary>
public class IdeaNestCardEnterPreviewTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadWorkspaceViewXaml()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml");
        Assert.True(File.Exists(path), $"IdeaNestWorkspaceView.xaml not found: {path}");
        return File.ReadAllText(path);
    }

    private static string ReadWorkspaceViewCodeBehind()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml.cs");
        Assert.True(File.Exists(path), $"IdeaNestWorkspaceView.xaml.cs not found: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>OnCardKeyDown メソッド本体だけを抜き出す（対象外キーを追加していないことの確認に使う）。</summary>
    private static string ExtractOnCardKeyDownBody()
    {
        var src = ReadWorkspaceViewCodeBehind();
        var start = src.IndexOf("private void OnCardKeyDown", System.StringComparison.Ordinal);
        Assert.True(start >= 0, "OnCardKeyDown が見つからない");
        var braceStart = src.IndexOf('{', start);
        Assert.True(braceStart >= 0);
        var depth = 0;
        var i = braceStart;
        for (; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}')
            {
                depth--;
                if (depth == 0) break;
            }
        }
        return src.Substring(braceStart, i - braceStart + 1);
    }

    // ── XAML 配線の確認 ──────────────────────────────────────────────────

    [Fact]
    public void CardBorder_HasKeyDownHandler_ForEnterPreview()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("KeyDown=\"OnCardKeyDown\"", xaml);
    }

    [Fact]
    public void CardBorder_StillHasMouseLeftButtonUpHandler_NotDuplicated()
    {
        // 既存のマウスクリック経路（OnCardMouseLeftButtonUp）を維持し、
        // Enter 専用のプレビュー処理を複製していないことの確認。
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("MouseLeftButtonUp=\"OnCardMouseLeftButtonUp\"", xaml);
    }

    [Fact]
    public void CardBorder_StillFocusable_ForTabReachability()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("Focusable=\"True\"", xaml);
    }

    [Fact]
    public void UserControl_InputBindings_UnchangedByThisVersion()
    {
        // v2.19.1 ID-4: 新しいグローバルショートカットは追加していない
        // （既存の Ctrl+Shift+N/C/R のみ維持）。
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("Modifiers=\"Ctrl+Shift\" Key=\"N\"", xaml);
        Assert.Contains("Modifiers=\"Ctrl+Shift\" Key=\"C\"", xaml);
        Assert.Contains("Modifiers=\"Ctrl+Shift\" Key=\"R\"", xaml);
    }

    // ── コードビハインドの存在・共有処理の確認 ──────────────────────────

    [Fact]
    public void OnCardKeyDown_MethodExists_WithExpectedSignature()
    {
        var method = typeof(IdeaNestWorkspaceView)
            .GetMethod("OnCardKeyDown",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(object), typeof(KeyEventArgs)],
                null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void IsInsideButton_HelperStillExists_SharedWithMouseHandler()
    {
        // Enter ハンドラはマウスハンドラと同じ IsInsideButton 判定を再利用し、
        // フッターボタンにフォーカスがある場合の Enter を横取りしない。
        var method = typeof(IdeaNestWorkspaceView)
            .GetMethod("IsInsideButton", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
    }

    [Fact]
    public void OnCardKeyDown_CallsPreviewIdeaCommand_ReusesExistingPreviewPath()
    {
        // 新しいプレビュー処理を複製せず、既存の PreviewIdeaCommand をそのまま呼ぶことの確認。
        var body = ExtractOnCardKeyDownBody();
        Assert.Contains("PreviewIdeaCommand.Execute(card)", body);
    }

    [Fact]
    public void OnCardKeyDown_ChecksIsInsideButton_BeforeInvokingPreview()
    {
        var body = ExtractOnCardKeyDownBody();
        Assert.Contains("IsInsideButton(src)", body);
    }

    [Fact]
    public void OnCardKeyDown_SetsHandled_OnlyWhenPreviewOpened()
    {
        var body = ExtractOnCardKeyDownBody();
        Assert.Contains("e.Handled = true;", body);
    }

    // ── 対象外キー（矢印移動・Space ピン留め・Delete 削除）を追加していないこと ─

    [Theory]
    [InlineData("Key.Space")]
    [InlineData("Key.Delete")]
    [InlineData("Key.Back")]
    [InlineData("Key.Left")]
    [InlineData("Key.Right")]
    [InlineData("Key.Up")]
    [InlineData("Key.Down")]
    public void OnCardKeyDown_DoesNotHandleOutOfScopeKeys(string keyLiteral)
    {
        var body = ExtractOnCardKeyDownBody();
        Assert.DoesNotContain(keyLiteral, body);
    }

    [Fact]
    public void OnCardKeyDown_OnlyChecksEnterKey()
    {
        var body = ExtractOnCardKeyDownBody();
        Assert.Contains("Key.Enter", body);
    }
}
