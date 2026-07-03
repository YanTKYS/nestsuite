using System.IO;
using System.Linq;
using NestSuite.FileAssociation;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// FileAssociationService のレジストリ非接触テスト。
/// AssociationTargets（単一情報源）と PowerShell スクリプト 2 本の整合性を検証する。
/// 実レジストリ操作（Register / Unregister / GetStatus）は CI 環境依存のためここでは検証しない。
/// </summary>
public class FileAssociationServiceTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    [Fact]
    public void AssociationTargets_ContainsNestsuite_AsNestSuiteWorkspace()
    {
        var entry = FileAssociationService.AssociationTargets
            .SingleOrDefault(t => t.Ext == ".nestsuite");

        Assert.NotEqual(default, entry);
        Assert.Equal("NoteNest.nestsuite", entry.ProgId);
        Assert.Equal("NestSuite Workspace", entry.Description);
    }

    [Fact]
    public void AssociationTargets_ContainsAllLegacyExtensions_Unchanged()
    {
        var targets = FileAssociationService.AssociationTargets;

        var notenest = targets.Single(t => t.Ext == ".notenest");
        Assert.Equal("NoteNest.notenest", notenest.ProgId);
        Assert.Equal("NoteNest Document", notenest.Description);

        var chatnest = targets.Single(t => t.Ext == ".chatnest");
        Assert.Equal("NoteNest.chatnest", chatnest.ProgId);
        Assert.Equal("ChatNest Document", chatnest.Description);

        var ideanest = targets.Single(t => t.Ext == ".ideanest");
        Assert.Equal("NoteNest.ideanest", ideanest.ProgId);
        Assert.Equal("IdeaNest Document", ideanest.Description);
    }

    [Fact]
    public void AssociationTargets_HasExactlyFourEntries_NoDuplicates()
    {
        var targets = FileAssociationService.AssociationTargets;

        Assert.Equal(4, targets.Count);

        var distinctExts = targets
            .Select(t => t.Ext)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .Count();
        Assert.Equal(4, distinctExts);
    }

    [Fact]
    public void RegisterScript_ContainsAllTargetExtensionsAndProgIds()
    {
        var path = Path.Combine(RepoRoot, "tools", "register-nestsuite-file-association.ps1");
        Assert.True(File.Exists(path), $"script not found: {path}");
        var content = File.ReadAllText(path);

        foreach (var target in FileAssociationService.AssociationTargets)
        {
            Assert.Contains(target.Ext, content);
            Assert.Contains(target.ProgId, content);
        }
    }

    [Fact]
    public void UnregisterScript_ContainsAllTargetExtensionsAndProgIds()
    {
        var path = Path.Combine(RepoRoot, "tools", "unregister-nestsuite-file-association.ps1");
        Assert.True(File.Exists(path), $"script not found: {path}");
        var content = File.ReadAllText(path);

        foreach (var target in FileAssociationService.AssociationTargets)
        {
            Assert.Contains(target.Ext, content);
            Assert.Contains(target.ProgId, content);
        }
    }
}
