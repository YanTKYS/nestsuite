using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using ChatNest.Models;
using ChatNest.Services;
using Microsoft.Win32;

namespace ChatNest.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public const string AppVersion = "0.4.1";

        private string? _currentFilePath;
        private bool _isTopmost;

        private readonly SettingsService _settings = new();

        public ChatNestWorkspaceViewModel Workspace { get; } = new();

        public bool IsTopmost
        {
            get => _isTopmost;
            set { _isTopmost = value; OnPropertyChanged(); }
        }

        public string WindowTitle => _currentFilePath != null
            ? $"ChatNest - {Path.GetFileName(_currentFilePath)} - ver{AppVersion}"
            : $"ChatNest - ver{AppVersion}";

        public ICommand SaveCommand   { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand LoadCommand   { get; }
        public ICommand NewCommand    { get; }

        public MainViewModel()
        {
            SaveCommand   = new RelayCommand(Save);
            SaveAsCommand = new RelayCommand(SaveAs);
            LoadCommand   = new RelayCommand(Load);
            NewCommand    = new RelayCommand(New);

            Workspace.WorkspaceModified += (_, _) => SaveIfFileOpen();
        }

        // ── Unsaved-changes guard ────────────────────────────────────────────

        public bool ConfirmDiscardChanges()
        {
            if (Workspace.IsDirty)
            {
                var result = MessageBox.Show(
                    "未保存の変更があります。保存しますか？",
                    "未保存の変更",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) return false;
                if (result == MessageBoxResult.Yes && !SaveForConfirm()) return false;
            }

            if (!string.IsNullOrWhiteSpace(Workspace.InputText))
            {
                return MessageBox.Show(
                    "未投稿の入力があります。破棄しますか？",
                    "未投稿の入力",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question) == MessageBoxResult.OK;
            }

            return true;
        }

        private bool SaveForConfirm()
        {
            if (_currentFilePath != null)
                return TryWriteToFile(_currentFilePath, updatePath: false);

            var dlg = new SaveFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest",
                DefaultExt = ".chatnest",
                FileName = $"chatnest_{DateTime.Now:yyyyMMdd_HHmm}"
            };
            if (dlg.ShowDialog() != true) return false;
            return TryWriteToFile(dlg.FileName, updatePath: true);
        }

        // ── Change-time save ─────────────────────────────────────────────────

        private void SaveIfFileOpen()
        {
            if (_currentFilePath != null)
                TryWriteToFile(_currentFilePath, updatePath: false);
        }

        // ── New ──────────────────────────────────────────────────────────────

        private void New()
        {
            if (!ConfirmDiscardChanges()) return;
            Workspace.Clear();
            SetCurrentFile(null);
        }

        // ── 終了処理コピー (save-then-copy) ──────────────────────────────────

        public void ExecuteMarkdownCopyWithSave()
        {
            if (!SaveForCopy()) return;
            if (TrySetClipboard(Workspace.BuildMarkdownCopyText()))
                MessageBox.Show("Markdown形式でコピーしました。", "コピー完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExecuteIdeaNestCopyWithSave()
        {
            if (!SaveForCopy()) return;
            if (TrySetClipboard(Workspace.BuildIdeaNestCopyText()))
                MessageBox.Show("IdeaNest用形式でコピーしました。", "コピー完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool SaveForCopy()
        {
            if (_currentFilePath != null)
                return TryWriteToFile(_currentFilePath, updatePath: false);

            var dlg = new SaveFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest",
                DefaultExt = ".chatnest",
                FileName = $"chatnest_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (dlg.ShowDialog() != true)
            {
                MessageBox.Show(
                    "保存がキャンセルされたため、コピーは実行しませんでした。",
                    "コピー中止",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            return TryWriteToFile(dlg.FileName, updatePath: true);
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        private void Save()
        {
            if (_currentFilePath == null) { SaveAs(); return; }
            TryWriteToFile(_currentFilePath, updatePath: false);
        }

        private void SaveAs()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest",
                DefaultExt = ".chatnest",
                FileName = $"chatnest_{DateTime.Now:yyyyMMdd_HHmm}"
            };
            if (dlg.ShowDialog() == true)
                TryWriteToFile(dlg.FileName, updatePath: true);
        }

        private bool TryWriteToFile(string path, bool updatePath)
        {
            var tmpPath = path + ".tmp";
            try
            {
                var session = new ChatSessionData
                {
                    Version = AppVersion,
                    Messages = Workspace.Messages.Select(m => new MessageData
                    {
                        Id = m.Id,
                        Speaker = m.Speaker.ToString(),
                        Text = m.Text,
                        CreatedAt = m.CreatedAt
                    }).ToList()
                };
                var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(tmpPath, json, Encoding.UTF8);

                if (File.Exists(path))
                    File.Replace(tmpPath, path, path + ".bak");
                else
                    File.Move(tmpPath, path);

                Workspace.MarkSaved();

                if (updatePath)
                {
                    SetCurrentFile(path);
                    _settings.AddRecentFile(path);
                }
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
                MessageBox.Show($"保存に失敗しました。\n{ex.Message}", "保存エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void Load()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "ChatNest ファイル (*.chatnest)|*.chatnest"
            };
            if (dlg.ShowDialog() == true)
                LoadFromPath(dlg.FileName);
        }

        public void LoadFromPath(string path)
        {
            try
            {
                if (!ConfirmDiscardChanges()) return;

                var json = File.ReadAllText(path, Encoding.UTF8);
                var session = JsonSerializer.Deserialize<ChatSessionData>(json);
                if (session?.Messages == null) return;

                var messages = new List<Message>();
                int skipped = 0;
                foreach (var data in session.Messages)
                {
                    var speakerName = data.Speaker == "要約" ? "結論" : data.Speaker;
                    if (Enum.TryParse<Speaker>(speakerName, out var speaker))
                        messages.Add(new Message { Id = data.Id, Speaker = speaker, Text = data.Text, CreatedAt = data.CreatedAt });
                    else
                        skipped++;
                }

                Workspace.LoadMessages(messages);
                SetCurrentFile(path);
                _settings.AddRecentFile(path);

                if (skipped > 0)
                    MessageBox.Show(
                        $"{skipped} 件の発言を読み込めませんでした。未知の発言者が含まれているためスキップしました。",
                        "読み込み警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"読み込みに失敗しました。\n{ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Clipboard ────────────────────────────────────────────────────────

        private static bool TrySetClipboard(string text)
        {
            try { Clipboard.SetText(text); return true; }
            catch (Exception ex)
            {
                MessageBox.Show($"クリップボードへのコピーに失敗しました。再度お試しください。\n{ex.Message}",
                    "コピー失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetCurrentFile(string? path)
        {
            _currentFilePath = path;
            OnPropertyChanged(nameof(WindowTitle));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ChatSessionData
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = MainViewModel.AppVersion;

        [JsonPropertyName("messages")]
        public List<MessageData> Messages { get; set; } = new();
    }

    public class MessageData
    {
        [JsonPropertyName("id")]       public Guid Id { get; set; }
        [JsonPropertyName("speaker")]  public string Speaker { get; set; } = string.Empty;
        [JsonPropertyName("text")]     public string Text { get; set; } = string.Empty;
        [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; }
    }
}
