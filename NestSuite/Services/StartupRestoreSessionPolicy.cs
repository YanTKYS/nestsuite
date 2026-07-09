namespace NestSuite.Services;

/// <summary>
/// v2.16.28 TD-75b: 起動時の session 保存要否判定を表す、UI 非依存の純粋ロジック。
/// <see cref="AutoSaveCandidatePolicy"/> と同じ方針で、WPF ウィンドウを生成せずに
/// コンストラクター内の判定条件だけを単体テストできるようにする。
///
/// <para>v2.16.18 TD-70 で導入された判断: session 復元が成功した場合はもちろん、
/// 復元自体は失敗（0 件）でも、起動中に FileNotFound の pending entry を利用者が
/// 解除していれば、その決定を保存する（強制終了時に失われないように）。</para>
/// </summary>
public static class StartupRestoreSessionPolicy
{
    public static bool ShouldSaveSessionAfterStartupRestore(bool restoredSession, bool forgotFileNotFound) =>
        restoredSession || forgotFileNotFound;
}
