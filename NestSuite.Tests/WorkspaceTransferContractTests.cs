using System.Reflection;
using NestSuite.Dialogs;
using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// Workspace 間手動転送（TN-3 / LK-4 / LK-3 / LK-2）の横断契約テスト。
///
/// <para>TD-94 (v2.24.1) で、旧 <c>TD93WorkspaceTransferRegressionTests</c> から
/// <b>実際に production code を実行する / 型情報を確認するテストだけ</b>を残して改名した。
/// 削除したのは次の 3 種類（方針: <c>docs/development/test-suite-policy.md</c>）:</para>
/// <list type="bullet">
///   <item>production の <c>.cs</c> をテキストとして読み <c>Assert.Contains("ActivateTab")</c> 等で
///         実装の書き方を固定していたもの（コメント追加・メソッド改名だけで誤検出し、
///         別経路で同じ副作用を持ち込まれても検出できない）</item>
///   <item>backlog / release notes / 設計文書の日本語本文を assert していたもの</item>
///   <item>他のテストファイルが存在することを確認していた test-of-test</item>
/// </list>
///
/// <para>Shell（<see cref="NestSuiteShellWindow"/>）は WPF <c>Window</c> でインスタンス化できないため、
/// Shell 側はリフレクションによる API 契約確認に留め、実際の振る舞い確認は
/// 転送元 ViewModel・転送先 ViewModel の側で行う。</para>
/// </summary>
public class WorkspaceTransferContractTests
{
    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // ── 共通転送 DTO / 結果契約 ───────────────────────────────────────────

    [Fact]
    public void WorkspaceTransferContent_HasOnlyTitleAndBody()
    {
        // 共通層へ Workspace 固有情報（発言者・スロット番号・日時・タグ等）が
        // 流入していないことを型構造で固定する。
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferContent", BindingFlags.NonPublic);
        Assert.NotNull(type);

        var properties = type!.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Body", "Title" }, properties);
    }

    [Fact]
    public void WorkspaceTransferResult_HasExactlyFiveValues()
    {
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferResult", BindingFlags.NonPublic);
        Assert.NotNull(type);
        Assert.True(type!.IsEnum);
        Assert.Equal(
            new[] { "Success", "NoTarget", "InvalidContent", "TargetRejected", "Failed" },
            Enum.GetNames(type));
    }

    [Fact]
    public void WorkspaceTransferTarget_IdentifiesByTabId()
    {
        // 同名タブへの誤転送を防ぐため、転送先は表示名ではなく TabId で識別する。
        var type = typeof(NestSuiteShellWindow).GetNestedType("WorkspaceTransferTarget", BindingFlags.NonPublic);
        Assert.NotNull(type);

        var properties = type!.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name).ToArray();
        Assert.Contains("TabId", properties);
        Assert.Contains("Kind", properties);
        Assert.Contains("DisplayName", properties);
    }

    // ── Shell 側 API 契約（4 本の転送が別々の入口として存在すること）────────

    [Fact]
    public void Shell_HasSeparateWiringMethod_ForEachTransfer()
    {
        // TN-3（新規タブ生成を伴う昇格）と LK-4/LK-3/LK-2（既存タブへの追加）は
        // 責務が異なるため、単一の汎用転送メソッドへ統合されていないことを確認する。
        var shell = typeof(NestSuiteShellWindow);
        Assert.NotNull(shell.GetMethod("WireTempNestPromotion", InstanceNonPublic));
        Assert.NotNull(shell.GetMethod("WireChatNestToIdeaNestTransfer", InstanceNonPublic));
        Assert.NotNull(shell.GetMethod("WireTempNestToIdeaNestTransfer", InstanceNonPublic));
        Assert.NotNull(shell.GetMethod("WireTempNestToNoteNestTransfer", InstanceNonPublic));
    }

    [Fact]
    public void Shell_CommonTransferHelpers_Exist_WithExpectedShape()
    {
        var shell = typeof(NestSuiteShellWindow);

        Assert.NotNull(shell.GetMethod("EnumerateTransferTargets", InstanceNonPublic));

        var transfer = shell.GetMethod("TransferToWorkspaceTab", InstanceNonPublic);
        Assert.NotNull(transfer);
        Assert.True(transfer!.IsGenericMethodDefinition);
    }

    [Fact]
    public void Shell_Tn3PromotionMethod_StillExists()
    {
        // TN-3 が共通ヘルパーへ吸収されず、独立した導線として残っていること。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("PromoteTempNestSlotToNoteNest", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool?), method!.ReturnType);
    }

    // ── 転送元 TempNest：3 導線が同じ delegate 形で共存する ────────────────

    [Fact]
    public void TempNestSlotViewModel_HasThreeRequestDelegates_WithSameShape()
    {
        var type = typeof(TempNestSlotViewModel);
        var promote = type.GetProperty("PromoteRequested");
        var toIdea = type.GetProperty("TransferToIdeaNestRequested");
        var toNote = type.GetProperty("TransferToNoteNestRequested");

        Assert.NotNull(promote);
        Assert.NotNull(toIdea);
        Assert.NotNull(toNote);
        Assert.Equal(typeof(Func<TempNestSlotViewModel, bool?>), promote!.PropertyType);
        Assert.Equal(promote.PropertyType, toIdea!.PropertyType);
        Assert.Equal(promote.PropertyType, toNote!.PropertyType);
    }

    [Fact]
    public void TempNestSlotViewModel_Dispose_ClearsAllThreeRequestDelegates()
    {
        var slot = new TempNestSlotViewModel
        {
            PromoteRequested = _ => null,
            TransferToIdeaNestRequested = _ => null,
            TransferToNoteNestRequested = _ => null,
        };

        slot.Dispose();

        Assert.Null(slot.PromoteRequested);
        Assert.Null(slot.TransferToIdeaNestRequested);
        Assert.Null(slot.TransferToNoteNestRequested);
    }

    // ── 転送先選択ダイアログ：転送先種別ごとの読み上げ名を指定できる ────────

    [Fact]
    public void WorkspaceTransferTargetDialog_AcceptsPerCallerListAutomationName()
    {
        // LK-2 で同じダイアログが NoteNest 転送にも再利用されたため、
        // 一覧の読み上げ名を呼出元が指定できる必要がある（固定だと NoteNest 転送でも
        // 「IdeaNest」と読み上げられる）。
        var ctor = typeof(WorkspaceTransferTargetDialog)
            .GetConstructors(InstanceNonPublic).FirstOrDefault();
        Assert.NotNull(ctor);

        var parameterNames = ctor!.GetParameters().Select(p => p.Name).ToArray();
        Assert.Contains("listAutomationName", parameterNames);
        Assert.Contains("windowTitle", parameterNames);
        Assert.Contains("promptText", parameterNames);
    }
}
