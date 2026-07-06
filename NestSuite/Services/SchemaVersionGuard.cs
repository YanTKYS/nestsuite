using System.IO;

namespace NestSuite.Services;

/// <summary>
/// v2.14.4 FM-4: 現在のアプリより新しい schema のファイルを検出した場合に読み込みを止めるための例外。
/// <see cref="InvalidDataException"/> は sealed のため派生できず、代わりに <see cref="Exception"/> を直接継承する。
/// 呼び出し元の読込処理は broad <c>catch (Exception ex)</c> でこの例外も含めて捕捉するため、
/// 既存の読込 catch 経路には影響しない。
/// <see cref="FileErrorMessages.ForLoad"/> がこの型を専用のユーザー向け文言（「より新しいバージョンの
/// NestSuite で作成された可能性があります」）へ変換する。「壊れています」とは断定しない。
/// </summary>
public sealed class SchemaVersionTooNewException : Exception
{
    public SchemaVersionTooNewException(string message) : base(message)
    {
    }
}

/// <summary>
/// v2.14.4 FM-4 / TD-58: schema version の数値比較と前方互換ガード。
///
/// <para><b>比較方針:</b> 文字列比較ではなく <see cref="Version"/> による数値比較を行う
/// （<c>1.4.2 &lt; 1.4.10</c> を正しく判定できる）。<c>major.minor.patch</c> 形式を基本とし、
/// <c>major.minor</c> も比較可能として扱う。</para>
///
/// <para><b>ガード方針（最小実装）:</b> ファイル側 version が現行 version より新しい場合は
/// <see cref="SchemaVersionTooNewException"/> で読み込みを止め、無警告の上書き保存による
/// 未知フィールド喪失を防ぐ。read-only モードや未知フィールド保持は今回実装しない。
/// 方針は docs/architecture/schema-versioning-policy.md 参照。</para>
/// </summary>
public static class SchemaVersionGuard
{
    /// <summary>version 文字列を数値比較可能な形へ解釈する。major.minor / major.minor.patch を受け付ける。</summary>
    public static bool TryParse(string? version, out Version parsed)
    {
        parsed = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(version)) return false;
        if (!Version.TryParse(version, out var result) || result == null) return false;
        parsed = result;
        return true;
    }

    /// <summary>
    /// <paramref name="fileVersion"/> が <paramref name="currentVersion"/> より新しいかを数値比較で判定する。
    /// どちらかが解釈できない場合は <see cref="InvalidDataException"/> で失敗する（不正な version は読み込み失敗）。
    /// </summary>
    public static bool IsNewer(string fileVersion, string currentVersion)
    {
        if (!TryParse(fileVersion, out var file))
            throw new InvalidDataException($"schema version を解釈できません: {fileVersion}");
        if (!TryParse(currentVersion, out var current))
            throw new InvalidDataException($"schema version を解釈できません: {currentVersion}");
        return file > current;
    }

    /// <summary>
    /// ファイル側 schema version が現行より新しい場合に読み込みを止める。
    /// <paramref name="fileVersion"/> が null / 空の場合は許容する
    /// （version 項目を持たない旧ファイル・payloadSchemaVersion 欠落の wrapper との互換のため）。
    /// </summary>
    public static void EnsureNotNewer(string? fileVersion, string currentVersion, string label)
    {
        if (string.IsNullOrWhiteSpace(fileVersion)) return;
        if (IsNewer(fileVersion, currentVersion))
            throw new SchemaVersionTooNewException(
                $"{label} の schema version {fileVersion} は、このバージョンの NestSuite が対応する {currentVersion} より新しいため読み込めません。");
    }

    /// <summary>
    /// v2.14.4 FM-4: `.nestsuite` wrapper の payloadSchemaVersion と payload 内 schema version の整合を確認する。
    ///
    /// <para><b>不整合の定義:</b> payload 内 version が payloadSchemaVersion より<b>新しい</b>場合のみ失敗させる。
    /// 逆方向（payloadSchemaVersion の方が新しい）は正常として許容する。これは v2.14.1〜v2.14.3 のアプリが
    /// 旧 schema のまま読み込んだ payload（例: version 1.4.1 の NoteNest）を wrapper で包む際、
    /// payloadSchemaVersion には常に現行定数（例: 1.4.2）を書くため、既存の正当なファイルに
    /// 「wrapper の方が新しい」組み合わせが実在するからである（厳密一致にすると読込互換が壊れる）。</para>
    /// </summary>
    public static void EnsureEnvelopeConsistent(string? payloadSchemaVersion, string? payloadVersion, string label)
    {
        if (string.IsNullOrWhiteSpace(payloadSchemaVersion) || string.IsNullOrWhiteSpace(payloadVersion)) return;
        if (!TryParse(payloadSchemaVersion, out var declared) || !TryParse(payloadVersion, out var actual))
            throw new InvalidDataException($"{label} の schema version を解釈できません。");
        if (actual > declared)
            throw new InvalidDataException(
                $"この .nestsuite の payloadSchemaVersion（{payloadSchemaVersion}）と {label} 本体の schema version（{payloadVersion}）が矛盾しています。");
    }
}
