# テストクラス分類・整理方針の一次分析

## 1. 概要

- 分析対象: `NestSuite.Tests/**/*.cs` のテストクラス、`[Fact]`、`[Theory]`。
- 対象バージョン: NestSuite v2.10.14。
- 分析日時: 2026-06-27。
- 分類方針: クラス単位テスト、機能単位テスト、シナリオ / 回帰テスト、ドキュメント / ルール固定テスト、不要テスト候補の 5 分類。
- この文書は一次分析であり、削除・リネーム・統合を直接行うものではない。

## 2. 全体サマリー

- テストクラス数: 96
- テストメソッド数: 1260
- 分類別件数:
  - クラス単位テスト: 658
  - 機能単位テスト: 85
  - シナリオ / 回帰テスト: 275
  - ドキュメント / ルール固定テスト: 221
  - 不要テスト候補: 21
- 課題番号 / versionベースのテストクラス数: 10
- 不要テスト候補数: 21
- 今後優先して整理すべき領域: 課題番号 / version ベースのクラス名、schema / release notes / backlog 文字列確認の重複、巨大な Shell 系テストクラス、Regression / Smoke の責務境界。

## 3. 分類表

| テストクラス | テストメソッド | 分類 | 対象クラス / 対象機能 | 関連ID | 備考 |
|-------------|----------------|------|------------------------|--------|------|
| `AppExitAndTabCloseRegressionTests` | `AppExit_NoUnsavedTabs_ContinuesWithoutAsking` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_UnsavedNoteNest_Cancel_StopsExit` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_UnsavedNoteNest_SaveSuccess_ContinuesExit` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_UnsavedNoteNest_SaveFail_StopsExit` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_UnsavedNoteNest_Discard_ContinuesExit` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_MultipleUnsavedTabs_CancelOnFirst_SecondNotAsked` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_MultipleUnsavedTabs_AllSaved_ContinuesExit` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_MultipleUnsavedTabs_SaveFailOnFirst_StopsExit` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `AppExit_SavedAndUnsavedTabs_OnlySavedIsSkipped` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `SaveAsCancel_PreventsClose_SaveReturningFalseIsCancel` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `SaveAsCancel_CanCloseSingle_ReturnsFalse` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `TempNestTab_CanCloseFalse_ExcludedFromExitConfirmation` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `DetachedWindowCloseButton_IsReattachOperation_NotSaveConfirmation` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `NoteNestConfirmation_HasThreeChoices_NotTwoChoices` | シナリオ / 回帰テスト | AppExitAndTabCloseRegression |  |  |
| `AppExitAndTabCloseRegressionTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `ApplicationVersionTests` | `ApplicationVersion_UsesAssemblyInformationalVersion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ApplicationVersionTests` | `WindowTitle_UsesApplicationVersion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ApplicationVersionTests` | `ApplicationAndSchemaVersionsAreManagedBySeparateSources` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ApplicationVersionTests` | `ApplicationVersion_IsNotTested_InOtherTestClasses` | ドキュメント / ルール固定テスト | ApplicationVersion 集約ルール |  | TD-27 の集約ルール固定。 |
| `ArchitectureBoundaryTests` | `WorkspaceViewModels_DoNotExposeAppShellTypesInSignatures` | クラス単位テスト | ArchitectureBoundary |  |  |
| `ArchitectureBoundaryTests` | `WorkspaceCoordinatorsAndServices_DoNotExposeAppShellTypesInSignatures` | クラス単位テスト | ArchitectureBoundary |  |  |
| `ArchitectureBoundaryTests` | `WorkspaceModels_DoNotExposeAppShellTypesInSignatures` | クラス単位テスト | ArchitectureBoundary |  |  |
| `ArchitectureBoundaryTests` | `WorkspaceTypes_DoNotInheritFromWindow` | クラス単位テスト | ArchitectureBoundary |  |  |
| `ArchitectureBoundaryTests` | `WorkspaceViewModels_CanBeInstantiatedWithoutWindowInfrastructure` | クラス単位テスト | ArchitectureBoundary |  |  |
| `ArchitectureBoundaryTests` | `WorkspaceSourceFiles_DoNotContainAppShellCallSites` | クラス単位テスト | ArchitectureBoundary |  |  |
| `AtomicFileWriterTests` | `WriteAllText_NewFile_CreatesFile` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_NewFile_NoTmpRemaining` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_NestedDirectory_CreatesDirectory` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_ExistingFile_Overwrites` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_ExistingFile_NoTmpRemaining` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_WithBackupPath_CreatesBackup` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_WithoutBackupPath_NoBackupFile` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_NewFile_WithBackupPath_NoBackupCreated` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_Utf8WithBom_HasBomBytes` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `WriteAllText_Utf8NoBom_HasNoBomBytes` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `IdeaNestWorkspaceService_Save_CreatesPreWriteBackup` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `IdeaNestWorkspaceService_Save_NoTmpRemaining` | クラス単位テスト | AtomicFileWriter |  |  |
| `AtomicFileWriterTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `AutomationIdTests` | `AllAutomationIds_MatchDotSeparatedFormat` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `AllAutomationIds_ContainOnlyAsciiCharacters` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `ShellAutomationIds_AreUnique` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `NoteNestAutomationIds_AreUnique` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `EditorAutomationIds_AreUnique` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `IdeaNestAutomationIds_AreUnique` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `ChatNestAutomationIds_AreUnique` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `TempNestAutomationIds_AreUnique` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `DialogAutomationIds_AreUnique` | クラス単位テスト | AutomationId |  |  |
| `AutomationIdTests` | `AllAutomationIds_PrefixMatchesContainingClass` | クラス単位テスト | AutomationId |  |  |
| `BacklinkServiceTests` | `NoBacklinks_ReturnsEmpty` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `OtherNoteHasLink_IsDetected` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `MultipleNotesReferencing_AllCounted` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `ExcludeNote_OwnContentIgnored` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `CaseInsensitiveMatch` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `RegularUrls_NotDetected` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `WhitespaceOnlyLink_NotDetected` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `ExistingDuplicateCheck_NotAffected` | クラス単位テスト | BacklinkService |  |  |
| `BacklinkServiceTests` | `RenameWithNoBacklinks_Succeeds` | クラス単位テスト | BacklinkService |  |  |
| `BrokenLinkCheckerServiceTests` | `ExistingLink_IsNotBroken` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `NonExistentLink_IsDetected` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `MultipleNotes_AllScanned` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `MultipleLinksInOneNote_BothChecked` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `SameBrokenLinkOnTwoLines_ReportedTwice` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `WhitespaceOnlyLinkName_Excluded` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `RegularUrls_NotDetectedAsLinks` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `DuplicateNoteTitles_LinkStillResolvesAsValid` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `LineNumber_IsOneBasedAndCorrect` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinkCheckerServiceTests` | `TitleComparison_IsCaseInsensitive` | クラス単位テスト | BrokenLinkCheckerService |  |  |
| `BrokenLinksDialogLogicTests` | `GetHeaderText_ZeroResults_ReturnsNoBrokenLinksMessage` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `BrokenLinksDialogLogicTests` | `GetHeaderText_OneResult_ReturnsCountMessage` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `BrokenLinksDialogLogicTests` | `GetHeaderText_MultipleResults_ReturnsCountMessage` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `BrokenLinksDialogLogicTests` | `BrokenLinkResult_SourceNoteTitle_IsPreserved` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `BrokenLinksDialogLogicTests` | `BrokenLinkResult_LinkName_IsPreserved` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `BrokenLinksDialogLogicTests` | `BrokenLinkResult_SourceNote_IsPreserved` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `BrokenLinksDialogLogicTests` | `BrokenLinksCheck_EmptyNoteList_ReturnsEmpty` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `BrokenLinksDialogLogicTests` | `BrokenLinksCheck_SameBrokenLinkTwiceInOneNote_ReportedTwice` | クラス単位テスト | BrokenLinksDialogLogic |  |  |
| `ChatNestCH13DragReorderTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | CH-13 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_ChangesOrder` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_FirstToLast` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_LastToFirst` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_SameIndex_DoesNotChangeOrder` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_InvalidOldIndex_DoesNotThrow` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_InvalidNewIndex_DoesNotThrow` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_SingleMessage_DoesNotThrow` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_EmptyCollection_DoesNotThrow` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_SetsDirty` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_SameIndex_DoesNotSetDirty` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_FiresWorkspaceModified` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_PreservesId` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_PreservesCreatedAt` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MoveMessage_PreservesSpeakerAndText` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `MessageModels_ReflectsReorderedSequence` | シナリオ / 回帰テスト | ChatNestCH13DragReorder | CH-13 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH13DragReorderTests` | `Backlog_CH13_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-13 |  |
| `ChatNestCH13DragReorderTests` | `ReleaseNotes_Contains_V2109` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-13, V2109 |  |
| `ChatNestCH8CH14Tests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | CH-8, CH-14 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `ChatNestCH8CH14Tests` | `ShowTimestamps_DefaultIsTrue` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `ShowTimestamps_CanBeSetToFalse` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `ShowTimestamps_CanBeToggledBackToTrue` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `ShowTimestamps_ToggleDoesNotChangeMessageModel` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `ShowTimestamps_ChatNestSaveModelUnchanged` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `BuildPlainTextConversation_EmptyConversation_ReturnsEmpty` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `BuildPlainTextConversation_SingleMessage_Format` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `BuildPlainTextConversation_MultipleMessages_SeparatedByBlankLine` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `BuildPlainTextConversation_MessagesInOrder` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `BuildPlainTextConversation_EmptyTextMessage_DoesNotCrash` | シナリオ / 回帰テスト | ChatNestCH8CH14 | CH-8, CH-14 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH8CH14Tests` | `BuildPlainTextConversationWithTimestamp_SingleMessage_ContainsTimestamp` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-8, CH-14 |  |
| `ChatNestCH8CH14Tests` | `Backlog_CH8_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-8, CH-14 |  |
| `ChatNestCH8CH14Tests` | `Backlog_CH14_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-8, CH-14 |  |
| `ChatNestCH8CH14Tests` | `ReleaseNotes_Contains_V2106` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-8, CH-14, V2106 |  |
| `ChatNestCH9ExportTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | CH-9 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `ChatNestCH9ExportTests` | `BuildMarkdownConversation_EmptyConversation_ReturnsEmpty` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `BuildMarkdownConversation_StartsWithH1` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `BuildMarkdownConversation_ContainsFormattedSpeaker` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-9 |  |
| `ChatNestCH9ExportTests` | `BuildMarkdownConversation_SingleMessage_Format` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `BuildMarkdownConversation_MultipleMessages_SeparatedByBlankLine` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `BuildMarkdownConversation_MessagesInOrder` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `BuildMarkdownConversation_EmptyTextMessage_DoesNotCrash` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `ExportConversationCommand_CanExecuteIsFalseWhenEmpty` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `ExportConversationCommand_CanExecuteIsTrueWhenHasMessages` | シナリオ / 回帰テスト | ChatNestCH9Export | CH-9 | 課題番号 / version ベースのテストクラス名。 |
| `ChatNestCH9ExportTests` | `Backlog_CH9_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-9 |  |
| `ChatNestCH9ExportTests` | `ReleaseNotes_Contains_V2107` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-9, V2107 |  |
| `ChatNestFileServiceTests` | `FileExtension_IsExpected` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `FileVersionString_IsExpected` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestFileServiceTests` | `Save_CreatesFile` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Save_DoesNotLeaveTmpFile` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Save_JsonContainsVersionField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestFileServiceTests` | `Save_JsonContainsMessagesField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestFileServiceTests` | `Save_Overwrites_ExistingFile` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_EmptyMessages_ReturnsEmptyList` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_PreservesMessageCount` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_PreservesId` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_PreservesSpeaker` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_PreservesText` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_PreservesCreatedAt` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_MapsYoyaku_ToKetsuron` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_SkipsUnknownSpeaker` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_ThrowsInvalidDataException_WhenJsonIsEmpty` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestFileServiceTests` | `Load_ThrowsException_WhenFileNotFound` | クラス単位テスト | ChatNestFileService |  |  |
| `ChatNestMultiTabSessionTests` | `TwoViewModels_Messages_AreIndependent` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoViewModels_InputText_IsIndependent` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoViewModels_HasUnsavedChanges_IsIndependent` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoViewModels_LoadMessages_DoNotCrossContaminate` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoViewModels_MarkSaved_IsIndependent` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoSessions_HoldDistinctViewModelInstances` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoSessions_ActivatingByTabId_ReturnsCorrectViewModel` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `Session_Remove_RemovesOnlyTargetSession` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `SaveSessionA_DoesNotChangeSessionB_FilePath` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `SaveSessionA_DoesNotChangeSessionB_IsModified` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `OpenFilePolicy_SameChatNestPath_IsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestMultiTabSessionTests` | `OpenFilePolicy_DifferentChatNestPaths_AreNotDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestMultiTabSessionTests` | `OpenFilePolicy_CaseInsensitive_ChatNestIsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestMultiTabSessionTests` | `OpenFilePolicy_AfterNormalization_RelativeAndAbsolute_AreSameFile` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestMultiTabSessionTests` | `OpenFilePolicy_BothNormalized_DifferentFiles_AreNotSame` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestMultiTabSessionTests` | `ViewModelA_PropertyChanged_DoesNotFireOnViewModelB` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoViewModels_EachFirePropertyChanged_Independently` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `SessionReverseLoopup_ByReferenceEquals_FindsCorrectSession` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `SessionReverseLoopup_UnknownViewModel_ReturnsNull` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `Sessions_FilterByChatNestKind_ExcludesOtherKinds` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `Sessions_FilterByChatNestKind_WhenNoChatNestSessions_ReturnsEmpty` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `TwoViewModels_IsDirty_IsIndependent` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `MarkSaved_ResetsHasUnsavedChanges_OnlyForThatViewModel` | シナリオ / 回帰テスト | ChatNestMultiTabSession |  |  |
| `ChatNestMultiTabSessionTests` | `OpenFilePolicy_NullPathA_IsNotSameAsAnyPath` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestMultiTabSessionTests` | `OpenFilePolicy_BothNull_IsNotSameFile` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestShortcutPolicyTests` | `CtrlLeftRight_AreSpeakerSwitchShortcuts` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestShortcutPolicyTests` | `ShiftLeftRight_AreNotSpeakerSwitchShortcuts` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestShortcutPolicyTests` | `ShiftLeftRight_AreLeftUnhandledForShellTabSwitching` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestShortcutPolicyTests` | `CtrlEnter_IsSendShortcut` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestShortcutPolicyTests` | `ShiftEnter_IsNotSendShortcut` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestUxTests` | `CopyMessageCommand_InvokesCallback_WithMessageViewModel` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `CopyMessageCommand_DoesNotIncludeSpeakerOrTimestamp` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchText_WhenSet_FiltersMessages` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchText_NoMatch_Returns0件` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchText_Empty_ReturnsEmptySummary` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchText_IsCaseInsensitive` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchText_MatchesSpeaker` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchNextCommand_AdvancesToNextResult` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchNextCommand_WrapsAroundToFirst` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchPreviousCommand_WrapsAroundToLast` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `IsSearchCurrent_SetOnFirstMatch_OnSearch` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `IsSearchCurrent_MovesOnNavigate` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `SearchState_ResetOnClear` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `IsSearchBarVisible_DefaultsFalse` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `OpenSearchCommand_SetsIsSearchBarVisibleTrue` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `CloseSearchCommand_ClearsSearchAndHidesBar` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `Search_AfterEdit_MatchingMessageRemoved_SummaryUpdates` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestUxTests` | `Search_AfterEdit_NonMatchingMessageBecomesMatch_SummaryUpdates` | 機能単位テスト | ChatNestUx |  |  |
| `ChatNestWorkspaceViewModelTests` | `Post_WithText_AddsMessageWithSelectedSpeaker` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `Post_WithWhitespaceOnly_DoesNotAddMessage` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `Post_TrimsSurroundingWhitespace` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `CycleSpeaker_Forward_AdvancesThroughAllSpeakersAndWraps` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `CycleSpeaker_Backward_WrapsToLast` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `WorkspaceModified_RaisedOnPost` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `Clear_RemovesMessagesAndResetsState` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `HasUnsavedChanges_FreshViewModel_IsFalse` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `HasUnsavedChanges_AfterPost_IsTrue` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `HasUnsavedChanges_WithUnpostedInputOnly_IsTrue` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `HasUnsavedChanges_WithWhitespaceInputOnly_IsFalse` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `HasUnsavedChanges_RaisesPropertyChanged_WhenInputTextChanges` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `Clear_ResetsHasUnsavedChanges` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `LoadMessages_ReplacesContentsAndMarksClean` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `MarkSaved_WhenInputTextRemains_HasUnsavedChangesIsTrue` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `MarkSaved_WhenInputTextEmpty_HasUnsavedChangesIsFalse` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `LoadMessages_SetsHasUnsavedChangesFalse` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `LoadMessages_ThenPost_HasUnsavedChangesIsTrue` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `BuildNestSuiteText_WithMessages_FormatsCorrectly` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `BuildNestSuiteText_ConsecutiveSameSpeaker_GroupsIntoOneBlock` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `BuildNestSuiteText_EmptyMessages_ReturnsEmptyString` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `BuildMarkdownText_ConsecutiveSameSpeaker_GroupsIntoOneBlock` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `BuildMarkdownText_WithMessages_StartsWithH1AndContainsSpeakerH2` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ChatNestWorkspaceViewModelTests` | `BuildMarkdownText_EmptyMessages_ReturnsEmptyString` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `CopyNestSuiteCommand_CanExecute_FalseWhenEmpty` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `CopyMarkdownCommand_CanExecute_FalseWhenEmpty` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `CopyNestSuiteCommand_CanExecute_TrueAfterMessageLoaded` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `ChatNestWorkspaceViewModelTests` | `CopyMarkdownCommand_CanExecute_TrueAfterMessageLoaded` | クラス単位テスト | ChatNestWorkspaceViewModel |  |  |
| `CloseConfirmationServiceTests` | `RequiresConfirmation_WhenNoUnsavedChanges_IsFalse` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `RequiresConfirmation_WhenUnsavedChanges_IsTrue` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `CanCloseSingle_SaveSelectedAndSaveSucceeds_IsTrue` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `CanCloseSingle_SaveSelectedAndSaveFails_IsFalse` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `CanCloseSingle_DiscardSelected_IsTrue` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `CanCloseSingle_CancelSelected_IsFalse` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `EvaluateMany_WhenMiddleTargetCancels_DoesNotEvaluateFollowingTargets` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `EvaluateMany_SkipsTempOrPinnedTabs_WhenCanCloseIsFalse` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `EvaluateMany_UnsavedTargetInvokesConfirmationFlow` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `CloseConfirmationServiceTests` | `EvaluateMany_SaveFailureRecordsFailedTabAndStops` | シナリオ / 回帰テスト | CloseConfirmationService |  |  |
| `DetachedWindowUxAndThemeTests` | `DetachedWorkspaceWindow_MinWidth_Is_870` | シナリオ / 回帰テスト | DetachedWindowUxAndTheme |  |  |
| `DetachedWindowUxAndThemeTests` | `DarkTheme_MarkerHighlightBrushes_AreDistinct` | シナリオ / 回帰テスト | DetachedWindowUxAndTheme |  |  |
| `DetachedWindowUxAndThemeTests` | `DarkTheme_FixmeBrush_HasRedHue` | シナリオ / 回帰テスト | DetachedWindowUxAndTheme |  |  |
| `DetachedWindowUxAndThemeTests` | `DarkTheme_NoteBrush_HasGreenHue` | シナリオ / 回帰テスト | DetachedWindowUxAndTheme |  |  |
| `DetachedWindowUxAndThemeTests` | `DarkTheme_NoteLinkBrush_HasBlueHue` | シナリオ / 回帰テスト | DetachedWindowUxAndTheme |  |  |
| `DetachedWindowUxAndThemeTests` | `ChatNestWorkspaceView_SpeakerToggleStyle_HasExplicitForeground` | シナリオ / 回帰テスト | DetachedWindowUxAndTheme |  |  |
| `DetachedWindowUxAndThemeTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `DetachedWorkspaceCrashGuardTests` | `NoteTitleProvider_WhenDataContextIsNull_ReturnsEmpty` | シナリオ / 回帰テスト | DetachedWorkspaceCrashGuard |  |  |
| `DetachedWorkspaceCrashGuardTests` | `NoteTitleProvider_WhenDataContextIsMainViewModel_ReturnsTitles` | シナリオ / 回帰テスト | DetachedWorkspaceCrashGuard |  |  |
| `DetachedWorkspaceCrashGuardTests` | `NoteTitleProvider_WhenDataContextChangedToNull_ReturnsEmpty` | シナリオ / 回帰テスト | DetachedWorkspaceCrashGuard |  |  |
| `DetachedWorkspaceCrashGuardTests` | `IsNoteEditModeProvider_WhenDataContextIsNull_ReturnsFalse` | シナリオ / 回帰テスト | DetachedWorkspaceCrashGuard |  |  |
| `DetachedWorkspaceCrashGuardTests` | `IsNoteEditModeProvider_WhenDataContextIsMainViewModel_ReflectsVmState` | シナリオ / 回帰テスト | DetachedWorkspaceCrashGuard |  |  |
| `DetachedWorkspaceCrashGuardTests` | `NoteEditorHost_WhenNoteTitleProviderIsNull_InvokeViaNull_ReturnsEmpty` | シナリオ / 回帰テスト | DetachedWorkspaceCrashGuard |  |  |
| `DetachedWorkspaceCrashGuardTests` | `NoteEditorHost_WhenNoteTitleProviderThrows_CanCatchGracefully` | シナリオ / 回帰テスト | DetachedWorkspaceCrashGuard |  |  |
| `DetachedWorkspaceCrashGuardTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `DetachedWorkspaceTests` | `NoteNestTab_IsDetachable_WhenNotDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `IdeaNestTab_IsDetachable_WhenNotDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `ChatNestTab_IsDetachable_WhenNotDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `TempTab_IsNotDetachable` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `NoteNestTab_IsNotDetachable_WhenAlreadyDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `IdeaNestTab_IsNotDetachable_WhenAlreadyDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `ChatNestTab_IsNotDetachable_WhenAlreadyDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `NewNoteNestTab_IsNotDetached_ByDefault` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `Tab_WithIsDetachedTrue_ReflectsDetachedState` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `Tab_WithIsDetachedTrue_CanBeResetToFalse` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `IsDetached_DoesNotAffectIsModified_OrFilePath` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `SavedWorkspaceStateUpdater_PreservesIsDetached_WhenTabIsDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `SavedWorkspaceStateUpdater_PreservesIsDetachedFalse_WhenTabIsNotDetached` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `SessionTabMapper_DetachedTab_ExcludesIsDetachedFromSession` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `SessionTabMapper_CreateSessionState_DetachedTabPreservesFilePath` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `SessionRestoreTarget_FromDetachedTabPath_RestoredAsNormal` | シナリオ / 回帰テスト | DetachedWorkspace |  |  |
| `DetachedWorkspaceTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `DialogServiceBoundaryTests` | `MainWindowAndMainViewModelDoNotOwnConcreteDialogTypes` | クラス単位テスト | DialogServiceBoundary |  |  |
| `DialogServiceBoundaryTests` | `MainViewModelUsesDialogCallbacksForProjectPathSelection` | クラス単位テスト | DialogServiceBoundary |  |  |
| `DialogServiceBoundaryTests` | `DialogServiceExposesUnifiedPathSelectionEntryPoints` | クラス単位テスト | DialogServiceBoundary |  |  |
| `EditorChangeCoordinatorTests` | `EditorContentRoutesToSelectedNote` | クラス単位テスト | EditorChangeCoordinator |  |  |
| `EditorChangeCoordinatorTests` | `SelectionPublishesViewChangeWithoutDataChange` | クラス単位テスト | EditorChangeCoordinator |  |  |
| `EditorLayoutTests` | `UiSettings_NoteNestEditorFontSize_DefaultIs14` | クラス単位テスト | EditorLayout |  |  |
| `EditorLayoutTests` | `ValidateNoteNestEditorFontSize_AcceptsValidValues` | クラス単位テスト | EditorLayout |  |  |
| `EditorLayoutTests` | `ValidateNoteNestEditorFontSize_InvalidValueFallsBackTo14` | クラス単位テスト | EditorLayout |  |  |
| `EditorLayoutTests` | `UiSettingsService_SaveAndLoad_RoundTripsNoteNestEditorFontSize` | クラス単位テスト | EditorLayout |  |  |
| `EditorLayoutTests` | `EditorFontSizeChoices_ContainsExpectedValues` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `EditorLayoutTests` | `EditorFontSizeChoices_DefaultFontSizeIsInList` | クラス単位テスト | EditorLayout |  |  |
| `EditorLayoutTests` | `Project_SchemaVersion_IsUnchangedAt141` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `EditorStateViewModelTests` | `SelectNoteOwnsNoteEditingStateWithoutRaisingContentEdited` | クラス単位テスト | EditorStateViewModel |  |  |
| `EditorStateViewModelTests` | `LoadSettingsDoesNotRaiseSettingsChanged` | クラス単位テスト | EditorStateViewModel |  |  |
| `EditorStateViewModelTests` | `DirectRelatedNoteChangeRaisesEventButSelectionDoesNot` | クラス単位テスト | EditorStateViewModel |  |  |
| `EditorStateViewModelTests` | `SelectTaskAndEditRaiseContentEdited` | クラス単位テスト | EditorStateViewModel |  |  |
| `ErrorLogServiceTests` | `Log_WritesToFile_ContainsExceptionTypeAndMessage` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ErrorLogServiceTests` | `Log_WritesToFile_ContainsOperationName` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ErrorLogServiceTests` | `Log_WritesToFile_ContainsWorkspaceKindAndFilePath` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ErrorLogServiceTests` | `Log_DoesNotThrow_WhenLogPathIsInvalid` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `Log_DoesNotContainUserContent_OnlyMetadata` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `Log_Appends_WhenCalledMultipleTimes` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `Log_IncludesInnerException_WhenPresent` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForLoad_FileNotFoundException_ReturnsSpecificMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForLoad_DirectoryNotFoundException_ReturnsSpecificMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForLoad_UnauthorizedAccessException_ReturnsAccessMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForLoad_SecurityException_ReturnsAccessMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForLoad_JsonException_ReturnsFormatMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForLoad_IOException_ReturnsIoMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForSave_UnauthorizedAccessException_ReturnsAccessMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForSave_IOException_ReturnsIoMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForLoad_UnknownException_ReturnsFallbackMessage` | クラス単位テスト | ErrorLogService |  |  |
| `ErrorLogServiceTests` | `ForSave_UnknownException_ReturnsFallbackMessage` | クラス単位テスト | ErrorLogService |  |  |
| `EventSubscriptionCleanupTests` | `TempNestSlotViewModel_Dispose_IsIdempotent_AndStopsFeedbackTimer` | クラス単位テスト | EventSubscriptionCleanup |  |  |
| `EventSubscriptionCleanupTests` | `TempNestWorkspaceViewModel_Dispose_IsIdempotent_AndStopsSaveTimer` | クラス単位テスト | EventSubscriptionCleanup |  |  |
| `EventSubscriptionCleanupTests` | `ChatNestWorkspaceViewModel_Dispose_IsIdempotent` | クラス単位テスト | EventSubscriptionCleanup |  |  |
| `EventSubscriptionCleanupTests` | `IdeaNestWorkspaceViewModel_Dispose_IsIdempotent` | クラス単位テスト | EventSubscriptionCleanup |  |  |
| `EventSubscriptionCleanupTests` | `TextBoxEditorAdapter_ImplementsDisposable` | クラス単位テスト | EventSubscriptionCleanup |  |  |
| `ExpertProposalPlanningTests` | `PlanningDoc_ExpertProposals_Exists` | シナリオ / 回帰テスト | ExpertProposalPlanning |  |  |
| `ExpertProposalPlanningTests` | `PlanningDoc_Contains_ShortTermSection` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExpertProposalPlanningTests` | `PlanningDoc_Contains_StagedSection` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExpertProposalPlanningTests` | `PlanningDoc_Contains_LongTermSection` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExpertProposalPlanningTests` | `PlanningDoc_Contains_OutOfScopeSection` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExpertProposalPlanningTests` | `PlanningDoc_AI_IsOutOfScope_NotShortTerm` | シナリオ / 回帰テスト | ExpertProposalPlanning |  |  |
| `ExpertProposalPlanningTests` | `Backlog_Contains_SH20_SaveAll` | ドキュメント / ルール固定テスト | docs / version / schema rule | SH-20 |  |
| `ExpertProposalPlanningTests` | `Backlog_Contains_SH19_ShortcutHelp` | ドキュメント / ルール固定テスト | docs / version / schema rule | SH-19 |  |
| `ExpertProposalPlanningTests` | `Backlog_Contains_L15_CharCount` | ドキュメント / ルール固定テスト | docs / version / schema rule | L15 |  |
| `ExpertProposalPlanningTests` | `Backlog_Contains_M15_MarkerCopy` | ドキュメント / ルール固定テスト | docs / version / schema rule | M15 |  |
| `ExpertProposalPlanningTests` | `Backlog_Contains_CH14_FormattedCopy` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-14 |  |
| `ExpertProposalPlanningTests` | `Backlog_Contains_TN7_SlotToWorkspace` | ドキュメント / ルール固定テスト | docs / version / schema rule | TN-7 |  |
| `ExpertProposalPlanningTests` | `Backlog_Contains_LK5_CrossInput` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExpertProposalPlanningTests` | `ReleaseNotes_Contains_V2101` | ドキュメント / ルール固定テスト | docs / version / schema rule | V2101 |  |
| `ExpertProposalPlanningTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `ExportServiceTests` | `SanitizeFileName_ReplacesForwardSlash` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_ReplacesColon` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_ReplacesMultipleInvalidChars` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_ReturnsDefault_WhenEmpty` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_ReturnsDefault_WhenWhitespaceOnly` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_TrimsWhitespace` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_PreservesJapanese` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_AppendsUnderscore_ForWindowsReservedNames` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_ReservedNameCheck_IsCaseInsensitive` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `SanitizeFileName_DottedReservedName_InsertsUnderscoreAfterStem` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `GetUniqueFilePath_ReturnsBasePath_WhenNoConflict` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `GetUniqueFilePath_AddsCounter_WhenConflict` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `GetUniqueFilePath_Increments_WhenMultipleConflicts` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `GetUniqueFilePath_WorksWithJapaneseName` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `BuildProjectText_ContainsProjectName` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExportServiceTests` | `BuildProjectText_ContainsNotebookAndNoteInfo` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExportServiceTests` | `BuildProjectText_ContainsAllNotebooks` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExportServiceTests` | `BuildProjectText_EmptyNotebook_StillHasSeparator` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `BuildProjectText_NoteLinkSyntaxPassedThrough` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `BuildProjectText_MarkerSyntaxPassedThrough` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `BuildNotebookText_ContainsNotebookName` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ExportServiceTests` | `BuildNotebookText_ExcludesOtherNotebooks` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `ExportProjectToText_WritesReadableUtf8File` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `ExportNotebooksToTextFiles_CreatesOneFilePerNotebook` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `ExportNotebooksToTextFiles_UniqueFilesForSameNotebookName` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `ExportNotebooksToTextFiles_SanitizesUnsafeCharactersInFileName` | 機能単位テスト | ExportService |  |  |
| `ExportServiceTests` | `ExportNotebooksToTextFiles_EmptyProject_CreatesNoFiles` | 機能単位テスト | ExportService |  |  |
| `FileOperationsPartialSplitTests` | `FileOperationResponsibilityPartialFiles_Exist` | クラス単位テスト | FileOperationsPartialSplit |  |  |
| `FileOperationsPartialSplitTests` | `FileOperationsOverviewFile_IsKeptSmallAfterSplit` | クラス単位テスト | FileOperationsPartialSplit |  |  |
| `FindReplaceLogicServiceTests` | `ComputeMatchPositions_EmptyKeyword_ReturnsEmpty` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ComputeMatchPositions_NoMatch_ReturnsEmpty` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ComputeMatchPositions_SingleMatch_ReturnsPosition` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ComputeMatchPositions_MultipleMatches_ReturnsAllPositions` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ComputeMatchPositions_CaseSensitive_DoesNotMatchDifferentCase` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ComputeMatchPositions_CaseInsensitive_MatchesDifferentCase` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ComputeMatchPositions_OverlappingPattern_AdvancesOneCharEachTime` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `AdvanceForward_NotAtEnd_AdvancesWithoutWrapping` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `AdvanceForward_AtEnd_WrapsToFirst` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `AdvanceForward_EmptyCount_ReturnsNegativeOne` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `AdvanceBackward_NotAtStart_RetreatsWithoutWrapping` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `AdvanceBackward_AtStart_WrapsToLast` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `AdvanceBackward_EmptyCount_ReturnsNegativeOne` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ReplaceAll_EmptyKeyword_ReturnsOriginalText` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ReplaceAll_CaseSensitive_OnlyReplacesExactCase` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ReplaceAll_CaseInsensitive_ReplacesAnyCase` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `ReplaceAll_EmptyReplacement_DeletesKeyword` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `BuildMatchContext_AtStart_NoLeadingEllipsis` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `BuildMatchContext_AtFarMiddle_HasEllipses` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FindReplaceLogicServiceTests` | `BuildMatchContext_NewlinesReplacedWithSpaces` | クラス単位テスト | FindReplaceLogicService |  |  |
| `FontSettingsValidationServiceTests` | `MinFontSize_Is6` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `MaxFontSize_Is72` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `IsFontSizeInRange_BoundaryAndMid` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_ValidSize_ReturnsTrueWithParsedValue` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_MinSize_ReturnsTrue` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_MaxSize_ReturnsTrue` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_BelowMin_ReturnsFalse` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_AboveMax_ReturnsFalse` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_NonNumericText_ReturnsFalse` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_EmptyText_ReturnsFalse` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FontSettingsValidationServiceTests` | `ValidateFontSize_NullText_ReturnsFalse` | クラス単位テスト | FontSettingsValidationService |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SchemaVersionConstant_Is_1_4_1` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `IdeaNest_SchemaVersionConstant_Is_1_1_4` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `ChatNest_FileVersionConstant_Is_0_4_1` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `ChatNest_FileExtensionConstant_Is_chatnest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `IdeaNest_FileExtensionConstant_Is_ideanest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `TempNest_DefaultJsonVersion_Is_1` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SerializedJson_ContainsProjectNameKey` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SerializedJson_ContainsNotebooksKey` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SerializedJson_ContainsTasksKey` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SerializedJson_ContainsSettingsKey` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SavedJson_IsValidJson` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SaveLoad_PreservesSchemaVersion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNest_SaveLoad_RoundTrip_PreservesNotebookAndNoteStructure` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `IdeaNest_SerializedJson_ContainsVersionField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `IdeaNest_SerializedJson_ContainsIdeasField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `IdeaNest_SerializedJson_ContainsSettingsField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `IdeaNest_SavedJson_IsValidJson` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `IdeaNest_SaveLoad_PreservesVersion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `ChatNest_SerializedJson_ContainsVersionField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `ChatNest_SerializedJson_ContainsMessagesField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `ChatNest_SavedJson_IsValidJson` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `ChatNest_SaveLoad_PreservesMessages` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `TempNest_StoreData_DefaultVersionIs1` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `TempNest_StoreData_HasSlotsArray` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `Session_DefaultState_HasEmptyFilePathsAndNullActivePath` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `Session_SerializedJson_ContainsFilePathsField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `Session_SerializedJson_ContainsActiveFilePathField` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `Session_RoundTrip_PreservesFilePathsAndActivePath` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `FormatSchemaRegressionTests` | `NoteNestSchemaVersion_Remains_1_4_1` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `GuardNestTD26Tests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | TD-26 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `GuardNestTD26Tests` | `AtomicFileWriter_NoTmpRemaining_AfterSuccessfulWrite` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `AtomicFileWriter_NoTmpRemaining_AfterOverwrite` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `AtomicFileWriter_CreatesBakFile_WhenBackupPathProvided` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `AtomicFileWriter_CreatesDirectory_IfNotExist` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `CloseConfirmation_WhenNotDirty_ReturnsNoActionNeeded` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `CloseConfirmation_SaveSuccess_CanClose` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `CloseConfirmation_SaveFailure_PreventClose` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `CloseConfirmation_Discard_AllowsClose` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `CloseConfirmation_Cancel_PreventClose` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `CloseConfirmation_TempTab_SkippedInEvaluateMany` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `FileErrorMessages_ForLoad_FileNotFound_ReturnsJapaneseMessage` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `FileErrorMessages_ForLoad_UnauthorizedAccess_ReturnsDifferentMessage` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `FileErrorMessages_ForSave_IOException_ReturnsJapaneseMessage` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `FileErrorMessages_ForSave_UnauthorizedAccess_ReturnsDifferentMessage` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `ErrorLogService_HasNoLogInfoMethod` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `ErrorLogService_HasNoLogWarningMethod` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `ErrorLogService_HasLogMethod_WithOperationAndException` | シナリオ / 回帰テスト | GuardNestTD26 | TD-26 | 課題番号 / version ベースのテストクラス名。 |
| `GuardNestTD26Tests` | `PolicyDocument_DescribesAtomicFileWriter` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-26 |  |
| `GuardNestTD26Tests` | `PolicyDocument_DescribesCloseConfirmationService` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-26 |  |
| `GuardNestTD26Tests` | `PolicyDocument_DescribesErrorLogService` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-26 |  |
| `GuardNestTD26Tests` | `PolicyDocument_StatesErrorOnlyPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-26 |  |
| `GuardNestTD26Tests` | `PolicyDocument_StatesPersonalDataNotLogged` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-26 |  |
| `GuardNestTD26Tests` | `Backlog_TD26_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-26 |  |
| `GuardNestTD26Tests` | `ReleaseNotes_Contains_V2_10_13` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-26 |  |
| `IdeaNestFileServiceTests` | `SaveAndLoad_RoundTripsCardsTagsOrderAndVersion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `IdeaNestFileServiceTests` | `Load_RejectsWrongExtensionBrokenJsonAndUnsupportedVersion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `IdeaNestFileServiceTests` | `FileExtension_IsExpected` | クラス単位テスト | IdeaNestFileService |  |  |
| `IdeaNestFileServiceTests` | `SchemaVersion_IsExpected` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `IdeaNestFileServiceTests` | `NewWorkspace_UsesCurrentSchemaVersion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `IdeaNestFileServiceTests` | `Idea_Property_HasJsonPropertyNameAttribute` | クラス単位テスト | IdeaNestFileService |  |  |
| `IdeaNestFileServiceTests` | `Workspace_Property_HasJsonPropertyNameAttribute` | クラス単位テスト | IdeaNestFileService |  |  |
| `IdeaNestFileServiceTests` | `WorkspaceSettings_Property_HasJsonPropertyNameAttribute` | クラス単位テスト | IdeaNestFileService |  |  |
| `IdeaNestHoverFocusTests` | `SavedFile_DoesNotContainHoverOrFocusState` | クラス単位テスト | IdeaNestHoverFocus |  |  |
| `IdeaNestHoverFocusTests` | `BuildWorkspaceForSave_DoesNotContainHoverOrFocusFields` | クラス単位テスト | IdeaNestHoverFocus |  |  |
| `IdeaNestHoverFocusTests` | `RoundTrip_LoadAndSave_PreservesCardData` | クラス単位テスト | IdeaNestHoverFocus |  |  |
| `IdeaNestHoverFocusTests` | `CardSize_SwitchPreservesCardWidths` | クラス単位テスト | IdeaNestHoverFocus |  |  |
| `IdeaNestHoverFocusTests` | `CardSize_AfterSwitch_BodyPreviewMaxLinesIsCorrect` | クラス単位テスト | IdeaNestHoverFocus |  |  |
| `IdeaNestHoverFocusTests` | `WorkspaceViewModel_CardOperationCommandsExist` | クラス単位テスト | IdeaNestHoverFocus |  |  |
| `IdeaNestMultiTabSessionTests` | `TwoViewModels_HasChanges_AreIndependent` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `TwoViewModels_MarkSaved_IsIndependent` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `TwoViewModels_LoadFromWorkspace_DoNotCrossContaminate` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `TwoViewModels_LoadFromWorkspace_SetsHasChangesToFalse` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `ViewModelA_PropertyChanged_DoesNotFireOnViewModelB` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_TwoSessions_HoldDistinctViewModelInstances` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_SessionManager_CanHoldMultipleIdeaNestSessions` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_SessionManager_ReverseLookupByViewModel_FindsCorrectSession` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_SessionManager_ReverseLookup_UnregisteredViewModel_ReturnsNull` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_TwoSessions_FilePathsAreIndependent` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_UpdatingSessionAFilePath_DoesNotAffectSessionB` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_UpdatingIsModifiedA_DoesNotAffectSessionB` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_SessionManager_RemoveByTabId_DecreasesCount` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_SessionManager_Remove_ThenTryGet_ReturnsFalse` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_TwoSessions_InManager_RemoveOne_OtherRemains` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `SessionManager_FilterIdeaNestOnly_ExcludesOtherKinds` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `SessionManager_FilterIdeaNestOnly_WhenNoIdeaNest_ReturnsEmpty` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_OpenFilePolicy_SamePath_DetectsAsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_OpenFilePolicy_DifferentPaths_NotDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_OpenFilePolicy_CaseInsensitive_IsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_OpenFilePolicy_NullFilePath_NeverDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_SessionA_FilePathUpdate_DoesNotAffectSessionB_InManager` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_SessionA_IsModifiedUpdate_DoesNotAffectSessionB_InManager` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_Session_WorkspaceKind_IsIdeaNest` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestMultiTabSessionTests` | `IdeaNest_Session_WorkspaceViewModel_IsIdeaNestViewModelType` | シナリオ / 回帰テスト | IdeaNestMultiTabSession |  |  |
| `IdeaNestWorkspaceViewModelTests` | `LoadAndBuildSaveWorkspace_RestoresCardsOrderTagsDatesAndClearsDirty` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `MarkDirtyAndMarkSaved_UpdateHasChanges` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_SetsPasteTitleFormat` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_EmptyBody_ReturnsFalse` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_WhitespaceOnlyBody_ReturnsFalse` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromFileContent_UsesFileNameAsTitle` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromFileContent_EmptyBody_ReturnsFalse` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_SetsTimestamps` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_ChatNestHeader_ExtractsTitleFromHeader` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_ChatNestHeader_StripsHeaderLineFromBody` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_ChatNestHeader_StripsLeadingBlankLineFromBody` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_ChatNestHeader_MultipleSpeakers_KeepsAllContent` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_SimilarButNotMatchingHeader_UsesPasteTitle` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `CommitAddFromText_ChatNestHeaderCrLf_ExtractsTitleCorrectly` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `BodyPreviewMaxLines_ReturnsExpectedLineCountPerSize` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `IdeaNestWorkspaceViewModelTests` | `BodyPreviewMaxLines_DefaultIsMedium` | クラス単位テスト | IdeaNestWorkspaceViewModel |  |  |
| `InputDialogLogicTests` | `ProcessInput_NormalText_IsReturnedAsIs` | クラス単位テスト | InputDialogLogic |  |  |
| `InputDialogLogicTests` | `ProcessInput_LeadingWhitespace_IsNotTrimmed` | クラス単位テスト | InputDialogLogic |  |  |
| `InputDialogLogicTests` | `ProcessInput_TrailingWhitespace_IsNotTrimmed` | クラス単位テスト | InputDialogLogic |  |  |
| `InputDialogLogicTests` | `ProcessInput_EmptyString_IsAccepted` | クラス単位テスト | InputDialogLogic |  |  |
| `InputDialogLogicTests` | `IsAcceptable_NonNullText_ReturnsTrue` | クラス単位テスト | InputDialogLogic |  |  |
| `InputDialogLogicTests` | `IsAcceptable_NullText_ReturnsFalse` | クラス単位テスト | InputDialogLogic |  |  |
| `LightImprovementsV2103Tests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | V2103 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `LightImprovementsV2103Tests` | `TempNestSlotViewModel_ClearCommand_CanExecute_False_WhenEmpty` | シナリオ / 回帰テスト | LightImprovementsV2103 | V2103 | 課題番号 / version ベースのテストクラス名。 |
| `LightImprovementsV2103Tests` | `TempNestSlotViewModel_ClearCommand_CanExecute_True_WhenTitleNonEmpty` | シナリオ / 回帰テスト | LightImprovementsV2103 | V2103 | 課題番号 / version ベースのテストクラス名。 |
| `LightImprovementsV2103Tests` | `TempNestSlotViewModel_ClearCommand_CanExecute_True_WhenBodyNonEmpty` | シナリオ / 回帰テスト | LightImprovementsV2103 | V2103 | 課題番号 / version ベースのテストクラス名。 |
| `LightImprovementsV2103Tests` | `TempNestSlotViewModel_ConfirmClear_Property_DefaultsToNull` | シナリオ / 回帰テスト | LightImprovementsV2103 | V2103 | 課題番号 / version ベースのテストクラス名。 |
| `LightImprovementsV2103Tests` | `TempNestSlotViewModel_ClearCommand_Execute_ClearsWithoutConfirm_WhenConfirmClearNull` | シナリオ / 回帰テスト | LightImprovementsV2103 | V2103 | 課題番号 / version ベースのテストクラス名。 |
| `LightImprovementsV2103Tests` | `TempNestSlotViewModel_ClearCommand_Execute_ClearsWhenConfirmClearReturnsTrue` | シナリオ / 回帰テスト | LightImprovementsV2103 | V2103 | 課題番号 / version ベースのテストクラス名。 |
| `LightImprovementsV2103Tests` | `TempNestSlotViewModel_ClearCommand_Execute_DoesNotClearWhenConfirmClearReturnsFalse` | シナリオ / 回帰テスト | LightImprovementsV2103 | V2103 | 課題番号 / version ベースのテストクラス名。 |
| `LightImprovementsV2103Tests` | `Backlog_TN2_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | TN-2, V2103 |  |
| `LightImprovementsV2103Tests` | `Backlog_L14_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | L14, V2103 |  |
| `LightImprovementsV2103Tests` | `Backlog_L15_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | L15, V2103 |  |
| `LightImprovementsV2103Tests` | `Backlog_CH13_InChatNestSection` | ドキュメント / ルール固定テスト | docs / version / schema rule | CH-13, V2103 |  |
| `LightImprovementsV2103Tests` | `ReleaseNotes_Contains_V2103` | ドキュメント / ルール固定テスト | docs / version / schema rule | V2103 |  |
| `MainViewModelCompositionTests` | `FacadeExposesIndependentResponsibilityOwners` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `SelectionChangesDoNotMarkProjectModified` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `EditorFacadePropagatesContentAndPersistentSettings` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `EditorFacadePropagatesTaskComment` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `EditorRelatedNoteChangePropagatesToEditingTask` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `EditorRelatedNoteClearPropagatesToEditingTask` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `DirectNoteChangeMarksProjectModifiedAndRefreshesMarkers` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `DirectSessionChangesPropagateThroughMainViewModelFacade` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelCompositionTests` | `TaskCommentModeSuppressesNoteTimestampTooltipText` | クラス単位テスト | MainViewModelComposition |  |  |
| `MainViewModelFacadeTests` | `XamlCompatibilityPropertiesForwardToResponsibilityOwners` | クラス単位テスト | MainViewModelFacade |  |  |
| `MainViewModelFacadeTests` | `ExistingCompatibilityPropertiesForwardToResponsibilityOwners` | クラス単位テスト | MainViewModelFacade |  |  |
| `MainViewModelFacadeTests` | `SessionNotificationsOnlyRelayPropertiesExposedByFacade` | クラス単位テスト | MainViewModelFacade |  |  |
| `MainViewModelFacadeTests` | `ClearingEditorRefreshesMarkersFromRemainingNoteWorkspaceNotes` | クラス単位テスト | MainViewModelFacade |  |  |
| `MainViewModelFacadeTests` | `NoteChangesOnlyPublishActiveFacadePropertyNames` | クラス単位テスト | MainViewModelFacade |  |  |
| `MainViewModelPartialTests` | `AddNotebookWithTitle_SetsStatusMessage` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `AddNoteToNotebook_ReturnsTrue_SelectsNote_SetsStatusMessage` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `AddNoteToNotebook_DuplicateTitle_ReturnsFalse` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `RenameNote_ValidName_ReturnsTrueAndUpdatesTitle` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `RenameNote_DuplicateName_ReturnsFalseAndTitleUnchanged` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `DeleteNote_WhenSelectedNote_ClearsEditorSelection` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `DeleteNote_WhenDifferentNoteSelected_DoesNotClearEditor` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `DeleteNote_ClearsLinkedNoteIdFromTasks` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `DeleteNotebook_RemovesAllNotesAndClearsTaskLinks` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `DeleteNotebook_WhenSelectedNoteIsInside_ClearsEditorSelection` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `DuplicateNote_SelectsNewNote_SetsStatusMessage` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `FindNoteById_ReturnsMatchingNote` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `FindNoteById_UnknownId_ReturnsNull` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `FindNoteByTitle_CaseInsensitive_ReturnsNote` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `FindNoteByTitle_NotExists_ReturnsNull` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `NoteNameExists_ReturnsTrueWhenExists` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `NoteNameExists_ReturnsFalseWhenNotExists` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `NoteNameExists_ExcludesSelf_AllowsSameName` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `NavigateToNote_SelectsNoteAndInvokesSyncCallback` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `MoveNoteToNotebook_SetsStatusMessageWithDestinationTitle` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `RenameTask_UpdatesTitle` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `SetTaskRelatedNote_WhenEditingTask_UpdatesLinkedNoteIdViaEditor` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `SetTaskRelatedNote_WhenNotEditingTask_SetsLinkedNoteIdDirectly` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `ClearTaskRelatedNote_WhenEditingTask_ClearsLinkedNoteId` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `ClearTaskRelatedNote_WhenNotEditingTask_ClearsLinkedNoteId` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `ApplyFontSettings_UpdatesEditorFontFamilyAndSize` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `ApplyFontSettings_ReflectedInEditorFontSizeFacadeProperty` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `MarkerPanel_AggregatesMarkersAcrossMultipleNotes` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `MarkerPanel_EmptyNoteContent_ProducesNoMarkers` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `MarkerPanel_DeleteNote_RemovesItsMarkers` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `LinkPanel_RefreshesOnNoteSelection_ShowsOutboundLinks` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainViewModelPartialTests` | `LinkPanel_UnresolvableLink_IsMarkedBroken` | クラス単位テスト | MainViewModelPartial |  |  |
| `MainWindowEventBoundaryTests` | `ContextMenuResolutionUsesExplicitlyNamedSharedHelper` | クラス単位テスト | MainWindowEventBoundary |  |  |
| `MainWindowEventBoundaryTests` | `DragDropUsesSharedThresholdAndEffectHelpers` | クラス単位テスト | MainWindowEventBoundary |  |  |
| `MarkdownExportTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `MarkdownExportTests` | `BuildCurrentNoteMarkdown_StartsWithH1Title` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildCurrentNoteMarkdown_EmptyTitle_UsesDefaultTitle` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildCurrentNoteMarkdown_ContentIsIncluded` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildCurrentNoteMarkdown_EmptyContent_OutputsHeaderOnly` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildCurrentNoteMarkdown_NoteLink_OutputsAsIs` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildCurrentNoteMarkdown_TitleWithNewline_ReplacesWithSpace` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildCurrentNoteMarkdown_HasBlankLineBetweenTitleAndContent` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildAllNotesMarkdown_StartsWithH1ProjectName` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildAllNotesMarkdown_EachNoteIsH2` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildAllNotesMarkdown_NotesSeparatedByHRule` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildAllNotesMarkdown_SingleNote_NoHRule` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildAllNotesMarkdown_EmptyTitle_UsesDefaultTitle` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildAllNotesMarkdown_NoteLink_OutputsAsIs` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `BuildAllNotesMarkdown_ContentIsIncluded` | 機能単位テスト | MarkdownExport |  |  |
| `MarkdownExportTests` | `Backlog_M10_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | M10 |  |
| `MarkdownExportTests` | `ReleaseNotes_Contains_V2105` | ドキュメント / ルール固定テスト | docs / version / schema rule | V2105 |  |
| `MarkerExtractorServiceTests` | `Extract_EmptyContent_ReturnsEmpty` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `Extract_NoMarkers_ReturnsEmpty` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `Extract_SingleTodo_ReturnsCorrectFields` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `Extract_AllThreeTypes_ReturnsAll` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `Extract_LineNumbers_AreOneBased` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `Extract_LowercaseKeyword_NotMatched` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `Extract_ExcerptTrimsLeadingSpace` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `HasMarkers_ReturnsTrueForTodo` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `HasMarkers_ReturnsTrueForFixme` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `HasMarkers_ReturnsTrueForNote` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `HasMarkers_ReturnsFalseForEmptyContent` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `HasMarkers_ReturnsFalseForPlainText` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerExtractorServiceTests` | `HasMarkers_ReturnsFalseForHack` | クラス単位テスト | MarkerExtractorService |  |  |
| `MarkerLineDetectorTests` | `Detect_EmptyString_ReturnsEmpty` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NullEquivalent_ReturnsEmpty` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoMarkers_ReturnsEmpty` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_TodoUppercase_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_FixmeUppercase_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoteUppercase_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_HackUppercase_NotDetected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_TodoLowercase_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_FixmeLowercase_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoteLowercase_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_MixedCase_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_MultipleMarkerLines_AllDetected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_LineWithMultipleMarkers_ReturnedOnce` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_MarkerInMiddleOfLine_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NonMarkerLineNotReturned` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_LineNumbers_AreZeroBased` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_SingleLineNoNewline_Correct` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_TrailingNewline_NoExtraLine` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_TodoLine_KindIsTodo` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_FixmeLine_KindIsFixme` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoteLine_KindIsNote` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoteLinkLine_KindIsNoteLink` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoteLinkOnly_Detected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoteLinkMultiple_AllDetected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_DoubleBracketNoClosing_StillDetected` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_FixmeAndTodoOnSameLine_KindIsFixme` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_TodoAndNoteOnSameLine_KindIsTodo` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_NoteAndNoteLinkOnSameLine_KindIsNote` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `Detect_FixmeBeatsAllOthersOnSameLine` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `NoteNestSchema_Remains_141` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `MarkerLineDetectorTests` | `NoteNestSave_DoesNotContainMarkerHighlightState` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `ThemeDictionary_ContainsMarkerLineHighlightBrush` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `MarkerLineDetectorTests` | `ThemeDictionary_ContainsPerKindMarkerBrushes` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `MarkerLineDetectorTests` | `MarkerLineHighlightBrush_IsFullyOpaque` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerLineDetectorTests` | `PerKindMarkerBrush_IsFullyOpaque` | クラス単位テスト | MarkerLineDetector |  |  |
| `MarkerPanelViewModelTests` | `RefreshOwnsFilteringAndSummary` | クラス単位テスト | MarkerPanelViewModel |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_HasId` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_HasWorkspaceKind` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_NoteNest_ToolId_IsNoteNest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_ChatNest_ToolId_IsChatNest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_IdeaNest_ToolId_IsIdeaNest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_WithoutFilePath_IsUntitled` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_WithFilePath_IsNotUntitled` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_DefaultIsModified_IsFalse` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `DocumentTab_CanBeMarkedModified` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `ToolDefinitionAndDocumentTab_AreDistinctTypes` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `MultipleDocumentTabs_CanHaveSameWorkspaceKind` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_CreateUntitled_NoteNest_IsUntitled` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_CreateUntitled_ChatNest_DisplayName_HasChatNestExtension` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_CreateUntitled_IdeaNest_DisplayName_HasIdeaNestExtension` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_FromFilePath_NoteNest_ResolvesCorrectly` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_FromFilePath_ChatNest_ResolvesCorrectly` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_FromFilePath_UnknownExtension_Throws` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_TryGetKind_NoteNestExtension_ReturnsNoteNest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_TryGetKind_UnknownExtension_ReturnsFalse` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `WorkspaceKind_HasThreeValues_NoteNest_ChatNest_IdeaNest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `GetExtension_ReturnsCorrectExtension_ForEachKind` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `ExtensionMapping_RoundTrips` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TryGetKind_IsCaseInsensitive_ForAllIntegratedTools` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TryGetKind_DoesNotMisclassifyIntegratedExtensions` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_FromFilePath_NoteNestExtension_IsNotChatNestKind` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_FromFilePath_ChatNestExtension_IsNotNoteNestKind` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_TryGetKind_ChatNestExtension_ReturnsCorrectKind` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_FromFilePath_IdeaNestExtension_ResolvesCorrectly` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TabFactory_TryGetKind_IdeaNestExtension_ReturnsIdeaNest` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TooltipText_NoteNest_SavedTab_ContainsKindAndPath` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TooltipText_UntitledTab_ShowsUntitledAndUnsaved` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TooltipText_ModifiedTab_ShowsUnsavedState` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TooltipText_SavedTab_ShowsSavedState` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteDocumentTabTests` | `TooltipText_ContainsCorrectKindLabel_ForEachTool` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `TabFactory_CreateUntitled_GeneratesUniqueIds` | クラス単位テスト | NestSuiteMultiFileTabsDesign |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `TabFactory_FromFilePath_SamePath_StillGeneratesDistinctIds` | クラス単位テスト | NestSuiteMultiFileTabsDesign |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `OpenFilePolicy_SamePath_IsSameFile` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `OpenFilePolicy_DifferentCase_IsSameFile` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `OpenFilePolicy_DifferentPath_IsNotSameFile` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `OpenFilePolicy_NullPath_IsNotSameFile` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `MultipleTabs_SameWorkspaceKind_IsStillExpressible` | クラス単位テスト | NestSuiteMultiFileTabsDesign |  |  |
| `NestSuiteMultiFileTabsDesignTests` | `ExtensionResolution_IsUnchanged` | クラス単位テスト | NestSuiteMultiFileTabsDesign |  |  |
| `NestSuiteRecentFilesServiceTests` | `Load_EmptyState_ReturnsEmpty` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Add_NewPath_AppearsAtFront` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Add_DuplicatePath_MovedToFront` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Add_ExceedsTenItems_ListTrimmedToTen` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Add_PersistsBetweenInstances` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Remove_ExistingPath_RemovesFromList` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Remove_NonExistentPath_ReturnsUnchangedList` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Clear_RemovesAllItems` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Add_WriteFailure_ReturnsPersistedListWithoutCrash` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Add_NoTmpFileLeft_AfterSuccessfulWrite` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Load_CorruptedJson_ReturnsEmpty` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteRecentFilesServiceTests` | `Remove_UnsupportedExtensionPath_RemovesFromList` | クラス単位テスト | NestSuiteRecentFilesService |  |  |
| `NestSuiteSessionStateServiceTests` | `Load_EmptyState_ReturnsEmpty` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Load_EmptyState_ReturnsNewInstance` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Save_AndLoad_RoundTrip` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Save_PersistsBetweenInstances` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Save_SupportsAllThreeExtensions` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Save_NullActiveFilePath_IsPreserved` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Save_EmptyFilePaths_IsPreserved` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Load_CorruptedJson_ReturnsEmpty` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteSessionStateServiceTests` | `Save_WriteFailure_DoesNotCrash` | クラス単位テスト | NestSuiteSessionStateService |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_IsWindowSubclass` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_ImplementsIWorkspaceDialogHost` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasNoteNestWorkspaceViewField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NoteNestWorkspaceView_StillIsNotWindow` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_OverridesOnClosing` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasUnintegratedPlaceholderField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasChatWorkspaceViewField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_ChatNestViewModels_ManagedBySessionManager_NotSingleField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_DefaultToolId_IsNoteNest` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSelectedToolIdProperty` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadInitialFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_ViewModelProperty_IsMainViewModelType` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_AllTools_ContainsThreeEntries` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_AllTools_IsNotMutableArray` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_NoteNest_IsFirstBuiltInTool` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_NoteNest_IsIntegrated` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_IdeaNest_IsIntegrated` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_ChatNest_IsIntegrated` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_ToolDefinitions_ContainsThreeEntries` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_ToolDefinitions_IsNotMutableArray` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_ToolDefinitions_FirstIsNoteNest` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_NoteNestDef_IsIntegrated` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_IdeaNestDef_IsIntegrated` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_ChatNestDef_IsIntegrated` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_AllThreeTools_AreIntegrated` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasTabStripField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasTabsCollectionField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasActivateTabMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasReplaceTabMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSyncNoteNestTabForViewModelMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasCloseTabMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_IsClosingTabField_IsRemovedInV198` | クラス単位テスト | NestSuiteShell | V198 |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadInitialChatNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasIdeaNestWorkspaceViewField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_IdeaNestViewModelField_IsRemovedInV197` | クラス単位テスト | NestSuiteShell | V197 |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSyncIdeaNestTabForViewModelMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasConfirmAndResetIdeaNestMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_IdeaNestDef_StatusText_IsIntegrationTest` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasOnIdeaNestPropertyChangedMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `IdeaNestWorkspaceViewModel_DoesNotHaveDirtyRequestedEvent` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `IdeaNestWorkspaceViewModel_HasMarkDirtyMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `IdeaNestWorkspaceViewModel_HasLoadFromWorkspaceMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadInitialFileMethod_AcceptsIdeaNestExtension` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_TryLoadIdeaNestFile_IsRemovedInV197` | クラス単位テスト | NestSuiteShell | V197 |  |
| `NestSuiteShellTests` | `NestSuiteTabFactory_IdeaNestExtension_IsRecognizedForLoading` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteTabFactory_IdeaNestExtension_IsNotNoteNest` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteTabFactory_IdeaNestExtension_IsNotChatNest` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteToolRegistry_AllThreeTools_NoteNestFirst` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_Constructor_AcceptsOptionalStringParameter` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasEnsureDefaultTabMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_NullFilePath_ShouldCreateInitialTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_EmptyFilePath_ShouldCreateInitialTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_WithFilePath_ShouldNotCreateInitialTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_ZeroTabs_ShouldEnsureFallbackTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_HasTabs_ShouldNotEnsureFallbackTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSessionManagerField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasCreateSessionForTabMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasTryGetActiveSessionMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteWorkspaceSession_HasTabIdProperty` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteWorkspaceSession_HasWorkspaceKindProperty` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteWorkspaceSession_HasMutableFilePathAndIsModified` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteWorkspaceSessionManager_HasAddRemoveTryGetMethods` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasCreateChatNestViewModelMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSyncChatNestTabForViewModelMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasNewChatNestSessionMethod_NoParameters` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_TrySaveChatNestToPath_TakesSessionParameter` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasNormalizeFilePathMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasUpdateChatNestTabPathMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasOpenChatNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasOnChatNestPropertyChangedMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasConfirmAndResetChatNestMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasCreateNoteNestViewModelMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasNewNoteNestSessionMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasOpenNoteNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSaveNoteNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadInitialNoteNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasOnNoteNestSessionPropertyChangedMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasConfirmAndResetNoteNestMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSaveNoteNestFileAsMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasCreateIdeaNestViewModelMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasNewIdeaNestSessionMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasOpenIdeaNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSaveIdeaNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSaveIdeaNestFileAsMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadInitialIdeaNestFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasConfirmAndResetIdeaNestMethod_ReturnsBool` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `MainViewModel_HasSaveToPathMethod_ReturnsBool` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasOpenNestSuiteFileMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadNoteNestFileAtMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadChatNestFileAtMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasLoadIdeaNestFileAtMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_MenuNew_Click_IsRemovedInV1101` | クラス単位テスト | NestSuiteShell | V1101 |  |
| `NestSuiteShellTests` | `NestSuiteTabFactory_TryGetKind_NoteNestExtension_ReturnsNoteNestKind` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteTabFactory_TryGetKind_ChatNestExtension_ReturnsChatNestKind` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteTabFactory_TryGetKind_UnsupportedExtension_ReturnsFalse` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_WithNullPath_ShouldCreateInitialTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_WithEmptyPath_ShouldCreateInitialTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_AllThreeKindPaths_SuppressInitialTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupTabPolicy_WithUnsupportedExtension_SuppressesInitialTab` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `StartupArgParser_GetFilePath_ReturnsNonFlagArg` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `StartupArgParser_GetFilePath_WithNoFileArg_ReturnsNull` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasRecentFilesMenuField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasUpdateRecentFilesMenuMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasRecentFilesServiceField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteRecentFilesService_DefaultDataPath_ContainsNestSuiteFileName` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSessionStateServiceField` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSaveSessionMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasTryRestoreSessionMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteSessionStateService_DefaultDataPath_ContainsSessionFileName` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteShellTests` | `DialogService_HasSelectNestSuiteOpenPathsMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `DialogService_DoesNotHaveSingleSelectNestSuiteOpenPathMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasCommandSave_ExecutedMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteShellTests` | `NestSuiteShellWindow_HasSaveActiveTabMethod` | クラス単位テスト | NestSuiteShell |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Add_Session_Count_Increases` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Add_DuplicateTabId_Overwrites` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `TryGet_ExistingTabId_ReturnsSession` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `TryGet_NonExistentTabId_ReturnsFalse` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Remove_ExistingTabId_ReturnsTrue_AndDecrementsCount` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Remove_NonExistentTabId_ReturnsFalse` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `After_Remove_TryGet_ReturnsFalse` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Sessions_ReturnsAllAddedSessions` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Sessions_Empty_ReturnsEmptyCollection` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Contains_ExistingTabId_ReturnsTrue` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Contains_NonExistentTabId_ReturnsFalse` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Session_FilePath_CanBeUpdated` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Session_IsModified_CanBeUpdated` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Session_Properties_AreSetFromConstructor` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `Session_DefaultFilePath_IsNull` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NestSuiteWorkspaceSessionManagerTests` | `MultipleSessionsOfSameKind_CanShareSameViewModelReference` | クラス単位テスト | NestSuiteWorkspaceSessionManager |  |  |
| `NoteChangeCoordinatorTests` | `NoteDataChangeRefreshesMarkersAndPublishesSemanticProperties` | クラス単位テスト | NoteChangeCoordinator |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_EmptyLine_ProducesNoHighlight` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_WhitespaceOnlyLine_ProducesNoHighlight` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_SingleCharLine_WithoutMarker_ProducesNoHighlight` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_HackLine_ExcludedFromHighlightSystem` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_HackLowercase_ExcludedFromHighlightSystem` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_MarkerKeyword_CaseInsensitive_CorrectKind` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_NoteLinkWithNoteInTitle_IsNoteLink_NotNote` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_NoteKeywordOutsideBracket_IsNote_NotNoteLink` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Classify_NoteKeywordAfterClosedBracket_IsNote_NotNoteLink` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Priority_FixmeBeatorTodo` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Priority_TodoBeatsNote` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Priority_NoteBeatsNoteLink` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Priority_TodoBeatsNoteLink` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Priority_FixmeBeatsNoteLink` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Priority_AllFourKindsOnSameLine_FixmeAlwaysWins` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `Priority_MultiLine_EachLineClassifiedIndependently` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_TextStartingWithNewline_Line0Returns0` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_TextStartingWithNewline_Line1Returns1` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_OnlyNewlines_Line0Returns0` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_OnlyNewlines_Line2Returns2` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_LongSingleLine_Line0Returns0` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_LongSingleLine_Line1ReturnsMinusOne` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_VeryLargeLineIndex_ReturnsMinusOne` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `LogicalLineStartChar_JapaneseMbcs_Line1CorrectOffset` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `ThemeChangedEvent_CanSubscribeAndUnsubscribe_WithoutThrowing` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `ThemeChangedEvent_WhenFired_NotifiesSubscriber` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `ThemeChangedEvent_AfterUnsubscribe_HandlerNotCalled` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `ThemeChangedEvent_MultipleSubscribers_AllNotified` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `ThemeDictionary_PerKindHighlightBrush_Exists` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `ThemeDictionary_PerKindHighlightBrush_HasNonEmptyColor` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `NoteNestSave_DoesNotContainLineHighlightKind` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `NoteNestSave_DoesNotContainLineHighlightInfo` | シナリオ / 回帰テスト | NoteEditorHostHighlightRegression |  |  |
| `NoteEditorHostHighlightRegressionTests` | `NoteNestSchema_RemainsAt141` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_WithNull_ClearsAllAndHasNoteIsFalse` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_WithNote_SetsHasNoteTrue` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_ExtractsOutboundLinksFromContent` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_ResolvedOutboundLink_IsNotBroken` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_UnresolvedOutboundLink_IsBroken` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_LinkResolutionIsCaseInsensitive` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_BuildsBacklinksFromOtherNotes` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_SelectedNoteExcludedFromBacklinks` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `HasNoOutboundLinks_TrueWhenNoteSelectedButNoLinks` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `HasNoBacklinks_TrueWhenNoteSelectedButNoBacklinks` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `HasNoNote_TrueInitially` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `CountTexts_ReflectLinkCounts` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `BacklinkEntry_DisplayText_IsSourceNoteTitle` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkPanelViewModelTests` | `Refresh_DuplicateTitlesInData_DoesNotThrow` | クラス単位テスト | NoteLinkPanelViewModel |  |  |
| `NoteLinkServiceTests` | `ExtractLinkAtCursor_ReturnsNull_WhenNoLink` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractLinkAtCursor_ReturnsTitle_WhenCaretInsideLink` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractLinkAtCursor_ReturnsTitle_WhenCaretAtOpenBracket` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractLinkAtCursor_ReturnsTitle_WhenCaretAtCloseBracket` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractLinkAtCursor_ReturnsNull_WhenCaretJustAfterLink` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractLinkAtCursor_ReturnsCorrectTitle_WhenMultipleLinks` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractAllLinks_ReturnsAllTitles` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractAllLinks_ReturnsEmpty_WhenNoLinks` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractAllLinks_IgnoresNestedBrackets` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractAllLinks_DuplicateLink_ReturnsBothOccurrences` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractAllLinks_EmptyContent_ReturnsEmpty` | クラス単位テスト | NoteLinkService |  |  |
| `NoteLinkServiceTests` | `ExtractLinkAtCursor_EmptyContent_ReturnsNull` | クラス単位テスト | NoteLinkService |  |  |
| `NoteNestCloseConfirmationTests` | `EvaluateSingle_WhenNotModified_ReturnsNoActionNeeded` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `EvaluateSingle_WhenModified_SaveDecision_SaveSuccess_ReturnsSave` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `EvaluateSingle_WhenModified_SaveDecision_SaveFail_ReturnsCancel` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `EvaluateSingle_WhenModified_SaveAsCancel_ReturnsCancel` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `EvaluateSingle_WhenModified_DiscardDecision_ReturnsDiscard` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `EvaluateSingle_WhenModified_CancelDecision_ReturnsCancel` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `CanCloseSingle_WhenModified_SaveSuccess_ReturnsTrue` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `CanCloseSingle_WhenModified_SaveFail_ReturnsFalse` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `CanCloseSingle_WhenModified_Discard_ReturnsTrue` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `CanCloseSingle_WhenModified_Cancel_ReturnsFalse` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `CanCloseSingle_WhenNotModified_ReturnsTrue` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `SaveDecision_WhenSaveSucceeds_SaveFunctionCalledOnce` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `DiscardDecision_SaveFunctionNotCalled` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `OldYesNoDialog_WouldNotOfferSaveOption` | シナリオ / 回帰テスト | NoteNestCloseConfirmation |  |  |
| `NoteNestCloseConfirmationTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `NoteNestMultiFileDesignTests` | `NoteNestSession_WorkspaceKind_IsNoteNest` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `NoteNestSession_Untitled_FilePathIsNull` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `NoteNestSession_FilePath_CanBeUpdatedAfterSave` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `NoteNestSession_IsModified_CanBeUpdated` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `TwoNoteNestSessions_HoldDistinctFilePaths` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `TwoNoteNestSessions_CanHoldDistinctViewModelInstances` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `TwoNoteNestSessions_UpdatingFilePathA_DoesNotAffectB` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `TwoNoteNestSessions_UpdatingIsModifiedA_DoesNotAffectB` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `NoteNestSessions_CanBeAddedToSessionManager` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `SessionManager_FilterByNoteNestKind_ExcludesChatNestAndIdeaNest` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `NoteNest_SaveSchema_IsVersion_1_4_1` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiFileDesignTests` | `TabFactory_NoteNestExtension_IsRecognizedAsNoteNest` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `TabFactory_NoteNestExtension_IsNotChatNest` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `TabFactory_NoteNestExtension_IsNotIdeaNest` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `OpenFilePolicy_SameNoteNestPath_IsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiFileDesignTests` | `OpenFilePolicy_DifferentNoteNestPaths_AreNotDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiFileDesignTests` | `OpenFilePolicy_CaseInsensitive_NoteNestIsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiFileDesignTests` | `OpenFilePolicy_AfterNormalization_NoteNestDetectedAsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiFileDesignTests` | `ChatNest_MultiTab_TwoViewModels_AreIndependent` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiFileDesignTests` | `ChatNest_MultiTab_SessionManager_StillWorks` | 機能単位テスト | NoteNestMultiFileDesign |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_TwoSessions_HoldDistinctMainViewModelInstances` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_SessionManager_CanHoldMultipleNoteNestSessions` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_SessionManager_ReverseLookupByViewModel_FindsCorrectSession` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_SessionManager_ReverseLookup_UnregisteredViewModel_ReturnsNull` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `SessionManager_FilterNoteNestOnly_ExcludesChatNestSessions` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `SessionManager_FilterNoteNestOnly_WhenNoNoteNest_ReturnsEmpty` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_TwoSessions_FilePathsAreIndependent` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_UpdatingSessionAFilePath_DoesNotAffectSessionB` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_UpdatingIsModifiedA_DoesNotAffectSessionB` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_OpenFilePolicy_SamePath_DetectsAsDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_OpenFilePolicy_DifferentPaths_NotDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_OpenFilePolicy_NullFilePath_NeverDuplicate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `NoteNestMultiTabSessionTests` | `MainViewModel_ImplementsIDisposable` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `MainViewModel_Dispose_DoesNotThrow` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `MainViewModel_Dispose_CanBeCalledTwice_WithoutError` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `MainViewModel_AutoSaveTimer_IsEnabled_AfterConstruction` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `MainViewModel_Dispose_StopsAutoSaveTimer` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_SessionManager_RemoveByTabId_DecreasesCount` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_SessionManager_Remove_ThenTryGet_ReturnsFalse` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_TwoSessions_InManager_RemoveOne_OtherRemains` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_SessionA_FilePathUpdate_DoesNotAffectSessionB_InManager` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_SessionA_IsModifiedUpdate_DoesNotAffectSessionB_InManager` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_Session_WorkspaceKind_IsNoteNest` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NoteNestMultiTabSessionTests` | `NoteNest_Session_WorkspaceViewModel_IsMainViewModelType` | シナリオ / 回帰テスト | NoteNestMultiTabSession |  |  |
| `NotePickerFilterServiceTests` | `FilterByTitle_EmptyFilter_ReturnsAllNotes` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `FilterByTitle_NullFilter_ReturnsAllNotes` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `FilterByTitle_MatchingFilter_ReturnsOnlyMatching` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `FilterByTitle_CaseInsensitive_MatchesRegardlessOfCase` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `FilterByTitle_NoMatch_ReturnsEmpty` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `FilterByTitle_PartialMatch_ReturnsPartialMatches` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `HasDuplicateTitle_NoDuplicates_ReturnsFalse` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `HasDuplicateTitle_HasDuplicate_ReturnsTrue` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `HasDuplicateTitle_CaseInsensitiveDuplicate_ReturnsTrue` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `HasDuplicateTitle_EmptyList_ReturnsFalse` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `TitleMatchesFilter_SubstringMatch_ReturnsTrue` | クラス単位テスト | NotePickerFilterService |  |  |
| `NotePickerFilterServiceTests` | `TitleMatchesFilter_NoMatch_ReturnsFalse` | クラス単位テスト | NotePickerFilterService |  |  |
| `NoteTaskModelTests` | `Priority_Default_NotWrittenToJson` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `DueDate_Null_NotWrittenToJson` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `LinkedNoteId_Null_NotWrittenToJson` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `Priority_NonDefault_RoundTrip` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `DueDate_Set_RoundTrip` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `LinkedNoteId_Set_RoundTrip` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `Load_LegacyJson_DefaultsApplied` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `Load_LegacyJson_WithoutNewSettingsFields_LoadsWithDefaults` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteTaskModelTests` | `Load_JsonWithLinkedNoteId_RoundTripsThroughSave` | クラス単位テスト | NoteTaskModel |  |  |
| `NoteWorkspaceViewModelTests` | `AddNote_PreventsDuplicateNamesAndBuildsModels` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DirectCollectionTitleAndContentChangesRaiseChanged` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `LoadDoesNotRaiseChanged` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DuplicateNote_AddsCopyWithSuffixInSameNotebook` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DuplicateNote_CopiesContent` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DuplicateNote_HasDifferentId` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DuplicateNote_HasNewTimestamps` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DuplicateNote_DoesNotModifyOriginal` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DuplicateNote_NumberedSuffixWhenCopyAlreadyExists` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `NoteWorkspaceViewModelTests` | `DuplicateNote_IncrementsNumberUntilUnique` | クラス単位テスト | NoteWorkspaceViewModel |  |  |
| `ProjectDocumentServiceTests` | `LoadAndBuildRoundTripResponsibilityOwners` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ProjectFileServiceTests` | `Save_NewFile_CreatesFile` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_NewFile_NoTempFileLeft` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_ExistingFile_CreatesBackup` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `SaveLoad_RoundTrip_PreservesProjectName` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Load_InvalidJson_ThrowsInvalidDataException` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Load_EmptyFile_Throws` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Load_FromBackup_RestoresPreviousState` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_PreservesNotebooksAndNotes` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_PreservesNoteIds` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_PreservesSettings` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_PreservesAllTaskGroups` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_OverwritesPreviousBackupOnRepeatedSaves` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectFileServiceTests` | `Save_DoesNotLeaveTempFile_AfterMultipleSaves` | クラス単位テスト | ProjectFileService |  |  |
| `ProjectLifecycleBoundaryTests` | `LifecycleDoesNotOwnExportResponsibility` | クラス単位テスト | ProjectLifecycleBoundary |  |  |
| `ProjectLifecycleBoundaryTests` | `RecentFilesClearOperationHasAnUnambiguousApiName` | クラス単位テスト | ProjectLifecycleBoundary |  |  |
| `ProjectLifecycleBoundaryTests` | `LifecycleExposesSnapshotWithoutOwningFileFormatConversion` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ProjectLifecycleServiceTests` | `CreateNewLoadsWorkspaceAndSessionWithoutUnsavedChange` | クラス単位テスト | ProjectLifecycleService |  |  |
| `ProjectLifecycleServiceTests` | `SaveAndOpenRoundTripOwnsSessionAndRecentFiles` | クラス単位テスト | ProjectLifecycleService |  |  |
| `ProjectLifecycleServiceTests` | `CreateSnapshotBuildsCurrentDocumentWithoutSavingOrChangingSession` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ProjectLifecycleServiceTests` | `CreateEmptyLoadsEmptyWorkspaceWithoutSampleData` | クラス単位テスト | ProjectLifecycleService |  |  |
| `ProjectLifecycleServiceTests` | `ClearRecentFilesSynchronizesSessionWithRecentFilesService` | クラス単位テスト | ProjectLifecycleService |  |  |
| `ProjectLifecycleServiceTests` | `SaveDoesNotShowRecentFileWhenRecentHistoryPersistenceFails` | クラス単位テスト | ProjectLifecycleService |  |  |
| `ProjectSessionViewModelTests` | `StartOwnsProjectIdentityAndResetsUnsavedState` | クラス単位テスト | ProjectSessionViewModel |  |  |
| `ProjectSessionViewModelTests` | `UnsavedWarningUsesInjectedClock` | クラス単位テスト | ProjectSessionViewModel |  |  |
| `ProjectSessionViewModelTests` | `ReplaceRecentFilesUpdatesOwnedCollection` | クラス単位テスト | ProjectSessionViewModel |  |  |
| `PromptStandardContractTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `PromptStandardContractTests` | `Guideline_ContainsPromptStandardContract` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `PromptStandardContractTests` | `Guideline_StandardContract_ContainsSchemaVersioningPolicyReference` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `PromptStandardContractTests` | `Guideline_StandardContract_ContainsErrorLogPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `PromptStandardContractTests` | `Guideline_StandardContract_ContainsCiGreenDoneCriteria` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `PromptStandardContractTests` | `Guideline_ContainsFuturePromptTemplate` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `PromptStandardContractTests` | `Guideline_FuturePromptTemplate_ContainsScopeSection` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `PromptStandardContractTests` | `Guideline_FuturePromptTemplate_ContainsVersionSection` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `PromptStandardContractTests` | `ReleaseNotes_Contains_V2108` | ドキュメント / ルール固定テスト | docs / version / schema rule | V2108 |  |
| `RecentFilesServiceTests` | `Add_DuplicatePath_MovedToFront` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Add_ExceedsMaxFive_ListTrimmed` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Add_NewPath_AppearsAtFront` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Load_EmptyState_ReturnsEmpty` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Add_WriteFailure_ReturnsPersistedListInsteadOfUnwrittenUpdate` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Add_ReplaceFailure_PreservesPersistedListAndRemovesTemporaryFile` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Clear_DeleteFailure_ReturnsPersistedList` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Remove_ExistingPath_ReturnsAndPersistsUpdatedList` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Remove_WriteFailure_ReturnsPersistedList` | クラス単位テスト | RecentFilesService |  |  |
| `RecentFilesServiceTests` | `Clear_RemovesAllRecentFiles` | クラス単位テスト | RecentFilesService |  |  |
| `SaveAllCommandTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `SaveAllCommandTests` | `Backlog_SH20_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | SH-20 |  |
| `SaveAllCommandTests` | `ReleaseNotes_Contains_V2104` | ドキュメント / ルール固定テスト | docs / version / schema rule | V2104 |  |
| `SaveAllCommandTests` | `ReleaseNotes_V2104_MentionsSH20` | ドキュメント / ルール固定テスト | docs / version / schema rule | SH-20, V2104 |  |
| `SavedWorkspaceStateUpdaterTests` | `TryCreate_SaveSuccess_UpdatesFilePathForWorkspace` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SavedWorkspaceStateUpdaterTests` | `TryCreate_SaveSuccess_ClearsDirtyStateAndUpdatesTabTitle` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SavedWorkspaceStateUpdaterTests` | `TryCreate_SaveSuccess_ProvidesRecentFilePath` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SavedWorkspaceStateUpdaterTests` | `ApplyToSession_SaveSuccess_UpdatesSessionForNextSessionEntry` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SavedWorkspaceStateUpdaterTests` | `TryCreate_SaveFailureNotCalled_LeavesExistingStateUnchanged` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SavedWorkspaceStateUpdaterTests` | `TryCreate_TempTab_IsExcluded` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SavedWorkspaceStateUpdaterTests` | `TryCreate_MismatchedWorkspaceExtension_IsRejected` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SavedWorkspaceStateUpdaterTests` | `SessionFormat_RemainsFilePathsAndActiveFilePathOnly_AfterSaveStateUpdate` | クラス単位テスト | SavedWorkspaceStateUpdater |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_FileExists` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_NoteNestFormat` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_IdeaNestFormat` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_ChatNestFormat` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_TempNestJson` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_SessionJson` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_MigrationPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_BackupPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_TestPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `SchemaVersioningPolicy_Contains_VersioningRule` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SchemaVersioningPolicyTests` | `Backlog_FM1_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | FM-1, M1 |  |
| `SchemaVersioningPolicyTests` | `Backlog_FM1_ReferencesSchemaVersioningPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule | FM-1, M1 |  |
| `SchemaVersioningPolicyTests` | `ReleaseNotes_Contains_V2102` | ドキュメント / ルール固定テスト | docs / version / schema rule | V2102 |  |
| `SchemaVersioningPolicyTests` | `NoteNestSchemaVersion_Remains_1_4_1` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SessionNestGuardNestPolicyTests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 |  | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `SessionNestGuardNestPolicyTests` | `PolicyDocument_Exists` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SessionNestGuardNestPolicyTests` | `PolicyDocument_DescribesSessionNestResponsibilities` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SessionNestGuardNestPolicyTests` | `PolicyDocument_DescribesGuardNestResponsibilities` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SessionNestGuardNestPolicyTests` | `PolicyDocument_ReferencesSchemaVersioningPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SessionNestGuardNestPolicyTests` | `Backlog_TD24_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-24 |  |
| `SessionNestGuardNestPolicyTests` | `ReleaseNotes_Contains_V2_10_11` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `SessionNestTD25Tests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | TD-25 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `SessionNestTD25Tests` | `SessionJson_HasOnlyFilePathsAndActiveFilePath` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `SessionState_RoundTrip_PreservesFilePathsAndActiveFilePath` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `TempNest_IsExcludedFromSession_ByKind` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `TempNest_IsExcludedFromSession_WhenMixedWithSavedTabs` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `TempNest_WhenActiveTab_ActiveFilePathIsNull` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `TempNest_IsNotRestorable_ByRestoreTarget` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `DetachedState_IsNotPresent_InSessionJson` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `DetachedTab_FilePathIsSaved_ToSession` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `DetachedTab_RestoresAsNormal_OnNextLaunch` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `MultipleDetachedTabs_AllFilePathsSaved_NoFlagLeak` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `CreateRestoreTargets_FiltersUnknownExtensions_Silently` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `UntitledTab_IsExcludedFromSession` | シナリオ / 回帰テスト | SessionNestTD25 | TD-25 | 課題番号 / version ベースのテストクラス名。 |
| `SessionNestTD25Tests` | `Backlog_TD25_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-25 |  |
| `SessionNestTD25Tests` | `ReleaseNotes_Contains_V2_10_12` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-25 |  |
| `SessionTabMapperTests` | `TryCreateSessionEntry_NoteNestTab_UsesWorkspaceFilePath` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `TryCreateSessionEntry_IdeaNestTab_UsesWorkspaceFilePath` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `TryCreateSessionEntry_ChatNestTab_UsesWorkspaceFilePath` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `TryCreateSessionEntry_TempTab_IsExcluded` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `CreateSessionState_ExcludesTempAndUntitledTabs_AndKeepsActiveSavedTab` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `CreateSessionState_WhenActiveTabIsTemp_SetsActiveFilePathNull` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `TryCreateRestoreTarget_SupportedExtension_ReturnsWorkspaceKind` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `TryCreateRestoreTarget_UnknownExtension_IsSafeFalse` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `TryCreateRestoreTarget_MissingFile_IsSkippedLikeExistingRestoreFlow` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `CreateRestoreTargets_FiltersInvalidEntriesWithoutChangingOrder` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `CreateSessionState_SessionJsonShape_RemainsFilePathsAndActiveFilePathOnly` | クラス単位テスト | SessionTabMapper |  |  |
| `SessionTabMapperTests` | `CloseConfirmationService_SaveFailureStillCancelsCloseFlow` | クラス単位テスト | SessionTabMapper |  |  |
| `ShellUxTests` | `NestSuiteWindowPositionGuard_TypeExists` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `NestSuiteWindowPositionGuard_IsOnScreen_ReturnsTrue_ForVisiblePosition` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `NestSuiteWindowPositionGuard_IsOnScreen_ReturnsFalse_ForNaN` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `NestSuiteWindowPositionGuard_IsOnScreen_ReturnsFalse_WhenTooFarRight` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `UiSettings_HasNestSuiteWindowLeft_Property` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `UiSettings_HasNestSuiteWindowTop_Property` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `NestSuiteShellWindow_HasShowStatusNotificationMethod` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `NestSuiteShellWindow_HasRestoreFocusToWorkspaceMethod` | 機能単位テスト | ShellUx |  |  |
| `ShellUxTests` | `NestSuiteShellWindow_HasTabListButtonClickHandler` | 機能単位テスト | ShellUx |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithNestSuiteFlag_ReturnsTrue` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithNestSuiteFlagMixedCase_ReturnsTrue` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithNestSuiteFlagUpperCase_ReturnsTrue` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithNoArgs_ReturnsFalse` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithFilePathOnly_ReturnsFalse` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithOtherFlag_ReturnsFalse` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithNestSuitePlusFilePath_ReturnsTrue` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithFilePath_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithNestSuitePlusFilePath_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithFilePathBeforeFlag_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithOnlyFlag_ReturnsNull` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithNoArgs_ReturnsNull` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithUnsupportedExtension_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithNestSuitePlusChatNestFilePath_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithNestSuitePlusChatNestFilePath_ReturnsTrue` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithNestSuitePlusIdeaNestFilePath_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `IsNestSuiteMode_WithNestSuitePlusIdeaNestFilePath_ReturnsTrue` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithNoArgsDefaultNestSuite_ReturnsNull` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithNotenestOnly_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithChatnestOnly_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithIdeanestOnly_ReturnsPath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithUnknownFlagAndFilePath_ReturnsFilePath` | クラス単位テスト | StartupArgParser |  |  |
| `StartupArgParserTests` | `GetFilePath_WithUnknownFlagOnly_ReturnsNull` | クラス単位テスト | StartupArgParser |  |  |
| `TabPartialSplitTests` | `TabResponsibilityPartialFiles_Exist` | クラス単位テスト | TabPartialSplit |  |  |
| `TabPartialSplitTests` | `TabsOverviewFile_IsKeptSmallAfterSplit` | クラス単位テスト | TabPartialSplit |  |  |
| `TaskBoardViewModelTests` | `MoveTaskAndBuildModelPreserveGroupOwnership` | クラス単位テスト | TaskBoardViewModel |  |  |
| `TaskBoardViewModelTests` | `DirectPersistentTaskChangesRaiseChanged` | クラス単位テスト | TaskBoardViewModel |  |  |
| `TaskBoardViewModelTests` | `ClearLinksRaisesChangedOnlyWhenPersistentDataChanges` | クラス単位テスト | TaskBoardViewModel |  |  |
| `TaskGroupViewModelTests` | `AddTask_IncreasesCount` | クラス単位テスト | TaskGroupViewModel |  |  |
| `TaskGroupViewModelTests` | `RemoveTask_ReturnsTrue_AndDecreasesCount` | クラス単位テスト | TaskGroupViewModel |  |  |
| `TaskGroupViewModelTests` | `RemoveTask_AbsentTask_ReturnsFalse` | クラス単位テスト | TaskGroupViewModel |  |  |
| `TaskGroupViewModelTests` | `InsertTask_PlacedAtCorrectIndex` | クラス単位テスト | TaskGroupViewModel |  |  |
| `TaskGroupViewModelTests` | `IncompleteTasks_And_CompletedTasks_CoverAllTasks` | クラス単位テスト | TaskGroupViewModel |  |  |
| `TaskGroupViewModelTests` | `IncompleteTasks_ExcludesCompleted` | クラス単位テスト | TaskGroupViewModel |  |  |
| `TaskGroupViewModelTests` | `CountText_ReflectsIncompleteSlashTotal` | クラス単位テスト | TaskGroupViewModel |  |  |
| `TempNestTests` | `CreateTempTab_HasFixedId` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CreateTempTab_WorkspaceKind_IsTemp` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CreateTempTab_CanClose_IsFalse` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CreateTempTab_FilePath_IsNull` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CreateTempTab_IsModified_IsFalse` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CreateTempTab_DisplayName_IsTemp` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `GetExtension_Temp_ThrowsArgumentOutOfRangeException` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `TryGetKind_TempExtension_DoesNotExist` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `TempNestSlot_DefaultTitle_IsEmpty` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `TempNestSlot_DefaultBody_IsEmpty` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `Load_AlwaysReturnsFourSlots` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `Load_AllSlotsAreNonNull` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `ClearCommand_BothTitleAndBodyEmpty_IsDisabled` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `ClearCommand_TitleNonEmpty_IsEnabled` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `ClearCommand_BodyNonEmpty_IsEnabled` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `ClearCommand_BothTitleAndBodyNonEmpty_IsEnabled` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CopyBodyCommand_EmptyBody_IsDisabled` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CopyBodyCommand_NonEmptyBody_IsEnabled` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `CopyBodyCommand_WhitespaceOnlyBody_IsEnabled` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `ToSlot_PreservesTitle` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `ToSlot_PreservesBody` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `LoadFromSlot_RestoresTitleAndBody` | クラス単位テスト | TempNest |  |  |
| `TempNestTests` | `ToSlot_LoadFromSlot_RoundTrip_PreservesContent` | クラス単位テスト | TempNest |  |  |
| `TestClassificationAnalysisTests` | `AnalysisDocument_Exists` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `TestClassificationAnalysisTests` | `AnalysisDocument_ContainsFiveClassifications` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `TestClassificationAnalysisTests` | `AnalysisDocument_ContainsClassificationTable` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `TestClassificationAnalysisTests` | `DevelopmentGuidelines_ContainTestClassNamingPolicy` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `TestClassificationAnalysisTests` | `Backlog_ContainsTD28Completion` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-28 |  |
| `TestClassificationAnalysisTests` | `ReleaseNotes_ContainV21014` | ドキュメント / ルール固定テスト | docs / version / schema rule | V21014 |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_Line0_Returns0` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_Line1_ReturnsAfterFirstNewline` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_Line2_ReturnsAfterSecondNewline` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_BeyondLastLine_ReturnsMinusOne` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_EmptyText_Line0_Returns0` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_EmptyText_Line1_ReturnsMinusOne` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_TrailingNewline_LastEmptyLine` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_SingleNewline_Line1_Returns1` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `TextBoxLineLayoutAdapterTests` | `LogicalLineStartChar_MultipleLines_AllCorrect` | クラス単位テスト | TextBoxLineLayoutAdapter |  |  |
| `ThemeSettingsTests` | `UiSettings_DefaultTheme_IsLight` | 機能単位テスト | ThemeSettings |  |  |
| `ThemeSettingsTests` | `UiSettings_DarkTheme_RoundTripsThroughJson` | 機能単位テスト | ThemeSettings |  |  |
| `ThemeSettingsTests` | `UiSettings_InvalidTheme_FallsBackToLight` | 機能単位テスト | ThemeSettings |  |  |
| `ThemeSettingsTests` | `ThemeDictionary_ContainsRequiredBrushes` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ThemeSettingsTests` | `NoteNestSave_DoesNotContainThemeSetting` | 機能単位テスト | ThemeSettings |  |  |
| `ThemeSettingsTests` | `ChatNestSave_DoesNotContainThemeSetting` | 機能単位テスト | ThemeSettings |  |  |
| `ThemeSettingsTests` | `IdeaNestSave_DoesNotContainThemeSetting` | 機能単位テスト | ThemeSettings |  |  |
| `ThemeSettingsTests` | `TempNestJsonVersion_RemainsOne` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ThreeToolsMultiTabRegressionTests` | `SessionManager_CanHoldAllThreeKindsSimultaneously` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `SessionManager_FilterByKind_EachKindHasExactlyOne` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `RemoveNoteNestSession_DoesNotAffectChatNestAndIdeaNestSessions` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `RemoveChatNestSession_DoesNotAffectNoteNestAndIdeaNestSessions` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `RemoveIdeaNestSession_DoesNotAffectNoteNestAndChatNestSessions` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `FilePath_IsIndependent_AcrossAllThreeKinds` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `IsModified_IsIndependent_AcrossAllThreeKinds` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `IsModified_SetOnChatNest_DoesNotAffectNoteNestOrIdeaNest` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `WorkspaceViewModel_CorrectType_PerKind` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `WorkspaceViewModels_AreNotShared_AcrossKinds` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `SessionManager_SixTabs_TwoPerTool_CountsCorrectly` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `SessionManager_SixTabs_RemoveAll_LeavesZero` | シナリオ / 回帰テスト | ThreeToolsMultiTabRegression |  |  |
| `ThreeToolsMultiTabRegressionTests` | `OpenFilePolicy_SameFile_IsDuplicate_WithinSameKind` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ThreeToolsMultiTabRegressionTests` | `OpenFilePolicy_NullPath_IsNeverDuplicate_AcrossAllTools` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `ThreeToolsMultiTabRegressionTests` | `OpenFilePolicy_CaseInsensitive_Works_AcrossAllExtensions` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `UiSmokeTD23Tests` | `NoteNestSchemaVersion_Remains_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | TD-23 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `UiSmokeTD23Tests` | `Backlog_TD23_IsMarkedComplete` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-23 |  |
| `UiSmokeTD23Tests` | `ReleaseNotes_Contains_V2_10_10` | ドキュメント / ルール固定テスト | docs / version / schema rule | TD-23 |  |
| `UiSmokeTD23Tests` | `SmokeProgram_Exists` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_HasWaitForMainWindowHelper` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_HasWaitForElementByAutomationIdHelper` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_HasCheckRequiredElementsHelper` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_HasClickElementByPointHelper` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_CoversNoteNestElements` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_CoversIdeaNestElements` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_CoversChatNestElements` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_CoversToolMenuIds` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `UiSmokeTD23Tests` | `SmokeProgram_CoversTempNestElements` | シナリオ / 回帰テスト | UiSmokeTD23 | TD-23 | 課題番号 / version ベースのテストクラス名。 |
| `V140RegressionTests` | `SaveAndReloadPreservesNotesTasksLinksSettingsSelectionAndSchema` | ドキュメント / ルール固定テスト | docs / version / schema rule | V140 |  |
| `V140RegressionTests` | `OverwriteSaveCreatesBackupAndClearsUnsavedState` | シナリオ / 回帰テスト | V140Regression | V140 | 課題番号 / version ベースのテストクラス名。 |
| `V140RegressionTests` | `SelectionAndViewSettingsDoNotMarkModifiedButEditsAndPersistentSettingsDo` | シナリオ / 回帰テスト | V140Regression | V140 | 課題番号 / version ベースのテストクラス名。 |
| `V140RegressionTests` | `DeletingRelatedNoteThroughFacadeClearsTaskLink` | シナリオ / 回帰テスト | V140Regression | V140 | 課題番号 / version ベースのテストクラス名。 |
| `V141FeatureTests` | `NoteTimestampsUpdateAndRoundTrip` | クラス単位テスト | V141Feature | V141 |  |
| `V141FeatureTests` | `LegacyNoteWithoutTimestampsLoadsWithDefaults` | クラス単位テスト | V141Feature | V141 |  |
| `V141FeatureTests` | `UnifiedExportSupportsTargetsFormatsTasksAndMarkers` | クラス単位テスト | V141Feature | V141 |  |
| `V141FeatureTests` | `AutoSaveOnlySavesModifiedExistingProject` | クラス単位テスト | V141Feature | V141 |  |
| `V141FeatureTests` | `ProjectInfoContainsCurrentCountsAndSaveState` | ドキュメント / ルール固定テスト | docs / version / schema rule | V141 |  |
| `V146RegressionTests` | `NewProject_StartsUnmodified` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `SaveLoad_RoundTrip_PreservesNotesTasksAndSchema` | ドキュメント / ルール固定テスト | docs / version / schema rule | V146 |  |
| `V146RegressionTests` | `Save_CreatesBakFile` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `Load_BrokenJson_Throws` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `AutoSave_DoesNotSave_WhenFilePathIsNull` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `AutoSave_DoesNotSave_WhenNotModified` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `AutoSave_Saves_WhenModifiedAndPathSet` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `RecentFiles_AddedOnSave` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `RecentFiles_ClearRemovesAll` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `RecentFiles_AtomicWrite_NoPermanentTempFile` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `NoteTimestamps_SetOnCreate` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `NoteTimestamps_CreatedAt_NotChangedOnEdit` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `NoteTimestamps_UpdatedAt_ChangesOnContentEdit` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `LegacyNote_WithoutTimestamps_LoadsWithDefaults` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `SchemaVersion_Is_1_4_1` | 不要テスト候補 | NoteNest schema 固定 | V146 | schema 固定は集約済みのため重複候補。削除判断は別途。 |
| `V146RegressionTests` | `SavedFile_ContainsSchemaVersion_1_4_1` | ドキュメント / ルール固定テスト | docs / version / schema rule | V146 |  |
| `V146RegressionTests` | `IsModified_FalseAfterSave` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `Export_Txt_WritesUtf8` | シナリオ / 回帰テスト | V146Regression | V146 | 課題番号 / version ベースのテストクラス名。 |
| `V146RegressionTests` | `Export_Markdown_ContainsHeadings` | ドキュメント / ルール固定テスト | docs / version / schema rule | V146 |  |
| `V146RegressionTests` | `Export_Html_ContainsHtmlTags` | ドキュメント / ルール固定テスト | docs / version / schema rule | V146 |  |
| `WorkspaceChangeCoordinatorTests` | `SelectionChangeIsNotClassifiedAsDataChange` | クラス単位テスト | WorkspaceChangeCoordinator |  |  |
| `WorkspaceChangeCoordinatorTests` | `DocumentLoadAndSelectionAreNotClassifiedAsDataChanges` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |
| `WorkspaceChangeCoordinatorTests` | `EditorContentRoutesToNoteAndRefreshesMarkers` | クラス単位テスト | WorkspaceChangeCoordinator |  |  |
| `WorkspaceChangeCoordinatorTests` | `EditorRelatedNoteRoutesToEditingTask` | クラス単位テスト | WorkspaceChangeCoordinator |  |  |
| `WorkspaceChangeCoordinatorTests` | `PersistentEditorSettingsAreDataChangesButViewSettingsAreNot` | クラス単位テスト | WorkspaceChangeCoordinator |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_FileNotFoundException_ReturnsFileNotFoundMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_DirectoryNotFoundException_ReturnsFileNotFoundMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_JsonException_ReturnsFormatMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_IOException_ReturnsIoMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_UnauthorizedAccessException_ReturnsAccessMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_SecurityException_ReturnsAccessMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_PathTooLongException_ReturnsPathMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForLoad_UnknownException_ReturnsFallbackAndNotEmpty` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForSave_IOException_ReturnsIoMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForSave_UnauthorizedAccessException_ReturnsAccessMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForSave_JsonException_ReturnsWriteErrorMessage` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `ForSave_UnknownException_ReturnsFallbackAndNotEmpty` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `IsSameFile_CaseInsensitive_ReturnsTrue` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `IsSameFile_NullLeft_ReturnsFalse` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `IsSameFile_NullRight_ReturnsFalse` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `IsSameFile_BothNull_ReturnsFalse` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `IsSameFile_DifferentFiles_ReturnsFalse` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceFileOperationHelperTests` | `IsSameFile_IdenticalPaths_ReturnsTrue` | クラス単位テスト | WorkspaceFileOperationHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_HasSyncTabModifiedStateMethod` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_HasTryActivateExistingTabMethod` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_HasLoadWorkspaceFileAtMethod` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_SyncChatNestTabForViewModel_StillExists` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_SyncIdeaNestTabForViewModel_StillExists` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_TryRestoreSession_StillExists` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_LoadInitialNoteNestFile_StillExists` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_LoadInitialChatNestFile_StillExists` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `NestSuiteShellWindow_LoadInitialIdeaNestFile_StillExists` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `ErrorLogService_HasNoInfoMethod` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `ErrorLogService_HasNoWarningMethod` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceSessionSyncHelperTests` | `ErrorLogService_HasLogMethod_ErrorOnly` | クラス単位テスト | WorkspaceSessionSyncHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_HasConfirmTabCloseMethod` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_HasNewWorkspaceSessionMethod` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_ConfirmAndResetNoteNest_StillExists` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_ConfirmAndResetChatNest_StillExists` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_ConfirmAndResetIdeaNest_StillExists` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_NewNoteNestSession_StillExists` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_NewChatNestSession_StillExists` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceTabHelperTests` | `NestSuiteShellWindow_NewIdeaNestSession_StillExists` | クラス単位テスト | WorkspaceTabHelper |  |  |
| `WorkspaceViewModelTests` | `NoteWorkspace_OwnsNoteCollectionAndPreventsDuplicateNames` | クラス単位テスト | WorkspaceViewModel |  |  |
| `WorkspaceViewModelTests` | `NoteWorkspace_RaisesChangedForCollectionTitleAndContentChanges` | クラス単位テスト | WorkspaceViewModel |  |  |
| `WorkspaceViewModelTests` | `NoteWorkspace_LoadDoesNotRaiseChanged` | クラス単位テスト | WorkspaceViewModel |  |  |
| `WorkspaceViewModelTests` | `MainViewModel_DirectNoteWorkspaceChangeMarksProjectModifiedAndRefreshesMarkers` | クラス単位テスト | WorkspaceViewModel |  |  |
| `WorkspaceViewModelTests` | `TaskBoard_BuildModelPreservesGroupOwnership` | クラス単位テスト | WorkspaceViewModel |  |  |
| `WorkspaceViewModelTests` | `MarkerPanel_OwnsFilteringAndSummary` | クラス単位テスト | WorkspaceViewModel |  |  |
| `WorkspaceViewModelTests` | `MainViewModel_ExposesIndependentWorkspaceOwners` | クラス単位テスト | WorkspaceViewModel |  |  |
| `WorkspaceViewRegressionTests` | `WorkspaceView_LayoutPropertiesRemainAvailable` | シナリオ / 回帰テスト | WorkspaceViewRegression |  |  |
| `WorkspaceViewRegressionTests` | `WorkspaceView_PublicMethodsRemainAvailable` | シナリオ / 回帰テスト | WorkspaceViewRegression |  |  |
| `WorkspaceViewRegressionTests` | `WorkspaceView_InternalDelegatesRemainAvailable` | シナリオ / 回帰テスト | WorkspaceViewRegression |  |  |
| `WorkspaceViewRegressionTests` | `WorkspaceView_DialogHostPropertyIsPublicReadWrite` | シナリオ / 回帰テスト | WorkspaceViewRegression |  |  |
| `WorkspaceViewRegressionTests` | `WorkspaceView_IsUserControlNotWindow` | シナリオ / 回帰テスト | WorkspaceViewRegression |  |  |
| `WorkspaceViewRegressionTests` | `IWorkspaceDialogHost_ContractMembersArePresent` | ドキュメント / ルール固定テスト | docs / version / schema rule |  |  |

## 4. テストクラス命名の問題点

### 課題番号ベースのテストクラス名

- `ChatNestCH13DragReorderTests`
- `ChatNestCH8CH14Tests`
- `ChatNestCH9ExportTests`
- `GuardNestTD26Tests`
- `SessionNestTD25Tests`
- `UiSmokeTD23Tests`

### versionベースのテストクラス名

- `LightImprovementsV2103Tests`
- `V140RegressionTests`
- `V141FeatureTests`
- `V146RegressionTests`

### 複数課題をまとめたテストクラス名

- `ChatNestCH8CH14Tests` は CH-8 と CH-14 が混在する。
- `LightImprovementsV2103Tests` は TN-2 / L14 / L15 と version が混在する。

### 対象クラスや対象機能が名前から分かりにくいテストクラス

- `NestSuiteShellTests` は対象範囲が広く、Shell 全体の機能単位・シナリオ単位が混在する。
- `LightImprovementsV2103Tests` は実装時期ベースで、保守時に対象機能を探しにくい。
- `UiSmokeTD23Tests` は課題番号が主で、Smoke 対象の Workspace 範囲が名前から読み取りにくい。

## 5. 今後の推奨方針

- 単体テストは原則「対象クラス名 + Tests」とする。
- 単一クラスに閉じない場合は「対象機能名 + Tests」とする。
- 複数機能をまたぐ事故防止は「Scenario / Regression / Smoke」を明示する。
- backlog ID / version番号はテストクラス名に入れない。
- backlog IDはコメントまたは Trait に残す。
- 既存テストは一括リネームせず、触るタイミングで段階的に整理する。

## 6. 段階的整理案

### Step 1
- ApplicationVersionテスト集約済みの状態を維持する。
- 新規テストクラス命名規約を追加する。
- v2.10.14 ではこの分析文書作成までとする。

### Step 2
- 課題番号 / versionベースのテストクラスを代表例からリネーム候補化する。

### Step 3
- 対象クラス単位に寄せられるものを移動・統合する。

### Step 4
- 不要テスト候補を個別に削除判断する。
