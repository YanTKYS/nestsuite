using NoteNest.Models;

namespace NoteNest.Services;

public class SampleDataService
{
    public Project Create()
    {
        var note001 = new Note
        {
            Id = "note_001",
            Title = "要件定義",
            Content = "# プロジェクト要件定義\r\n\r\n## 概要\r\nこのプロジェクトは...\r\n\r\n[TODO] データベース設計の詳細を追記する\r\n\r\n## 目的\r\n- ユーザー体験の向上\r\n- 業務効率化\r\n- データ可視化\r\n\r\n[FIXME] パフォーマンス要件を明確にする必要がある\r\n\r\n## 機能要件\r\n1. ユーザー認証\r\n2. ダッシュボード表示\r\n3. レポート生成\r\n\r\n[NOTE] セキュリティ要件については別ドキュメントを参照"
        };

        return new Project
        {
            ProjectId = "project_001",
            ProjectName = "業務改善プロジェクト",
            Notebooks = new()
            {
                new Notebook
                {
                    Id = "nb_001",
                    Title = "プロジェクト管理",
                    Notes = new()
                    {
                        note001,
                        new Note { Id = "note_002", Title = "進捗報告",
                            Content = "# 進捗報告\r\n\r\n## 今週の進捗\r\n- 要件定義完了\r\n- DB設計開始\r\n\r\n[TODO] 来週の計画を追記する" },
                        new Note { Id = "note_003", Title = "会議メモ",
                            Content = "# 会議メモ\r\n\r\n## 2024-01-15\r\n参加者: 田中、佐藤、鈴木\r\n\r\n議題:\r\n1. 要件確認\r\n2. スケジュール調整\r\n\r\n[NOTE] 次回は2週間後" }
                    }
                },
                new Notebook
                {
                    Id = "nb_002",
                    Title = "技術ドキュメント",
                    Notes = new()
                    {
                        new Note { Id = "note_004", Title = "アーキテクチャ設計",
                            Content = "# アーキテクチャ設計\r\n\r\n## システム構成\r\n- フロントエンド: WPF\r\n- データ保存: ローカルJSON\r\n\r\n[TODO] インフラ構成を追記する" },
                        new Note { Id = "note_005", Title = "API仕様",
                            Content = "# API仕様\r\n\r\n[FIXME] エンドポイントの命名規則を統一する\r\n\r\n## 認証API\r\n- POST /api/auth/login\r\n- POST /api/auth/logout" }
                    }
                },
                new Notebook
                {
                    Id = "nb_003",
                    Title = "個人メモ",
                    Notes = new()
                    {
                        new Note { Id = "note_006", Title = "アイデアメモ",
                            Content = "# アイデアメモ\r\n\r\n- ダッシュボードのカスタマイズ機能\r\n- エクスポート機能\r\n\r\n[NOTE] 次バージョンで検討する" },
                        new Note { Id = "note_007", Title = "読書メモ",
                            Content = "# 読書メモ\r\n\r\n参考書籍のメモ。\r\n\r\n- 設計パターン実践ガイド（読了）\r\n- Clean Architecture（読み中）\r\n- リファクタリング（積読）" }
                    }
                }
            },
            Tasks = new TaskCollection
            {
                Today = new()
                {
                    new NoteTask { Id = "task_001", Title = "要件定義書のレビュー", IsCompleted = true },
                    new NoteTask { Id = "task_002", Title = "APIエンドポイントの実装", IsCompleted = false },
                    new NoteTask { Id = "task_003", Title = "ユニットテストの作成", IsCompleted = false }
                },
                Week = new()
                {
                    new NoteTask { Id = "task_004", Title = "DB設計レビュー", IsCompleted = false },
                    new NoteTask { Id = "task_005", Title = "UI実装", IsCompleted = false },
                    new NoteTask { Id = "task_006", Title = "結合テスト", IsCompleted = false }
                },
                Backlog = new()
                {
                    new NoteTask { Id = "task_007", Title = "ドキュメント整備", IsCompleted = false },
                    new NoteTask { Id = "task_008", Title = "パフォーマンス改善", IsCompleted = false }
                }
            },
            Settings = new AppSettings
            {
                LastOpenedNoteId = "note_001",
                FontFamily = "Yu Gothic UI",
                FontSize = 14
            }
        };
    }
}
