using System.Windows;
using System.Windows.Controls;

namespace NestSuite.IdeaNest.Views;

public enum ConfirmResult
{
    Primary,
    Cancel,
}

public partial class IdeaConfirmWindow : Window
{
    public ConfirmResult Result { get; private set; } = ConfirmResult.Cancel;

    private IdeaConfirmWindow()
    {
        InitializeComponent();
    }

    public static ConfirmResult ShowOkCancel(
        Window? owner,
        string header,
        string message,
        string primaryText,
        string cancelText)
    {
        var dlg = new IdeaConfirmWindow { Owner = owner };
        dlg.HeaderText.Text = header;
        dlg.MessageText.Text = message;

        dlg.ButtonStack.Children.Add(MakeButton(cancelText, "Secondary", () =>
        {
            dlg.Result = ConfirmResult.Cancel;
            dlg.Close();
        }, isCancel: true));

        dlg.ButtonStack.Children.Add(MakeButton(primaryText, "Primary", () =>
        {
            dlg.Result = ConfirmResult.Primary;
            dlg.Close();
        }, isDefault: true));

        dlg.ShowDialog();
        return dlg.Result;
    }

    private static Button MakeButton(string text, string styleKind, System.Action onClick,
                                     bool isDefault = false, bool isCancel = false)
    {
        var styleKey = styleKind == "Primary" ? "IdeaPrimaryButtonStyle" : "IdeaSecondaryButtonStyle";
        var btn = new Button
        {
            Content = text,
            Style = (Style)Application.Current.FindResource(styleKey),
            Margin = new Thickness(8, 0, 0, 0),
            IsDefault = isDefault,
            IsCancel = isCancel,
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }
}
