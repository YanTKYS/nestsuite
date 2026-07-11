using NestSuite;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.Models;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.35 TD-59b-2 (nestsuite-double-read-design-review.md §8.6, §10, §16):
/// 3 Workspace の <c>LoadPrepared</c> にまたがる誤配線シナリオを横断的に確認する。
/// 各 FileService 単体の LoadPrepared テストは ProjectFileServiceTests / IdeaNestFileServiceTests /
/// ChatNestFileServiceTests にある。ここでは以下の 3 つの安全性マトリクスだけを扱う。
/// <list type="bullet">
///   <item>同種ファイル間の path 取り違え（A の envelope + B の path、同じ WorkspaceKind）</item>
///   <item>WorkspaceKind 誤配線（正しく生成された context を「別 Workspace の」FileService へ渡す）6 通り</item>
///   <item>context enum 改変（wrapper の kind は正しいが context.WorkspaceKind だけ異なる不正 context）</item>
/// </list>
/// </summary>
public class WorkspaceLoadPreparedSafetyTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "WorkspaceLoadPreparedSafetyTests_" + Guid.NewGuid().ToString("N"));

    public WorkspaceLoadPreparedSafetyTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(string name) => Path.Combine(_tempDir, name);

    // ── 同種ファイル間の path 取り違え（§9） ────────────────────────────────

    [Fact]
    public void PathMismatch_NoteNest_SameWorkspaceKind_ThrowsArgumentException()
    {
        var pathA = TempPath("notenest-A.nestsuite");
        var pathB = TempPath("notenest-B.nestsuite");
        new ProjectFileService().Save(pathA, new Project { ProjectName = "A" });
        new ProjectFileService().Save(pathB, new Project { ProjectName = "B" });
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(pathA, out var contextA, out _));

        var mismatched = WorkspaceFileOpenContextTestFactory.Create(
            pathB, NestSuiteWorkspaceKind.NoteNest, contextA.Preloaded);

        Assert.Throws<ArgumentException>(() => new ProjectFileService().LoadPrepared(mismatched));
    }

    [Fact]
    public void PathMismatch_IdeaNest_SameWorkspaceKind_ThrowsArgumentException()
    {
        var pathA = TempPath("ideanest-A.nestsuite");
        var pathB = TempPath("ideanest-B.nestsuite");
        IdeaNestFileService.Save(pathA, new Workspace { WorkspaceName = "A", Ideas = new() });
        IdeaNestFileService.Save(pathB, new Workspace { WorkspaceName = "B", Ideas = new() });
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(pathA, out var contextA, out _));

        var mismatched = WorkspaceFileOpenContextTestFactory.Create(
            pathB, NestSuiteWorkspaceKind.IdeaNest, contextA.Preloaded);

        Assert.Throws<ArgumentException>(() => IdeaNestFileService.LoadPrepared(mismatched));
    }

    [Fact]
    public void PathMismatch_ChatNest_SameWorkspaceKind_ThrowsArgumentException()
    {
        var pathA = TempPath("chatnest-A.nestsuite");
        var pathB = TempPath("chatnest-B.nestsuite");
        ChatNestFileService.Save(pathA, [new Message { Speaker = Speaker.自分, Text = "A" }]);
        ChatNestFileService.Save(pathB, [new Message { Speaker = Speaker.自分, Text = "B" }]);
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(pathA, out var contextA, out _));

        var mismatched = WorkspaceFileOpenContextTestFactory.Create(
            pathB, NestSuiteWorkspaceKind.ChatNest, contextA.Preloaded);

        var ex = Assert.Throws<ArgumentException>(() => ChatNestFileService.LoadPrepared(mismatched));
        // EnsureKind（wrapper 内容の不一致）ではなく path 一致ガードで失敗していることを、
        // 全文一致ではなくキーワードのみで確認する。
        Assert.Contains("パス", ex.Message);
    }

    // ── WorkspaceKind 誤配線（§10）: 6 通り ──────────────────────────────

    [Fact]
    public void KindMiswiring_NoteNestContext_ToIdeaNestAndChatNestFileService_ThrowsInvalidDataException()
    {
        var path = TempPath("notenest-context.nestsuite");
        new ProjectFileService().Save(path, new Project { ProjectName = "X" });
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _));
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, context.WorkspaceKind);

        Assert.Throws<InvalidDataException>(() => IdeaNestFileService.LoadPrepared(context));
        Assert.Throws<InvalidDataException>(() => ChatNestFileService.LoadPrepared(context));
    }

    [Fact]
    public void KindMiswiring_IdeaNestContext_ToNoteNestAndChatNestFileService_ThrowsInvalidDataException()
    {
        var path = TempPath("ideanest-context.nestsuite");
        IdeaNestFileService.Save(path, new Workspace { WorkspaceName = "X", Ideas = new() });
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _));
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, context.WorkspaceKind);

        Assert.Throws<InvalidDataException>(() => new ProjectFileService().LoadPrepared(context));
        Assert.Throws<InvalidDataException>(() => ChatNestFileService.LoadPrepared(context));
    }

    [Fact]
    public void KindMiswiring_ChatNestContext_ToNoteNestAndIdeaNestFileService_ThrowsInvalidDataException()
    {
        var path = TempPath("chatnest-context.nestsuite");
        ChatNestFileService.Save(path, [new Message { Speaker = Speaker.自分, Text = "X" }]);
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _));
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, context.WorkspaceKind);

        Assert.Throws<InvalidDataException>(() => new ProjectFileService().LoadPrepared(context));
        Assert.Throws<InvalidDataException>(() => IdeaNestFileService.LoadPrepared(context));
    }

    // ── context enum 改変（§11） ─────────────────────────────────────────

    [Fact]
    public void EnumTampering_NoteNestEnvelope_ContextWorkspaceKindChangedToIdeaNest_ThrowsArgumentException()
    {
        var path = TempPath("enumtamper-notenest.nestsuite");
        new ProjectFileService().Save(path, new Project { ProjectName = "X" });
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _));

        // wrapper の workspaceKind は NoteNest のまま（EnsureKind は通過する）で、
        // context.WorkspaceKind だけを改変した不正 context。
        var tampered = WorkspaceFileOpenContextTestFactory.Create(
            context.FilePath, NestSuiteWorkspaceKind.IdeaNest, context.Preloaded);

        Assert.Throws<ArgumentException>(() => new ProjectFileService().LoadPrepared(tampered));
    }

    [Fact]
    public void EnumTampering_IdeaNestEnvelope_ContextWorkspaceKindChangedToChatNest_ThrowsArgumentException()
    {
        var path = TempPath("enumtamper-ideanest.nestsuite");
        IdeaNestFileService.Save(path, new Workspace { WorkspaceName = "X", Ideas = new() });
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _));

        var tampered = WorkspaceFileOpenContextTestFactory.Create(
            context.FilePath, NestSuiteWorkspaceKind.ChatNest, context.Preloaded);

        Assert.Throws<ArgumentException>(() => IdeaNestFileService.LoadPrepared(tampered));
    }

    [Fact]
    public void EnumTampering_ChatNestEnvelope_ContextWorkspaceKindChangedToNoteNest_ThrowsArgumentException()
    {
        var path = TempPath("enumtamper-chatnest.nestsuite");
        ChatNestFileService.Save(path, [new Message { Speaker = Speaker.自分, Text = "X" }]);
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(path, out var context, out _));

        // wrapper の workspaceKind は ChatNest のまま（呼び先も ChatNestFileService のままにして
        // EnsureKind を通過させる）で、context.WorkspaceKind だけを改変した不正 context。
        var tampered = WorkspaceFileOpenContextTestFactory.Create(
            context.FilePath, NestSuiteWorkspaceKind.NoteNest, context.Preloaded);

        Assert.Throws<ArgumentException>(() => ChatNestFileService.LoadPrepared(tampered));
    }
}
