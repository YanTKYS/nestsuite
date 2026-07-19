using System.IO;
using System.Threading;
using NestSuite;
using NestSuite.FileAssociation;
using NestSuite.PlainText;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.0 SH-43: PlainText（.txt）が既存の共通 Open 計画・タブ生成・session mapper・
/// 自動保存対象判定へ正しく合流していることを確認する。独自の並行 Open 経路を持たないことの回帰。
/// </summary>
public class PlainTextTabIntegrationTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public PlainTextTabIntegrationTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(string name) => Path.Combine(_tempDir, name);

    // ── NestSuiteTabFactory ──────────────────────────────────────────────

    [Fact]
    public void GetExtension_PlainText_ReturnsTxt()
    {
        Assert.Equal(".txt", NestSuiteTabFactory.GetExtension(NestSuiteWorkspaceKind.PlainText));
    }

    [Fact]
    public void CreateUntitled_PlainText_HasExpectedDefaults()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.PlainText);

        Assert.Equal(NestSuiteWorkspaceKind.PlainText, tab.WorkspaceKind);
        Assert.Null(tab.FilePath);
        Assert.False(tab.IsModified);
        Assert.Equal("無題.txt", tab.DisplayName);
    }

    [Fact]
    public void FromFilePath_TxtExtension_ResolvesToPlainText()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\memo.txt");
        Assert.Equal(NestSuiteWorkspaceKind.PlainText, tab.WorkspaceKind);
    }

    [Fact]
    public void TryPrepareOpen_TxtFile_ReturnsLegacyStyleContext_NoPreload()
    {
        var path = TempPath("note.txt");
        PlainTextFileService.Save(path, "hello", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        var success = NestSuiteTabFactory.TryPrepareOpen(path, out var context, out var failure);

        Assert.True(success);
        Assert.Equal(WorkspaceKindDetectionFailure.None, failure);
        Assert.Equal(NestSuiteWorkspaceKind.PlainText, context.WorkspaceKind);
        Assert.Null(context.Preloaded);
    }

    [Fact]
    public void IsPathCompatibleWithResolvedKind_TxtPath_MatchesPlainTextOnly()
    {
        Assert.True(NestSuiteTabFactory.IsPathCompatibleWithResolvedKind(@"C:\work\a.txt", NestSuiteWorkspaceKind.PlainText));
        Assert.False(NestSuiteTabFactory.IsPathCompatibleWithResolvedKind(@"C:\work\a.txt", NestSuiteWorkspaceKind.NoteNest));
    }

    [Fact]
    public void IsPathCompatibleWithResolvedKind_NestsuitePath_DoesNotIncludePlainText()
    {
        // .txt は .nestsuite wrapper へ格納しないため、.nestsuite 拡張子は PlainText と互換にならない。
        Assert.False(NestSuiteTabFactory.IsPathCompatibleWithResolvedKind(@"C:\work\a.nestsuite", NestSuiteWorkspaceKind.PlainText));
    }

    // ── NestSuiteDocumentTab ─────────────────────────────────────────────

    [Fact]
    public void Tab_PlainText_IsDetachableWhenNotDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.PlainText);
        Assert.True(tab.IsDetachable);
        Assert.True(tab.IsPlainText);
    }

    [Fact]
    public void Tab_PlainText_NotDetachableWhenAlreadyDetached()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.PlainText) with { IsDetached = true };
        Assert.False(tab.IsDetachable);
    }

    [Fact]
    public void Tab_PlainText_ToolIdIsDistinctFromNestToolIds()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.PlainText);
        Assert.NotEqual(NestSuiteToolRegistry.NoteNestToolId, tab.ToolId);
        Assert.NotEqual(NestSuiteToolRegistry.IdeaNestToolId, tab.ToolId);
        Assert.NotEqual(NestSuiteToolRegistry.ChatNestToolId, tab.ToolId);
    }

    [Fact]
    public void Tab_PlainText_IsNotRegisteredInToolRegistry()
    {
        // v2.19.0 SH-43: PlainText は Nest ではないため NestSuiteToolRegistry に登録しない。
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.PlainText);
        Assert.DoesNotContain(NestSuiteToolRegistry.ToolDefinitions, t => t.Id == tab.ToolId);
    }

    [Fact]
    public void Tab_PlainText_TooltipText_ContainsPlainTextLabel()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.PlainText);
        Assert.Contains("テキスト", tab.TooltipText);
    }

    // ── AutoSaveCandidatePolicy ──────────────────────────────────────────

    [Fact]
    public void AutoSaveCandidatePolicy_PlainText_SavedAndModified_IsCandidate()
    {
        Assert.True(AutoSaveCandidatePolicy.IsCandidate(NestSuiteWorkspaceKind.PlainText, @"C:\work\a.txt", isModified: true));
    }

    [Fact]
    public void AutoSaveCandidatePolicy_PlainText_UntitledNewTab_IsNotCandidate()
    {
        // 新規に保存場所を作らない方針（NoteNest/IdeaNest/ChatNest と同じ）。
        Assert.False(AutoSaveCandidatePolicy.IsCandidate(NestSuiteWorkspaceKind.PlainText, filePath: null, isModified: true));
    }

    [Fact]
    public void AutoSaveCandidatePolicy_PlainText_NotModified_IsNotCandidate()
    {
        Assert.False(AutoSaveCandidatePolicy.IsCandidate(NestSuiteWorkspaceKind.PlainText, @"C:\work\a.txt", isModified: false));
    }

    // ── DraftCandidatePolicy: .txt は下書き復元の対象外（初期対象外の明示） ─

    [Fact]
    public void DraftCandidatePolicy_PlainText_IsNotSupportedWorkspace()
    {
        Assert.False(DraftCandidatePolicy.IsSupportedWorkspace(NestSuiteWorkspaceKind.PlainText));
    }

    [Fact]
    public void DraftCandidatePolicy_PlainText_NeverCandidate()
    {
        Assert.False(DraftCandidatePolicy.IsCandidate(NestSuiteWorkspaceKind.PlainText, filePath: null, hasDraftableChanges: true));
    }

    // ── SessionTabMapper: .txt は既存の汎用経路のみで復元できる ──────────

    [Fact]
    public void SessionTabMapper_TryCreateSessionTabState_PlainTextSavedTab_IsPersistable()
    {
        var tab = NestSuiteTabFactory.FromFilePath(@"C:\work\memo.txt");
        var created = SessionTabMapper.TryCreateSessionTabState(tab, out var state);

        Assert.True(created);
        Assert.Equal(@"C:\work\memo.txt", state.FilePath);
    }

    [Fact]
    public void SessionTabMapper_CreateRestoreTargets_PlainTextEntry_ResolvesToPlainTextKind()
    {
        var path = TempPath("restore.txt");
        PlainTextFileService.Save(path, "restore content", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        var state = new NestSuiteSessionState
        {
            FilePaths = new List<string> { path },
            Tabs = new List<NestSuiteSessionTabState>
            {
                new() { FilePath = path, WorkspaceKind = "PlainText", IsPinned = false },
            },
        };

        var targets = SessionTabMapper.CreateRestoreTargets(state, fileExists: File.Exists);

        Assert.Single(targets);
        Assert.Equal(NestSuiteWorkspaceKind.PlainText, targets[0].WorkspaceKind);
    }

    // ── NestSuiteOpenFilePolicy: .txt の重複判定は他 legacy 拡張子と同方針 ─

    [Fact]
    public void OpenFilePolicy_IsDuplicateForSave_SameTxtPathSameKind_IsDuplicate()
    {
        Assert.True(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            @"C:\work\a.txt", NestSuiteWorkspaceKind.PlainText,
            @"C:\work\a.txt", NestSuiteWorkspaceKind.PlainText));
    }

    [Fact]
    public void OpenFilePolicy_IsDuplicateForSave_SameTxtPathDifferentKind_IsNotDuplicate()
    {
        // .txt は拡張子だけで種別が一意に定まる legacy 形式（.nestsuite wrapper ではない）。
        Assert.False(NestSuiteOpenFilePolicy.IsDuplicateForSave(
            @"C:\work\a.txt", NestSuiteWorkspaceKind.NoteNest,
            @"C:\work\a.txt", NestSuiteWorkspaceKind.PlainText));
    }

    // ── SH-41 未オープンファイル候補列挙: .txt は本文検索対象へ含めない ────

    [Fact]
    public void UnopenedRecentFileLoader_TxtFile_IsSkippedSafely_NoDocument()
    {
        // v2.19.0 SH-43: PlainTextWorkspace の本文は初期実装で横断検索の対象へ含めない。
        // .txt を候補に含めても例外にならず、Document=null（検索対象外）として安全にスキップされること。
        var path = TempPath("unopened.txt");
        PlainTextFileService.Save(path, "not searched", PlainTextEncodingKind.Utf8NoBom, PlainTextNewlineKind.None);

        var results = UnopenedRecentFileLoader.Load(new[] { path }, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(path, results[0].FilePath);
        Assert.Null(results[0].Document);
    }

    // ── FileAssociationService: .txt の関連付けは追加しない（初期対象外） ─

    [Fact]
    public void FileAssociationService_DoesNotRegisterTxtExtension()
    {
        // v2.19.0 SH-43: Windows 標準の .txt 関連付けを奪わないため、初期実装では .txt を
        // ファイル関連付け対象へ追加しない（NestSuite EXE への引数渡しでは開ける）。
        Assert.DoesNotContain(FileAssociationService.AssociationTargets, t => t.Ext == ".txt");
    }
}
