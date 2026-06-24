using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NestSuite.Services;

namespace NestSuite.Dialogs;

public partial class FontSettingsDialog : Window
{
    public string SelectedFontFamily { get; private set; }
    public double SelectedFontSize   { get; private set; }

    public FontSettingsDialog(string currentFamily, double currentSize)
    {
        InitializeComponent();
        SelectedFontFamily = currentFamily;
        SelectedFontSize   = currentSize;

        // Select current font
        foreach (ComboBoxItem item in FontFamilyBox.Items)
        {
            if (item.Content?.ToString() == currentFamily)
            {
                FontFamilyBox.SelectedItem = item;
                break;
            }
        }
        if (FontFamilyBox.SelectedItem == null) FontFamilyBox.SelectedIndex = 0;

        FontSizeBox.Text = currentSize.ToString("0");
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewText == null) return;

        var family = (FontFamilyBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Yu Gothic UI";
        if (FontSettingsValidationService.ValidateFontSize(FontSizeBox.Text, out var size))
        {
            PreviewText.FontFamily = new FontFamily(family);
            PreviewText.FontSize   = size;
        }
    }

    private void FontFamilyBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePreview();

    private void FontSizeBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePreview();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var family = (FontFamilyBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (string.IsNullOrEmpty(family)) { DialogResult = false; return; }

        if (!FontSettingsValidationService.ValidateFontSize(FontSizeBox.Text, out var size))
        {
            MessageBox.Show("フォントサイズは 6〜72 の範囲で入力してください。", "入力エラー",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedFontFamily = family;
        SelectedFontSize   = size;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
