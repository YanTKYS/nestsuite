using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.8 L8 (review1-fable5.md R-5): ヘルプメニュー「バックアップ復元ガイド」の
/// 案内文言を確認するテスト。自動復元・自動コピーは行わず、案内のみであることを前提とする。
/// </summary>
public class BackupRestoreGuideProviderTests
{
    [Fact]
    public void DialogTitle_IsBackupRestoreGuide()
    {
        Assert.Equal("バックアップ復元ガイド", BackupRestoreGuideProvider.DialogTitle);
    }

    [Fact]
    public void GetGuideText_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(BackupRestoreGuideProvider.GetGuideText()));
    }

    [Fact]
    public void GetGuideText_MentionsBakExtension()
    {
        Assert.Contains(".bak", BackupRestoreGuideProvider.GetGuideText());
    }

    [Fact]
    public void GetGuideText_MentionsLastManualSave()
    {
        Assert.Contains("最後の手動保存", BackupRestoreGuideProvider.GetGuideText());
    }

    [Fact]
    public void GetGuideText_MentionsEvacuatingOriginalFile()
    {
        Assert.Contains("元ファイルを退避", BackupRestoreGuideProvider.GetGuideText());
    }

    [Fact]
    public void GetGuideText_MentionsClosingFileBeforeRestore()
    {
        Assert.Contains("閉じ", BackupRestoreGuideProvider.GetGuideText());
    }

    [Fact]
    public void GetGuideText_MentionsCopyingBakFile()
    {
        Assert.Contains(".bak ファイルをコピー", BackupRestoreGuideProvider.GetGuideText());
    }

    [Fact]
    public void GetGuideText_MentionsAutoSaveDoesNotUpdateBak()
    {
        // v2.16.6 TD-64 の意味論（自動保存では .bak を更新しない）を前提として案内していること。
        Assert.Contains("自動保存では .bak を更新しません", BackupRestoreGuideProvider.GetGuideText());
    }

    [Fact]
    public void GetGuideText_DoesNotPromiseAutomaticRestore()
    {
        // 自動復元・自動コピー・自動リネームは行わない方針を、文言でも示唆しないことを確認する。
        var text = BackupRestoreGuideProvider.GetGuideText();
        Assert.DoesNotContain("自動的に復元", text);
        Assert.DoesNotContain("自動で復元", text);
    }

    [Fact]
    public void GetGuideText_IncludesWorkedExample()
    {
        var text = BackupRestoreGuideProvider.GetGuideText();
        Assert.Contains("project.notenest", text);
        Assert.Contains("project.notenest.bak", text);
    }
}
