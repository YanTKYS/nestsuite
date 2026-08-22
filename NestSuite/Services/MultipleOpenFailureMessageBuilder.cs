using System.IO;
using System.Linq;

namespace NestSuite.Services;

/// <summary>
/// 複数ファイルオープンで一部・全部が失敗した場合の
/// 通知文言を組み立てる。件数・ファイル名・理由を必ず示す
/// （「一部のファイルを開けませんでした。」だけでは利用者が対処できないため）。
///
/// 理由文言は既存の <see cref="FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure)"/> に揃え、
/// 1 行目のみを使う（InvalidFormat 等の複数行文言・.bak 案内を複数件表示で繰り返さないため）。
/// 1 件ずつ「- ファイル名: 理由」で列挙する形は、session 復元失敗通知と同じ様式。
///
/// 表示が長くなりすぎないよう、上位 <see cref="MaxListedFailures"/> 件のみ列挙し、
/// 残りは「ほか N 件」とまとめる。内部例外・型名は表示しない。UI に依存しない string を返すのみで、
/// 自動復元・自動変換・ファイル修復は一切行わない。
/// </summary>
public static class MultipleOpenFailureMessageBuilder
{
    internal const int MaxListedFailures = 5;

    public static string Build(IReadOnlyList<OpenFileFailure> failures)
    {
        var lines = failures
            .Take(MaxListedFailures)
            .Select(f => $"- {Path.GetFileName(f.Path)}: {FileErrorMessages.ForKindDetectionFailure(f.Failure).Split('\n')[0]}");

        var body = string.Join("\n", lines);
        var remaining = failures.Count - MaxListedFailures;
        if (remaining > 0)
            body += $"\n- ほか {remaining} 件";

        var message = $"{failures.Count} 件のファイルを開けませんでした。\n\n{body}";

        // InvalidFormat が 1 件でも含まれる場合のみ、
        // 単体で開き直すと詳しい .bak 復元案内が出ることを末尾に 1 行添える。
        if (failures.Any(f => f.Failure == WorkspaceKindDetectionFailure.InvalidFormat))
            message += "\n\n" + FileErrorMessages.MultipleFailuresBakDetailHint;

        return message;
    }
}
