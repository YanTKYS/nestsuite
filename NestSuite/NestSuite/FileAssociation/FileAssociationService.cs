using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace NestSuite.FileAssociation;

/// <summary>
/// HKCU\Software\Classes への NestSuite ファイル関連付け登録・解除・状態確認。
/// 管理者権限不要、ユーザー単位のみ。自動実行はしない（ダイアログから明示的に呼び出す）。
/// </summary>
public sealed class FileAssociationService
{
    private const string ProgIdNotenest = "NoteNest.notenest";
    private const string ProgIdChatnest = "NoteNest.chatnest";
    private const string ProgIdIdeanest = "NoteNest.ideanest";
    // v2.14.6 FM-3: .nestsuite は拡張子だけでは WorkspaceKind が確定しないため「NestSuite Workspace」として登録する。
    // ProgId は既存の互換性識別子（NoteNest.* 族、compatibility-identifiers-audit.md 分類 A）の命名規則に合わせる。
    private const string ProgIdNestsuite = "NoteNest.nestsuite";

    // 関連付け対象の単一情報源。tools/register-nestsuite-file-association.ps1 /
    // tools/unregister-nestsuite-file-association.ps1 と 3 箇所同期必須（audit doc §1-4）。
    private static readonly (string Ext, string ProgId, string Description)[] Targets =
    [
        // v2.14.8: 拡張子は各 FileService / Envelope の FileExtension 定数を単一情報源として参照する
        (Services.NestSuiteWorkspaceEnvelope.FileExtension, ProgIdNestsuite, "NestSuite Workspace"),
        (Services.ProjectFileService.FileExtension, ProgIdNotenest, "NoteNest Document"),
        (ChatNest.ChatNestFileService.FileExtension, ProgIdChatnest, "ChatNest Document"),
        (IdeaNest.Services.IdeaNestFileService.FileExtension, ProgIdIdeanest, "IdeaNest Document"),
    ];

    /// <summary>
    /// v2.14.6 FM-3: 関連付け対象（拡張子 / ProgId / 表示名）の読み取り専用ビュー。
    /// ダイアログ表示とテスト（レジストリ非接触の定義確認）で使用する。
    /// </summary>
    public static IReadOnlyList<(string Ext, string ProgId, string Description)> AssociationTargets => Targets;

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

    private const int ShcneAssocChanged = 0x08000000;

    /// <summary>指定拡張子の HKCU\Software\Classes 上の状態を返す。</summary>
    public FileAssociationStatus GetStatus(string ext, string currentExePath)
    {
        var progId = GetProgId(ext);
        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ext}");
        if (key == null) return FileAssociationStatus.NotRegistered;
        var value = key.GetValue(null) as string;
        if (string.IsNullOrEmpty(value)) return FileAssociationStatus.NotRegistered;
        if (!string.Equals(value, progId, StringComparison.OrdinalIgnoreCase))
            return FileAssociationStatus.OtherApp;

        // ProgId は一致する。コマンドが現在の EXE を指しているか確認する。
        using var cmdKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command");
        var cmd = cmdKey?.GetValue(null) as string;
        if (string.IsNullOrEmpty(cmd)) return FileAssociationStatus.StaleRegistration;

        // コマンド形式: "C:\path\to\NestSuite.exe" "%1" — 先頭のパスを取り出す
        var trimmed = cmd.Trim();
        string registeredExe;
        if (trimmed.StartsWith("\""))
        {
            var end = trimmed.IndexOf('"', 1);
            registeredExe = end > 0 ? trimmed.Substring(1, end - 1) : trimmed;
        }
        else
        {
            var sp = trimmed.IndexOf(' ');
            registeredExe = sp > 0 ? trimmed.Substring(0, sp) : trimmed;
        }

        return string.Equals(registeredExe, currentExePath, StringComparison.OrdinalIgnoreCase)
            ? FileAssociationStatus.Registered
            : FileAssociationStatus.StaleRegistration;
    }

    /// <summary>
    /// 4 拡張子（.nestsuite / .notenest / .chatnest / .ideanest）を指定の exePath に関連付ける。
    /// 既存エントリは上書きする。
    /// </summary>
    public void Register(string exePath)
    {
        foreach (var (ext, progId, desc) in Targets)
        {
            using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
                progIdKey.SetValue(null, desc);

            using (var cmdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\shell\open\command"))
                cmdKey.SetValue(null, $"\"{exePath}\" \"%1\"");

            using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}"))
                extKey.SetValue(null, progId);
        }

        SHChangeNotify(ShcneAssocChanged, 0, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// この機能で作成した ProgId キーを削除し、拡張子キーがこの ProgId を指していれば削除する。
    /// 他アプリの関連付けは変更しない。
    /// </summary>
    public UnregisterResult Unregister()
    {
        bool anyRemoved = false;

        foreach (var (ext, progId, _) in Targets)
        {
            // 拡張子キーは ProgId が一致する場合のみ削除する
            using (var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ext}"))
            {
                var current = extKey?.GetValue(null) as string;
                if (string.Equals(current, progId, StringComparison.OrdinalIgnoreCase))
                {
                    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", throwOnMissingSubKey: false);
                    anyRemoved = true;
                }
            }

            // ProgId キーは存在すれば削除する
            using (var progIdKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}"))
            {
                if (progIdKey != null)
                {
                    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{progId}", throwOnMissingSubKey: false);
                    anyRemoved = true;
                }
            }
        }

        if (anyRemoved)
            SHChangeNotify(ShcneAssocChanged, 0, IntPtr.Zero, IntPtr.Zero);

        return anyRemoved ? UnregisterResult.Removed : UnregisterResult.NotFound;
    }

    // v2.14.6 FM-3: 拡張子一覧が Targets と switch の 2 箇所に重複していた同期漏れの温床を解消し、
    // Targets を単一情報源として参照する。
    private static string GetProgId(string ext)
    {
        foreach (var (targetExt, progId, _) in Targets)
            if (string.Equals(targetExt, ext, StringComparison.OrdinalIgnoreCase))
                return progId;
        throw new ArgumentException($"Unknown extension: {ext}", nameof(ext));
    }
}

public enum FileAssociationStatus
{
    Registered,          // 登録済み (この機能による ProgId + 現在の EXE が設定されている)
    NotRegistered,       // 未登録 (HKCU にエントリなし)
    OtherApp,            // 他のアプリに関連付け済み (HKCU に別の ProgId が設定されている)
    StaleRegistration,   // 再登録が必要 (ProgId は一致するが EXE パスが現在と異なる)
}

public enum UnregisterResult
{
    Removed,  // 削除した
    NotFound, // 登録が見つからなかった
}
