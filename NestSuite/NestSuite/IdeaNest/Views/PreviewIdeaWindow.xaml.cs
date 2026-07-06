using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Services;

namespace NestSuite.IdeaNest.Views;

public partial class PreviewIdeaWindow : Window
{
    private readonly IReadOnlyList<IdeaCardViewModel> _cards;
    private readonly Action<IdeaCardViewModel> _onCommitEdit;
    private readonly Func<EditIdeaViewModel, IdeaCardViewModel?>? _onCommitAdd;
    private readonly bool _isNew;
    private readonly string _contentFontFamily;
    private int _currentIndex;
    private EditIdeaViewModel _editVm = null!;
    private bool _hasEdits;
    private IdeaCardViewModel? _addedCard;

    private IdeaCardViewModel CurrentCard => _cards[_currentIndex];

    // Existing-card mode
    public PreviewIdeaWindow(
        IReadOnlyList<IdeaCardViewModel> cards,
        int initialIndex,
        Action<IdeaCardViewModel> onCommitEdit,
        string contentFontFamily = "Yu Gothic UI")
    {
        InitializeComponent();
        ApplyWindowSettings();
        _cards = cards;
        _onCommitEdit = onCommitEdit;
        _currentIndex = initialIndex;
        _isNew = false;
        _contentFontFamily = contentFontFamily;
        LoadCard();
        UpdateButtonStates();
        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnWindowClosed;
    }

    // New-card mode
    public PreviewIdeaWindow(
        Func<EditIdeaViewModel, IdeaCardViewModel?> onCommitAdd,
        Action<IdeaCardViewModel> onCommitEdit,
        string contentFontFamily = "Yu Gothic UI")
    {
        InitializeComponent();
        ApplyWindowSettings();
        _cards = Array.Empty<IdeaCardViewModel>();
        _onCommitAdd = onCommitAdd;
        _onCommitEdit = onCommitEdit;
        _currentIndex = 0;
        _isNew = true;
        _contentFontFamily = contentFontFamily;
        LoadCard(new Idea());
        UpdateButtonStates();
        Title = "新規アイデア";
        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// L22: カード本文編集欄（BodyBox）に適用する Workspace 共通フォント種類。
    /// コンストラクタ引数（<see cref="_contentFontFamily"/>）で受け取り、カード切替のたびに
    /// 再生成される <see cref="_editVm"/> へ都度反映する。
    /// </summary>
    private void LoadCard(Idea? freshIdea = null)
    {
        _hasEdits = false;
        var idea = freshIdea ?? CurrentCard.Model;
        _editVm = new EditIdeaViewModel(idea, isExistingCard: freshIdea == null);
        _editVm.ContentFontFamily = _contentFontFamily;
        _editVm.PropertyChanged += OnEditVmPropertyChanged;
        DataContext = _editVm;
    }

    private void CommitCurrentEdit()
    {
        if (!_hasEdits) return;
        _hasEdits = false;

        if (_isNew)
        {
            if (_addedCard != null)
            {
                // Card was already created via Ctrl+S; commit further edits
                _editVm.ApplyTo(_addedCard.Model);
                _onCommitEdit(_addedCard);
            }
            else if (HasIdeaContent())
            {
                _addedCard = _onCommitAdd!(_editVm);
            }
            // else: no meaningful content → don't create empty card
        }
        else
        {
            _editVm.ApplyTo(CurrentCard.Model);
            _onCommitEdit(CurrentCard);
        }
    }

    private bool HasIdeaContent() =>
        !string.IsNullOrWhiteSpace(_editVm.Title)
        || !string.IsNullOrWhiteSpace(_editVm.Body)
        || !string.IsNullOrWhiteSpace(_editVm.TagsText);

    private void NavigateTo(int index)
    {
        if (_isNew) return;
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
        SaveWindowSettings();
        CommitCurrentEdit();
        _editVm.PropertyChanged -= OnEditVmPropertyChanged;
    }

    private void ApplyWindowSettings()
    {
        try
        {
            var s = new UiSettingsService().Load();
            double w = Math.Max(MinWidth, Math.Min(s.PreviewIdeaWindowWidth ?? Width, 2560));
            double h = Math.Max(MinHeight, Math.Min(s.PreviewIdeaWindowHeight ?? Height, 1600));
            Width = w;
            Height = h;
            if (s.PreviewIdeaWindowLeft.HasValue && s.PreviewIdeaWindowTop.HasValue)
            {
                double left = s.PreviewIdeaWindowLeft.Value;
                double top = s.PreviewIdeaWindowTop.Value;
                if (IsPositionOnScreen(left, top, w, h))
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = left;
                    Top = top;
                }
            }
        }
        catch { }
    }

    private void SaveWindowSettings()
    {
        try
        {
            double width, height, left, top;
            if (WindowState == WindowState.Normal)
            {
                width = Width; height = Height; left = Left; top = Top;
            }
            else
            {
                var rb = RestoreBounds;
                if (rb.Width <= 0 || rb.Height <= 0) return;
                width = rb.Width; height = rb.Height; left = rb.Left; top = rb.Top;
            }
            var svc = new UiSettingsService();
            var s = svc.Load();
            s.PreviewIdeaWindowWidth = width;
            s.PreviewIdeaWindowHeight = height;
            s.PreviewIdeaWindowLeft = left;
            s.PreviewIdeaWindowTop = top;
            svc.Save(s);
        }
        catch { }
    }

    private static bool IsPositionOnScreen(double left, double top, double width, double height)
    {
        double vLeft = SystemParameters.VirtualScreenLeft;
        double vTop = SystemParameters.VirtualScreenTop;
        double vRight = vLeft + SystemParameters.VirtualScreenWidth;
        double vBottom = vTop + SystemParameters.VirtualScreenHeight;
        return left + width > vLeft + 50 && left < vRight - 50 &&
               top >= vTop && top < vBottom - 50;
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
        if (_isNew)
        {
            PrevButton.IsEnabled = false;
            NextButton.IsEnabled = false;
        }
        else
        {
            PrevButton.IsEnabled = _currentIndex > 0;
            NextButton.IsEnabled = _currentIndex < _cards.Count - 1;
        }
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
