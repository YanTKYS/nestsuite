using System.IO;
using System.Linq;

namespace NestSuite.Services;

/// <summary>
/// session 復元失敗の通知文言を組み立てる。
/// UI に依存しない string を返すのみで、ダイアログの表示種別（<c>ShowError</c> / <c>Confirm</c>）の
/// 選択は呼び元（<see cref="NestSuiteShellWindow"/>）の責務として残す。
///
/// FileNotFound を含む場合は 1 つの Confirm へ統合し、含まない場合は ShowError のみとする。
/// <see cref="BuildFailuresMessage"/> は両方のケースで共通の本文（一覧 + .bak 誘導）を返し、
/// <see cref="ForgetFileNotFoundQuestion"/> は FileNotFound を含む場合のみ呼び元が末尾に連結する。
/// </summary>
public static class SessionRestoreFailuresMessageBuilder
{
    /// <summary>
    /// 一覧本文（ファイル名 + 理由の 1 行目）。
    /// InvalidFormat が 1 件でも含まれる場合のみ、
    /// 単体で開き直すと詳しい .bak 復元案内が出ることを末尾に 1 行添える。
    /// </summary>
    public static string BuildFailuresMessage(IReadOnlyList<SessionRestoreFailure> failures)
    {
        var lines = failures.Select(f =>
            $"- {Path.GetFileName(f.FilePath)}: {FileErrorMessages.ForKindDetectionFailure(f.Failure).Split('\n')[0]}");
        var message =
            "前回開いていた一部のファイルを復元できませんでした。\n次回起動時にも再試行します。\n\n" +
            string.Join("\n", lines);

        if (failures.Any(f => f.Failure == WorkspaceKindDetectionFailure.InvalidFormat))
            message += "\n\n" + FileErrorMessages.MultipleFailuresBakDetailHint;

        return message;
    }

    /// <summary>
    /// 解除対象は FileNotFound のみであることを明示し、外部/ネットワークドライブ未接続の
    /// 可能性がある場合に誤って「はい」を選ばれないよう注意文を添える。
    /// </summary>
    public const string ForgetFileNotFoundQuestion =
        "見つからないファイルを、次回から復元対象から外しますか？\n\n" +
        "ファイルを移動・削除した場合は「はい」を選んでください。\n" +
        "外部ドライブやネットワークドライブが一時的に接続されていないだけの場合は「いいえ」を選んでください。\n\n" +
        "「はい」を選んでも、見つからないファイル以外は引き続き再試行されます。";
}
