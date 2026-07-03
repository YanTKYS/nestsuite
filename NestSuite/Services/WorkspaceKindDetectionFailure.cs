namespace NestSuite.Services;

/// <summary>
/// v2.14.7 SH-31: ファイルからの WorkspaceKind 判定に失敗した理由。
/// `.nestsuite` は拡張子だけでは種別が確定せずファイル内容の読取りが必要なため、
/// 判定失敗を「不明」で握りつぶさず、利用者向け文言（<see cref="FileErrorMessages.ForKindDetectionFailure"/>）を
/// 出し分けられる最小限の粒度で理由を保持する。
/// </summary>
public enum WorkspaceKindDetectionFailure
{
    /// <summary>失敗なし（判定成功）。</summary>
    None,

    /// <summary>対応していない拡張子。</summary>
    UnsupportedExtension,

    /// <summary>ファイルが存在しない。</summary>
    FileNotFound,

    /// <summary>アクセス権限がない、または他プロセスが使用中。</summary>
    AccessDenied,

    /// <summary>JSON として読めない、または NestSuite Workspace wrapper 形式ではない。</summary>
    InvalidFormat,

    /// <summary>wrapper は読めたが workspaceKind が未知（将来の Workspace 種別の可能性）。</summary>
    UnknownWorkspaceKind,

    /// <summary>payloadSchemaVersion が現行より新しい（FM-4 と同じ「破損ではない」扱い）。</summary>
    SchemaVersionTooNew,

    /// <summary>入出力エラー（ネットワークドライブ未接続等）。</summary>
    IoError,

    /// <summary>上記以外の予期しない失敗。</summary>
    Unknown,
}
