using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NestSuite.IdeaNest.ViewModels;

namespace NestSuite.IdeaNest.Views;

public partial class PreviewIdeaWindow : Window
{
    private readonly IReadOnlyList<IdeaCardViewModel> _cards;
    private readonly Action<IdeaCardViewModel> _onCommitEdit;
    private int _currentIndex;
    private EditIdeaViewModel _editVm = null!;
    private bool _hasEdits;

    private IdeaCardViewModel CurrentCard => _cards[_currentIndex];

    public PreviewIdeaWindow(
        IReadOnlyList<IdeaCardViewModel> cards,
        int initialIndex,
        Action<IdeaCardViewModel> onCommitEdit)
    {
        InitializeComponent();
        _cards = cards;
        _onCommitEdit = onCommitEdit;
        _currentIndex = initialIndex;
        LoadCard();
        UpdateButtonStates();
        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnWindowClosed;
    }

    private void LoadCard()
    {
        _hasEdits = false;
        _editVm = new EditIdeaViewModel(CurrentCard.Model);
        _editVm.PropertyChanged += OnEditVmPropertyChanged;
        DataContext = _editVm;
    }

    private void CommitCurrentEdit()
    {
        if (!_hasEdits) return;
        _editVm.ApplyTo(CurrentCard.Model);
        _onCommitEdit(CurrentCard);
    }

    private void NavigateTo(int index)
    {
        if (index < 0 || index >= _cards.Count) return;
        CommitCurrentEdit();
        _editVm.PropertyChanged -= OnEditVmPropertyChanged;
        _currentIndex = index;
        LoadCard();
        UpdateButtonStates();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.S when (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0:
                CommitCurrentEdit();
                e.Handled = true;
                break;
            case Key.Left:
                if (FocusManager.GetFocusedElement(this) is System.Windows.Controls.TextBox) break;
                NavigateTo(_currentIndex - 1);
                e.Handled = true;
                break;
            case Key.Right:
                if (FocusManager.GetFocusedElement(this) is System.Windows.Controls.TextBox) break;
                NavigateTo(_currentIndex + 1);
                e.Handled = true;
                break;
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        CommitCurrentEdit();
        _editVm.PropertyChanged -= OnEditVmPropertyChanged;
    }

    private void OnEditVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditIdeaViewModel.IsPinned) or nameof(EditIdeaViewModel.IsArchived))
            UpdateButtonStates();
        if (e.PropertyName is not nameof(EditIdeaViewModel.BackgroundBrush))
            _hasEdits = true;
    }

    private void UpdateButtonStates()
    {
        PinButton.Content = _editVm.IsPinned ? "📌 ピン留め解除" : "📌 ピン留め";
        ArchiveButton.Content = _editVm.IsArchived ? "📤 アーカイブ解除" : "📥 アーカイブ";
        PrevButton.IsEnabled = _currentIndex > 0;
        NextButton.IsEnabled = _currentIndex < _cards.Count - 1;
    }

    private void OnPrevClick(object sender, RoutedEventArgs e) => NavigateTo(_currentIndex - 1);
    private void OnNextClick(object sender, RoutedEventArgs e) => NavigateTo(_currentIndex + 1);

    private void OnTogglePinClick(object sender, RoutedEventArgs e)
    {
        _editVm.IsPinned = !_editVm.IsPinned;
    }

    private void OnToggleArchiveClick(object sender, RoutedEventArgs e)
    {
        _editVm.IsArchived = !_editVm.IsArchived;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
