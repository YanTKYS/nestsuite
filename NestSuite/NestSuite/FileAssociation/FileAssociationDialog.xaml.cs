using System;
using System.Windows;
using System.Windows.Media;

namespace NoteNest.NestSuite.FileAssociation;

public partial class FileAssociationDialog : Window
{
    private readonly FileAssociationService _service = new();
    private readonly string _exePath;

    public FileAssociationDialog(string exePath)
    {
        _exePath = exePath;
        InitializeComponent();
        ExePathLabel.Text = exePath;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        SetStatus(StatusNotenest, _service.GetStatus(".notenest", _exePath));
        SetStatus(StatusChatnest, _service.GetStatus(".chatnest", _exePath));
        SetStatus(StatusIdeanest, _service.GetStatus(".ideanest", _exePath));
    }

    private static void SetStatus(System.Windows.Controls.TextBlock label, FileAssociationStatus status)
    {
        (label.Text, label.Foreground) = status switch
        {
            FileAssociationStatus.Registered        => ("登録済み", new SolidColorBrush(Color.FromRgb(0x22, 0x8B, 0x22))),
            FileAssociationStatus.NotRegistered     => ("未登録", SystemColors.GrayTextBrush),
            FileAssociationStatus.OtherApp          => ("他のアプリに関連付け済み（HKCU に別の登録あり）",
                                                        new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00))),
            FileAssociationStatus.StaleRegistration => ("再登録が必要（別の実行ファイルへの古い登録が残っています）",
                                                        new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00))),
            _ => ("不明", SystemColors.GrayTextBrush)
        };
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
            this,
            $".notenest / .chatnest / .ideanest を\n\"{_exePath}\"\nに関連付けます。よろしいですか？",
            "ファイル関連付けの登録",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question) != MessageBoxResult.OK) return;

        try
        {
            _service.Register(_exePath);
            RefreshStatus();
            MessageBox.Show(
                this,
                "3 つの拡張子の関連付けを登録しました。\nファイルをダブルクリックして動作を確認してください。",
                "登録完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"登録に失敗しました。\n\n{ex.Message}",
                "登録エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UnregisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
            this,
            "この機能で作成したファイル関連付けを解除します。よろしいですか？",
            "ファイル関連付けの解除",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question) != MessageBoxResult.OK) return;

        try
        {
            var result = _service.Unregister();
            RefreshStatus();
            var msg = result == UnregisterResult.Removed
                ? "ファイル関連付けを解除しました。"
                : "この機能で作成した登録が見つかりませんでした。";
            MessageBox.Show(this, msg, "解除完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"解除に失敗しました。\n\n{ex.Message}",
                "解除エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
