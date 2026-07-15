using NestSuite.ChatNest;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// CH-11: ChatNest 会話一覧の日付区切りヘッダー。既存 timestamp（<see cref="Message.CreatedAt"/>）を
/// 利用し、保存しない表示専用の派生状態として実装した。
/// </summary>
public class ChatDateSeparatorServiceTests
{
    private static DateTimeOffset At(int y, int m, int d, int h = 0, int min = 0) => new(y, m, d, h, min, 0, TimeSpan.Zero);

    [Fact]
    public void ComputeShowSeparator_FirstMessage_AlwaysTrue()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2026, 7, 15) });
        Assert.True(flags[0]);
    }

    [Fact]
    public void ComputeShowSeparator_SameDaySecondMessage_False()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2026, 7, 15, 9), At(2026, 7, 15, 10) });
        Assert.True(flags[0]);
        Assert.False(flags[1]);
    }

    [Fact]
    public void ComputeShowSeparator_DateChange_True()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2026, 7, 14, 23), At(2026, 7, 15, 0) });
        Assert.True(flags[0]);
        Assert.True(flags[1]);
    }

    [Fact]
    public void ComputeShowSeparator_ThreeConsecutiveDays_AllTrue()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2026, 7, 13), At(2026, 7, 14), At(2026, 7, 15) });
        Assert.Equal(new[] { true, true, true }, flags);
    }

    [Fact]
    public void ComputeShowSeparator_YearBoundary_HandledCorrectly()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2025, 12, 31), At(2026, 1, 1) });
        Assert.Equal(new[] { true, true }, flags);
    }

    [Fact]
    public void ComputeShowSeparator_MonthBoundary_HandledCorrectly()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2026, 6, 30), At(2026, 7, 1) });
        Assert.Equal(new[] { true, true }, flags);
    }

    [Fact]
    public void ComputeShowSeparator_LeapDay_HandledCorrectly()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2024, 2, 28), At(2024, 2, 29), At(2024, 3, 1) });
        Assert.Equal(new[] { true, true, true }, flags);
    }

    [Fact]
    public void ComputeShowSeparator_ReorderedNonChronological_ReflectsGivenOrder()
    {
        // timestamp 順への自動整列はしない。与えられた順序のまま日付変化を判定する。
        var flags = ChatDateSeparatorService.ComputeShowSeparator(new[] { At(2026, 7, 15), At(2026, 7, 14), At(2026, 7, 15) });
        Assert.Equal(new[] { true, true, true }, flags);
    }

    [Fact]
    public void ComputeShowSeparator_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(ChatDateSeparatorService.ComputeShowSeparator(Array.Empty<DateTimeOffset>()));
    }

    [Fact]
    public void ComputeShowSeparator_DefaultTimestamp_TreatedAsOwnDate_NoException()
    {
        var ex = Record.Exception(() =>
            ChatDateSeparatorService.ComputeShowSeparator(new[] { default(DateTimeOffset), At(2026, 7, 15) }));
        Assert.Null(ex);
    }
}

/// <summary>CH-11: MessageViewModel.ShowDateSeparator / DateSeparatorText の表示形式確認。</summary>
public class MessageViewModelDateSeparatorTests
{
    [Fact]
    public void DateSeparatorText_UsesAbsoluteJapaneseFormat_NotRelative()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { new Message { Speaker = Speaker.自分, Text = "A", CreatedAt = new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero) } });

        var text = vm.Messages[0].DateSeparatorText;

        Assert.Equal("2026年7月15日", text);
        Assert.DoesNotContain("今日", text);
        Assert.DoesNotContain("昨日", text);
    }

    [Fact]
    public void ShowDateSeparator_DefaultsToFalse_BeforeAnyRefresh()
    {
        var vm = new MessageViewModel(
            new Message { Speaker = Speaker.自分, Text = "A" },
            _ => { }, _ => { }, _ => { }, _ => { });

        Assert.False(vm.ShowDateSeparator);
    }
}

/// <summary>CH-11: ChatNestWorkspaceViewModel の日付区切り再計算タイミングの統合確認。</summary>
public class ChatNestDateSeparatorIntegrationTests
{
    private static Message MakeMessage(string text, DateTimeOffset createdAt, Speaker speaker = Speaker.自分) =>
        new() { Speaker = speaker, Text = text, CreatedAt = createdAt };

