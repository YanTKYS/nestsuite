using System;
using System.Windows;
using Microsoft.Win32;

namespace NestSuite.IdeaNest.ViewModels;

/// <summary>Centralizes the WPF host-dependent operations used by the IdeaNest workspace.</summary>
// ID-10: テストから実クリップボード/実SaveFileDialogに依存せず差し替えられるよう、sealedを外しvirtualにしている。
public class IdeaNestWorkspaceUiService
{
    private Func<Window?> _ownerResolver = () => Application.Current?.MainWindow;

    public Window? Owner => _ownerResolver();

    public void SetOwnerResolver(Func<Window?> resolver) =>
        _ownerResolver = resolver ?? (() => Application.Current?.MainWindow);

    public virtual string? GetClipboardText()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; }
    }

    public virtual void SetClipboardText(string text) => Clipboard.SetText(text);

    /// <summary>ID-10: Markdownファイルの保存先を選択するダイアログ。キャンセル時は null。</summary>
    public virtual string? ShowSaveMarkdownDialog(string defaultFileName)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Markdownとして保存",
            Filter = "Markdownファイル (*.md)|*.md|すべてのファイル (*.*)|*.*",
            DefaultExt = ".md",
            FileName = defaultFileName,
        };
        return dlg.ShowDialog(Owner) == true ? dlg.FileName : null;
    }

    public virtual void ShowInformation(string message) =>
        MessageBox.Show(Owner, message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Information);

    public virtual void ShowWarning(string message) =>
        MessageBox.Show(Owner, message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Warning);

    public virtual void ShowError(string message) =>
        MessageBox.Show(Owner, message, "IdeaNest", MessageBoxButton.OK, MessageBoxImage.Error);
}
