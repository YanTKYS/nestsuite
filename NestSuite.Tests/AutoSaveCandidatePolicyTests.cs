using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.14.12 SH-33: <see cref="AutoSaveCandidatePolicy"/> の自動保存対象判定を固定する。
/// UI 非依存の純粋ロジックのため <c>NestSuiteShellWindow</c> を生成せず直接テストできる
/// （Shell 上の private ヘルパーは WPF ウィンドウに依存するため直接テストしない、という
/// 既存方針とは別に、この policy 自体は <see cref="NestSuiteOpenFilePolicy"/> と同じ位置づけ）。
/// </summary>
public class AutoSaveCandidatePolicyTests
{
    [Fact]
    public void IsCandidate_NoteNest_WithFilePathAndDirty_ReturnsTrue()
    {
        Assert.True(AutoSaveCandidatePolicy.IsCandidate(
            NestSuiteWorkspaceKind.NoteNest, "a.notenest", isModified: true));
    }

    [Fact]
    public void IsCandidate_IdeaNest_WithFilePathAndDirty_ReturnsTrue()
    {
        Assert.True(AutoSaveCandidatePolicy.IsCandidate(
            NestSuiteWorkspaceKind.IdeaNest, "a.ideanest", isModified: true));
    }

    [Fact]
    public void IsCandidate_ChatNest_WithFilePathAndDirty_ReturnsTrue()
    {
        Assert.True(AutoSaveCandidatePolicy.IsCandidate(
            NestSuiteWorkspaceKind.ChatNest, "a.chatnest", isModified: true));
    }

    [Fact]
    public void IsCandidate_Temp_ReturnsFalse_EvenWithFilePathAndDirty()
    {
        // TempNest は専用の保存機構（TempNestStoreService）を持つため常に対象外。
        Assert.False(AutoSaveCandidatePolicy.IsCandidate(
            NestSuiteWorkspaceKind.Temp, "a.notenest", isModified: true));
    }

    [Fact]
    public void IsCandidate_NullFilePath_ReturnsFalse()
    {
        // 新規未保存タブ（FilePath なし）は自動保存対象外。勝手に保存場所を作らない。
        Assert.False(AutoSaveCandidatePolicy.IsCandidate(
            NestSuiteWorkspaceKind.NoteNest, null, isModified: true));
    }

    [Fact]
    public void IsCandidate_NotModified_ReturnsFalse()
    {
        Assert.False(AutoSaveCandidatePolicy.IsCandidate(
            NestSuiteWorkspaceKind.NoteNest, "a.notenest", isModified: false));
    }

    [Fact]
    public void IsCandidate_NullFilePathAndNotModified_ReturnsFalse()
    {
        Assert.False(AutoSaveCandidatePolicy.IsCandidate(
            NestSuiteWorkspaceKind.NoteNest, null, isModified: false));
    }
}
