using NoteNest.NestSuite;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.7.2: ファイル単位タブの最小設計モデルを確認するテスト。
///
/// NestSuite の最終タブはツール単位ではなくファイル／作業単位（NestSuiteDocumentTab）であることを
/// 型・プロパティ・ファクトリ動作を通じて検証する。本格的な TabControl・ファイル I/O は対象外。
/// </summary>
public class NestSuiteDocumentTabTests
{
    // ── NestSuiteDocumentTab 基本構造 ────────────────────────────────────

    [Fact]
    public void DocumentTab_HasId()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "abc",
            WorkspaceKind = NestSuiteWorkspaceKind.NoteNest,
            DisplayName = "A.notenest",
        };
        Assert.Equal("abc", tab.Id);
    }

    [Fact]
    public void DocumentTab_HasWorkspaceKind()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "x",
            WorkspaceKind = NestSuiteWorkspaceKind.ChatNest,
            DisplayName = "会議メモ.chatnest",
        };
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
    }

    // ── ToolId は WorkspaceKind から導出される ──────────────────────────

    [Fact]
    public void DocumentTab_NoteNest_ToolId_IsNoteNest()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "1", WorkspaceKind = NestSuiteWorkspaceKind.NoteNest, DisplayName = "A.notenest",
        };
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, tab.ToolId);
    }

    [Fact]
    public void DocumentTab_ChatNest_ToolId_IsChatNest()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "2", WorkspaceKind = NestSuiteWorkspaceKind.ChatNest, DisplayName = "会議メモ.chatnest",
        };
        Assert.Equal(NestSuiteToolRegistry.ChatNestToolId, tab.ToolId);
    }

    [Fact]
    public void DocumentTab_IdeaNest_ToolId_IsIdeaNest()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "3", WorkspaceKind = NestSuiteWorkspaceKind.IdeaNest, DisplayName = "案出し.ideanest",
        };
        Assert.Equal(NestSuiteToolRegistry.IdeaNestToolId, tab.ToolId);
    }

    // ── IsUntitled / FilePath ────────────────────────────────────────────

    [Fact]
    public void DocumentTab_WithoutFilePath_IsUntitled()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "u", WorkspaceKind = NestSuiteWorkspaceKind.NoteNest, DisplayName = "無題.notenest",
        };
        Assert.True(tab.IsUntitled);
        Assert.Null(tab.FilePath);
    }

    [Fact]
    public void DocumentTab_WithFilePath_IsNotUntitled()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "f", WorkspaceKind = NestSuiteWorkspaceKind.NoteNest, DisplayName = "A.notenest",
            FilePath = @"C:\projects\A.notenest",
        };
        Assert.False(tab.IsUntitled);
        Assert.NotNull(tab.FilePath);
    }

    // ── IsModified ───────────────────────────────────────────────────────

    [Fact]
    public void DocumentTab_DefaultIsModified_IsFalse()
    {
        var tab = new NestSuiteDocumentTab
        {
            Id = "m", WorkspaceKind = NestSuiteWorkspaceKind.NoteNest, DisplayName = "A.notenest",
        };
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void DocumentTab_CanBeMarkedModified()
    {
        var original = new NestSuiteDocumentTab
        {
            Id = "m", WorkspaceKind = NestSuiteWorkspaceKind.ChatNest, DisplayName = "会議.chatnest",
        };
        // sealed record は with 式で非破壊更新できる（IsModified の反映を確認）
        var modified = original with { IsModified = true };
        Assert.False(original.IsModified);
        Assert.True(modified.IsModified);
    }

    // ── ツール定義とタブ定義の区別 ──────────────────────────────────────

    [Fact]
    public void ToolDefinitionAndDocumentTab_AreDistinctTypes()
    {
        // NestSuiteTool はツールの「機能定義」、NestSuiteDocumentTab は「開いている作業単位」
        // 型が分かれていることで混同を防ぐ
        Assert.NotEqual(typeof(NestSuiteTool), typeof(NestSuiteDocumentTab));
    }

    [Fact]
    public void MultipleDocumentTabs_CanHaveSameWorkspaceKind()
    {
        // 1 ツールから複数タブを開ける設計を表現できることを確認
        var tabA = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
        var tabB = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);

        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tabA.WorkspaceKind);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tabB.WorkspaceKind);
        Assert.NotEqual(tabA.Id, tabB.Id); // 別タブとして区別できる
    }

    // ── NestSuiteTabFactory ─────────────────────────────────────────────

    [Fact]
    public void TabFactory_CreateUntitled_NoteNest_IsUntitled()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);

        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
        Assert.True(tab.IsUntitled);
        Assert.False(tab.IsModified);
        Assert.NotEmpty(tab.Id);
    }

    [Fact]
    public void TabFactory_CreateUntitled_ChatNest_DisplayName_HasChatNestExtension()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);

        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
        Assert.EndsWith(".chatnest", tab.DisplayName, StringComparison.OrdinalIgnoreCase);
        Assert.True(tab.IsUntitled);
    }

    [Fact]
    public void TabFactory_CreateUntitled_IdeaNest_DisplayName_HasIdeaNestExtension()
    {
        // IdeaNest は v1.7.2 では未統合だが、タブモデルは定義済み
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.IdeaNest);

        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, tab.WorkspaceKind);
        Assert.EndsWith(".ideanest", tab.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TabFactory_FromFilePath_NoteNest_ResolvesCorrectly()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\project.notenest");

        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
        Assert.Equal("project.notenest", tab.DisplayName);
        Assert.Equal(@"C:\work\project.notenest", tab.FilePath);
        Assert.False(tab.IsUntitled);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void TabFactory_FromFilePath_ChatNest_ResolvesCorrectly()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\notes\会議メモ.chatnest");

        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
        Assert.Equal("会議メモ.chatnest", tab.DisplayName);
        Assert.False(tab.IsUntitled);
    }

    [Fact]
    public void TabFactory_FromFilePath_UnknownExtension_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            NestSuiteTabFactory.FromFilePath(@"C:\data\file.txt"));
    }

    [Fact]
    public void TabFactory_TryGetKind_NoteNestExtension_ReturnsNoteNest()
    {
        var result = NestSuiteTabFactory.TryGetKind(@"any\path\A.notenest", out var kind);

        Assert.True(result);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, kind);
    }

    [Fact]
    public void TabFactory_TryGetKind_UnknownExtension_ReturnsFalse()
    {
        var result = NestSuiteTabFactory.TryGetKind(@"file.unknown", out _);

        Assert.False(result);
    }

    // ── NestSuiteWorkspaceKind の全値確認 ───────────────────────────────

    [Fact]
    public void WorkspaceKind_HasThreeValues_NoteNest_ChatNest_IdeaNest()
    {
        var values = Enum.GetValues<NestSuiteWorkspaceKind>();
        Assert.Equal(3, values.Length);
        Assert.Contains(NestSuiteWorkspaceKind.NoteNest, values);
        Assert.Contains(NestSuiteWorkspaceKind.ChatNest, values);
        Assert.Contains(NestSuiteWorkspaceKind.IdeaNest, values);
    }

    [Fact]
    public void GetExtension_ReturnsCorrectExtension_ForEachKind()
    {
        Assert.Equal(".notenest", NestSuiteTabFactory.GetExtension(NestSuiteWorkspaceKind.NoteNest));
        Assert.Equal(".chatnest", NestSuiteTabFactory.GetExtension(NestSuiteWorkspaceKind.ChatNest));
        Assert.Equal(".ideanest", NestSuiteTabFactory.GetExtension(NestSuiteWorkspaceKind.IdeaNest));
    }

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(NestSuiteWorkspaceKind.ChatNest)]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest)]
    public void ExtensionMapping_RoundTrips(NestSuiteWorkspaceKind kind)
    {
        // GetExtension → TryGetKind の往復で元の kind に戻ることを確認
        // （ExtensionByKind と KindByExtension が整合していることの保証）
        var extension = NestSuiteTabFactory.GetExtension(kind);

        Assert.True(NestSuiteTabFactory.TryGetKind($"file{extension}", out var resolved));
        Assert.Equal(kind, resolved);
    }

    [Theory]
    [InlineData("FILE.NOTENEST", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData("FILE.CHATNEST", NestSuiteWorkspaceKind.ChatNest)]
    [InlineData("FILE.IDEANEST", NestSuiteWorkspaceKind.IdeaNest)]
    public void TryGetKind_IsCaseInsensitive_ForAllIntegratedTools(
        string filePath,
        NestSuiteWorkspaceKind expected)
    {
        Assert.True(NestSuiteTabFactory.TryGetKind(filePath, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("file.notenest", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData("file.chatnest", NestSuiteWorkspaceKind.ChatNest)]
    [InlineData("file.ideanest", NestSuiteWorkspaceKind.IdeaNest)]
    public void TryGetKind_DoesNotMisclassifyIntegratedExtensions(
        string filePath,
        NestSuiteWorkspaceKind expected)
    {
        Assert.True(NestSuiteTabFactory.TryGetKind(filePath, out var actual));
        Assert.Equal(expected, actual);
        Assert.All(
            Enum.GetValues<NestSuiteWorkspaceKind>().Where(kind => kind != expected),
            other => Assert.NotEqual(other, actual));
    }

    // ── v1.7.5: .notenest / .chatnest 拡張子の混同防止確認 ─────────────

    [Fact]
    public void TabFactory_FromFilePath_NoteNestExtension_IsNotChatNestKind()
    {
        // .notenest ファイルは ChatNest タブとして誤って解釈されないことを確認する
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\project.notenest");

        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
        Assert.NotEqual(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
    }

    [Fact]
    public void TabFactory_FromFilePath_ChatNestExtension_IsNotNoteNestKind()
    {
        // .chatnest ファイルは NoteNest タブとして誤って解釈されないことを確認する
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\notes\会議メモ.chatnest");

        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
        Assert.NotEqual(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
    }

    [Fact]
    public void TabFactory_TryGetKind_ChatNestExtension_ReturnsCorrectKind()
    {
        var result = NestSuiteTabFactory.TryGetKind("session.chatnest", out var kind);

        Assert.True(result);
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, kind);
    }

    // ── v1.7.8: IdeaNest 統合前の基盤確認 ──────────────────────────────────

    [Fact]
    public void TabFactory_FromFilePath_IdeaNestExtension_ResolvesCorrectly()
    {
        // v1.7.8: IdeaNest 統合検証（v1.8.0 予定）の前に、タブモデルが .ideanest を正しく扱えることを確認する
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\ideas\brainstorm.ideanest");

        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, tab.WorkspaceKind);
        Assert.Equal("brainstorm.ideanest", tab.DisplayName);
        Assert.Equal(@"C:\ideas\brainstorm.ideanest", tab.FilePath);
        Assert.False(tab.IsUntitled);
        Assert.False(tab.IsModified);
        Assert.Equal(NestSuiteToolRegistry.IdeaNestToolId, tab.ToolId);
    }

    [Fact]
    public void TabFactory_TryGetKind_IdeaNestExtension_ReturnsIdeaNest()
    {
        // v1.7.8: .ideanest 拡張子が NestSuiteWorkspaceKind.IdeaNest に解決されることを確認する
        var result = NestSuiteTabFactory.TryGetKind("project.ideanest", out var kind);

        Assert.True(result);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, kind);
    }

    // ── v1.9.9: TooltipText ─────────────────────────────────────────────

    [Fact]
    public void TooltipText_NoteNest_SavedTab_ContainsKindAndPath()
    {
        // v1.9.9: ツールチップにツール種別・ファイルパス・保存状態が含まれることを確認
        var tab = new NestSuiteDocumentTab
        {
            Id = "t", WorkspaceKind = NestSuiteWorkspaceKind.NoteNest,
            DisplayName = "A.notenest", FilePath = @"C:\work\A.notenest", IsModified = false,
        };
        Assert.Contains("NoteNest", tab.TooltipText);
        Assert.Contains(@"C:\work\A.notenest", tab.TooltipText);
        Assert.Contains("保存済み", tab.TooltipText);
    }

    [Fact]
    public void TooltipText_UntitledTab_ShowsUntitledAndUnsaved()
    {
        // v1.9.9: 無題タブのツールチップに「未保存（無題）」と「保存済み」が含まれる
        var tab = new NestSuiteDocumentTab
        {
            Id = "u", WorkspaceKind = NestSuiteWorkspaceKind.ChatNest,
            DisplayName = "無題.chatnest",
        };
        Assert.Contains("ChatNest", tab.TooltipText);
        Assert.Contains("未保存（無題）", tab.TooltipText);
    }

    [Fact]
    public void TooltipText_ModifiedTab_ShowsUnsavedState()
    {
        // v1.9.9: IsModified=true のタブのツールチップに「未保存の変更あり」が含まれる
        var tab = new NestSuiteDocumentTab
        {
            Id = "m", WorkspaceKind = NestSuiteWorkspaceKind.IdeaNest,
            DisplayName = "案出し.ideanest", FilePath = @"C:\ideas\案出し.ideanest", IsModified = true,
        };
        Assert.Contains("IdeaNest", tab.TooltipText);
        Assert.Contains("未保存の変更あり", tab.TooltipText);
    }

    [Fact]
    public void TooltipText_SavedTab_ShowsSavedState()
    {
        // v1.9.9: IsModified=false のタブのツールチップに「保存済み」が含まれ「未保存の変更あり」は含まれない
        var tab = new NestSuiteDocumentTab
        {
            Id = "s", WorkspaceKind = NestSuiteWorkspaceKind.NoteNest,
            DisplayName = "B.notenest", FilePath = @"C:\work\B.notenest", IsModified = false,
        };
        Assert.Contains("保存済み", tab.TooltipText);
        Assert.DoesNotContain("未保存の変更あり", tab.TooltipText);
    }

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest, "NoteNest")]
    [InlineData(NestSuiteWorkspaceKind.ChatNest, "ChatNest")]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest, "IdeaNest")]
    public void TooltipText_ContainsCorrectKindLabel_ForEachTool(NestSuiteWorkspaceKind kind, string expectedKindLabel)
    {
        // v1.9.9: ツールチップにツール種別が正しく含まれることを3ツール横断で確認
        var tab = NestSuiteTabFactory.CreateUntitled(kind);
        Assert.Contains(expectedKindLabel, tab.TooltipText);
    }
}
