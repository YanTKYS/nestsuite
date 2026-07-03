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
    public static string ForLoad(Exception ex) => ex switch
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
            => "ファイル形式を読み取れません。ファイルが破損している可能性があります。",
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
    public static string ForKindDetectionFailure(WorkspaceKindDetectionFailure failure) => failure switch
    {
        WorkspaceKindDetectionFailure.FileNotFound
            => "ファイルが見つかりません。移動または削除された可能性があります。",
        WorkspaceKindDetectionFailure.AccessDenied
            => "ファイルにアクセスできません。権限または他のアプリによる使用状況を確認してください。",
        WorkspaceKindDetectionFailure.InvalidFormat
            => "この .nestsuite ファイルの形式を確認できませんでした。\nファイルが壊れているとは限りません。より新しいバージョンの NestSuite で作成された可能性があります。",
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
