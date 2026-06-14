using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ChatNest.Models;

namespace ChatNest.ViewModels
{
    public class ChatNestWorkspaceViewModel : INotifyPropertyChanged
    {
        private string _inputText = string.Empty;
        private Speaker _selectedSpeaker = Speaker.自分;
        private bool _isDirty;

        private readonly RelayCommand _postCommand;

        public ObservableCollection<Message> Messages { get; } = new();
        public Speaker[] Speakers { get; } = Enum.GetValues<Speaker>();

        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                OnPropertyChanged();
                _postCommand.RaiseCanExecuteChanged();
            }
        }

        public Speaker SelectedSpeaker
        {
            get => _selectedSpeaker;
            set { _selectedSpeaker = value; OnPropertyChanged(); }
        }

        public bool IsDirty
        {
            get => _isDirty;
            private set { _isDirty = value; OnPropertyChanged(); }
        }

        public ICommand PostCommand => _postCommand;
        public ICommand DeleteMessageCommand { get; }

        public event EventHandler? WorkspaceModified;

        public ChatNestWorkspaceViewModel()
        {
            _postCommand = new RelayCommand(Post, () => !string.IsNullOrWhiteSpace(InputText));
            DeleteMessageCommand = new RelayCommand<Message>(DeleteMessage);
        }

        public void CycleSpeaker(bool forward)
        {
            var speakers = Enum.GetValues<Speaker>();
            int idx = Array.IndexOf(speakers, SelectedSpeaker);
            idx = forward
                ? (idx + 1) % speakers.Length
                : (idx - 1 + speakers.Length) % speakers.Length;
            SelectedSpeaker = speakers[idx];
        }

        public void MarkSaved() => IsDirty = false;

        public void Clear()
        {
            Messages.Clear();
            InputText = string.Empty;
            IsDirty = false;
        }

        public void LoadMessages(IEnumerable<Message> messages)
        {
            Messages.Clear();
            InputText = string.Empty;
            foreach (var m in messages)
                Messages.Add(m);
            IsDirty = false;
        }

        public string BuildMarkdownCopyText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ChatNest Log");
            sb.AppendLine();
            Speaker? prevSpeaker = null;
            foreach (var msg in Messages)
            {
                if (msg.Speaker != prevSpeaker)
                {
                    sb.AppendLine($"## {msg.Speaker}");
                    sb.AppendLine();
                    prevSpeaker = msg.Speaker;
                }
                sb.AppendLine(msg.Text);
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        public string BuildIdeaNestCopyText()
        {
            var sb = new StringBuilder();
            Speaker? prevSpeaker = null;
            foreach (var msg in Messages)
            {
                if (msg.Speaker != prevSpeaker)
                {
                    sb.AppendLine($"【{msg.Speaker}】");
                    prevSpeaker = msg.Speaker;
                }
                sb.AppendLine(msg.Text);
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        private void Post()
        {
            var text = InputText.Trim();
            if (string.IsNullOrEmpty(text)) return;
            Messages.Add(new Message { Speaker = SelectedSpeaker, Text = text });
            InputText = string.Empty;
            IsDirty = true;
            WorkspaceModified?.Invoke(this, EventArgs.Empty);
        }

        private void DeleteMessage(Message? message)
        {
            if (message == null) return;
            if (MessageBox.Show("この発言を削除しますか？", "削除の確認",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                Messages.Remove(message);
                IsDirty = true;
                WorkspaceModified?.Invoke(this, EventArgs.Empty);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
