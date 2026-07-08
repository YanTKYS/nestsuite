using System.IO;
using System.Security;
using System.Text.Json;

namespace NestSuite.Services;

/// <summary>
/// ファイル操作例外をユーザー向け日本語メッセージへ変換するヘルパー。
/// ex.Message をそのまま表示せず、原因別に短く具体的なメッセージを返す。
/// </summary>
public static class FileErrorMessages
{
    /// <summary>ファイル読込失敗時のユーザー向けメッセージを返す。</summary>
    public static string ForLoad(Exception ex) => ForLoad(ex, path: null);

    /// <summary>
    /// v2.16.8 L20 (review1-fable5.md R-5): path が分かる場合、対象と同じ場所に
    /// `.bak` が実在するかどうかで案内を出し分ける。破損・形式不正など、`.bak` からの
    /// 手動復元が意味を持つ失敗にのみ案内を付ける（ファイル不存在・権限等には付けない）。
    /// v2.16.6 TD-64 以降、自動保存では `.bak` を更新しないため「最後の手動保存時点の
    /// 復元候補」として案内する。自動復元やコピーはここでは行わない（案内のみ）。
    /// </summary>
    public static string ForLoad(Exception ex, string? path) => ex switch
    {
        // v2.14.4 FM-4: 新しい schema の検出は「破損」と区別し、理由と対処が分かる文言にする。
        // SchemaVersionTooNewException は Exception を直接継承する専用型のため、
        // より汎用の分岐（JsonException 等）に巻き込まれないよう先に置く。
        SchemaVersionTooNewException
            => "このファイルは、より新しいバージョンの NestSuite で作成された可能性があります。\n現在のバージョンでは安全に開けません。新しいバージョンの NestSuite で開いてください。",
        FileNotFoundException or DirectoryNotFoundException
            => "ファイルが見つかりません。移動または削除された可能性があります。",
        UnauthorizedAccessException or SecurityException
            => "アクセス権限がありません。ファイルの権限を確認してください。",
        JsonException
            => "ファイル形式を読み取れません。ファイルが破損している可能性があります。" + BackupRestoreHint(path),
        PathTooLongException
            => "ファイルパスが長すぎます。短いパスへ移動してから開いてください。",
        NotSupportedException
            => "このファイル形式には対応していません。",
        IOException
            => "入出力エラーが発生しました。ネットワークドライブや保存先の状態を確認してください。",
        _
            => "予期しないエラーが発生しました。"
    };

    /// <summary>
    /// v2.14.7 SH-31: WorkspaceKind 判定失敗時のユーザー向けメッセージを返す。
    /// 「壊れています」と断定せず、理由に応じて文言を出し分ける。
    /// SchemaVersionTooNew は FM-4（<see cref="SchemaVersionTooNewException"/>）と同じ文言方針。
    /// </summary>
    public static string ForKindDetectionFailure(WorkspaceKindDetectionFailure failure) =>
        ForKindDetectionFailure(failure, path: null);

    /// <summary>
    /// v2.16.8 L20 (review1-fable5.md R-5): path が分かる場合、InvalidFormat（`.nestsuite`
    /// の形式不正・JSON 読込失敗相当）にのみ `.bak` 復元案内を付ける。
    /// </summary>
    public static string ForKindDetectionFailure(WorkspaceKindDetectionFailure failure, string? path) => failure switch
    {
        // v2.16.7 TD-65 (review1-fable5.md R-3): 「削除された」と断定せず、
        // 外部/ネットワークドライブ未接続や移動済みなど、次に確認すべきことを示す。
        WorkspaceKindDetectionFailure.FileNotFound
            => "ファイルが見つかりません。外部ドライブ、ネットワークドライブ、または移動済みのファイルを確認してください。",
        WorkspaceKindDetectionFailure.AccessDenied
            => "ファイルにアクセスできません。権限または他のアプリによる使用状況を確認してください。",
        WorkspaceKindDetectionFailure.InvalidFormat
            => "この .nestsuite ファイルの形式を確認できませんでした。\nファイルが壊れているとは限りません。より新しいバージョンの NestSuite で作成された可能性があります。" + BackupRestoreHint(path),
        WorkspaceKindDetectionFailure.UnknownWorkspaceKind
            => "この .nestsuite ファイルの Workspace 種別を判定できませんでした。\nより新しいバージョンの NestSuite で作成された可能性があります。",
        WorkspaceKindDetectionFailure.SchemaVersionTooNew
            => "このファイルは、より新しいバージョンの NestSuite で作成された可能性があります。\n現在のバージョンでは安全に開けません。新しいバージョンの NestSuite で開いてください。",
        WorkspaceKindDetectionFailure.UnsupportedExtension
            => "このファイル形式は NestSuite では開けません。\n対応形式: .nestsuite / .notenest / .chatnest / .ideanest",
        WorkspaceKindDetectionFailure.IoError
            => "入出力エラーが発生しました。ネットワークドライブや保存先の状態を確認してください。",
        _
            => "ファイルを開けませんでした。",
    };

    /// <summary>
    /// v2.16.8 L20 (review1-fable5.md R-5): `.bak` からの手動復元案内を短く返す。
    /// path と同じ場所に実在する `.bak` を確認できた場合はファイル名を含めて案内し、
    /// 確認できない場合（path 不明・存在しない）は汎用文言にする。手順の詳細はヘルプの
    /// 「バックアップ復元ガイド」（<see cref="BackupRestoreGuideProvider"/>）に譲る。
    /// 自動復元・自動コピー・自動リネームはここでは一切行わない（文言の組み立てのみ）。
    /// </summary>
    private static string BackupRestoreHint(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path + ".bak"))
        {
            return "\n\n同じ場所にバックアップファイル「" + Path.GetFileName(path) +
                   ".bak」が見つかりました。最後に手動保存した時点の内容を復元できる可能性があります。" +
                   "復元方法はヘルプ > バックアップ復元ガイドをご覧ください。";
        }

        return "\n\n同じ場所に「ファイル名.bak」がある場合、最後に手動保存した時点の内容を復元できる可能性があります。" +
               "復元方法はヘルプ > バックアップ復元ガイドをご覧ください。";
    }

    /// <summary>ファイル保存失敗時のユーザー向けメッセージを返す。</summary>
    public static string ForSave(Exception ex) => ex switch
    {
        UnauthorizedAccessException or SecurityException
            => "アクセス権限がありません。保存先またはファイルの権限を確認してください。",
        JsonException
            => "データの書き込み中にエラーが発生しました。",
        PathTooLongException
            => "ファイルパスが長すぎます。短いパスへ保存してください。",
        IOException
            => "入出力エラーが発生しました。ネットワークドライブや保存先の状態を確認してください。",
        _
            => "予期しないエラーが発生しました。"
    };
}