    private static readonly DateTimeOffset Day1 = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 7, 15, 9, 0, 0, TimeSpan.Zero);

    // ── 基本表示 ─────────────────────────────────────────────────────────

    [Fact]
    public void LoadMessages_FirstMessage_ShowsDateSeparator()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day1) });

        Assert.True(vm.Messages[0].ShowDateSeparator);
    }

    [Fact]
    public void LoadMessages_SecondMessageSameDay_DoesNotShowSeparator()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day1), MakeMessage("B", Day1.AddHours(1)) });

        Assert.True(vm.Messages[0].ShowDateSeparator);
        Assert.False(vm.Messages[1].ShowDateSeparator);
    }

    [Fact]
    public void LoadMessages_DateChanges_ShowsSeparatorAtChangePosition()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day1), MakeMessage("B", Day1.AddHours(2)), MakeMessage("C", Day2) });

        Assert.True(vm.Messages[0].ShowDateSeparator);
        Assert.False(vm.Messages[1].ShowDateSeparator);
        Assert.True(vm.Messages[2].ShowDateSeparator);
    }

    [Fact]
    public void LoadMessages_DoesNotPersistSeparatorState_MessageModelsUnaffected()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day1), MakeMessage("B", Day2) });

        var models = vm.MessageModels.ToList();
        // Message モデル自体には日付区切り情報が一切存在しない（型に追加していない）。
        Assert.All(models, m => Assert.NotEqual(default, m.Id));
    }

    // ── 追加・削除 ───────────────────────────────────────────────────────

    [Fact]
    public void Post_SameDayMessage_DoesNotAddSeparator()
    {
        // Post は常に DateTimeOffset.Now を使うため、既存メッセージも「今日」の時刻にして再現する。
        var vm = new ChatNestWorkspaceViewModel();
        var today = DateTimeOffset.Now;
        vm.LoadMessages(new[] { MakeMessage("A", today.AddMinutes(-5)) });

        vm.InputText = "B";
        vm.PostCommand.Execute(null);

        Assert.True(vm.Messages[0].ShowDateSeparator);
        Assert.False(vm.Messages[1].ShowDateSeparator);
    }

    [Fact]
    public void Post_NewDayMessage_AddsSeparator()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day1) });

        vm.InputText = "B";
        vm.PostCommand.Execute(null);

        // Post した瞬間の CreatedAt は DateTimeOffset.Now（Day1 とは別日のはず）。
        Assert.True(vm.Messages[1].ShowDateSeparator);
    }

    [Fact]
    public void DeleteFirstOfDay_SeparatorMovesToNextSameDayMessage()
    {
        var vm = new ChatNestWorkspaceViewModel();
        var a = MakeMessage("A", Day1);
        var b = MakeMessage("B", Day1.AddHours(1));
        vm.LoadMessages(new[] { a, b });
        Assert.True(vm.Messages[0].ShowDateSeparator);

        var target = vm.Messages[0];
        target.RequestDeleteCommand.Execute(null);
        vm.ConfirmDeleteCommand.Execute(null);

        Assert.Single(vm.Messages);
        Assert.True(vm.Messages[0].ShowDateSeparator);
        Assert.Equal("B", vm.Messages[0].Text);
    }

    [Fact]
    public void DeleteLastMessageOfDay_RemovesSeparatorEntirely()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day1), MakeMessage("B", Day2) });
        var lastOfDay2 = vm.Messages[1];

        lastOfDay2.RequestDeleteCommand.Execute(null);
        vm.ConfirmDeleteCommand.Execute(null);

        Assert.Single(vm.Messages);
        Assert.True(vm.Messages[0].ShowDateSeparator); // A は先頭のため引き続き表示
    }

    // ── 並び替え ─────────────────────────────────────────────────────────

    [Fact]
    public void MoveMessage_RecalculatesSeparatorsForNewDisplayOrder()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day1), MakeMessage("B", Day2), MakeMessage("C", Day1) });
        // 初期: A(先頭,区切り) B(区切り) C(区切り)
        Assert.True(vm.Messages[2].ShowDateSeparator);

        vm.MoveMessage(2, 1); // 順序: A, C, B
        Assert.Equal("C", vm.Messages[1].Text);

        Assert.True(vm.Messages[0].ShowDateSeparator);  // A: 先頭
        Assert.False(vm.Messages[1].ShowDateSeparator); // C: A と同日(Day1)
        Assert.True(vm.Messages[2].ShowDateSeparator);  // B: Day2 で日付変化
    }

    [Fact]
    public void MoveMessage_DoesNotSortByTimestamp_OrderStaysAsMoved()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("A", Day2), MakeMessage("B", Day1) });

        vm.MoveMessage(1, 0);

        Assert.Equal(new[] { "B", "A" }, vm.MessageModels.Select(m => m.Text));
    }

    // ── 検索（フィルタしないため常に全件が対象） ──────────────────────────

    [Fact]
    public void Search_DoesNotHideMessages_SeparatorsUnaffected()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { MakeMessage("Meeting", Day1), MakeMessage("Other", Day2) });
        var before = vm.Messages.Select(m => m.ShowDateSeparator).ToList();

        vm.SearchText = "meeting";

        Assert.Equal(2, vm.Messages.Count); // 検索は非表示にしない
        Assert.Equal(before, vm.Messages.Select(m => m.ShowDateSeparator).ToList());

        vm.SearchText = "";
        Assert.Equal(before, vm.Messages.Select(m => m.ShowDateSeparator).ToList());
    }

    // ── 保存回帰 ─────────────────────────────────────────────────────────

    [Fact]
    public void MessageType_HasNoDateSeparatorField()
    {
        var properties = typeof(Message).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("ShowDateSeparator", properties);
        Assert.DoesNotContain("DateSeparatorText", properties);
    }
}
