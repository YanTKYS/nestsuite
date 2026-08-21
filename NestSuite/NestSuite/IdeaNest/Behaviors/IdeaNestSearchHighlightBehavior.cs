using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NestSuite.IdeaNest.Services;

namespace NestSuite.IdeaNest.Behaviors;

/// <summary>
/// TextBlock へ「表示対象文字列 + 検索語」の読み取り専用ハイライト表示を付与する
/// 添付プロパティ。<see cref="IdeaNestSearchHighlightHelper"/>（既存3-Run分割helperの薄いラッパー）
/// が返すセグメントを <see cref="TextBlock.Inlines"/> の <see cref="Run"/> として並べるだけで、
/// RichTextBox・FlowDocument・独自テキストレンダラーは使用しない。
/// カード表示専用であり、Model（<see cref="NestSuite.IdeaNest.Models.Idea"/>）や
/// 保存データへは一切書き込まない。TextBlock.Text は使わず Inlines のみを操作するため、
/// AutomationName など既存のカード全体の読み上げ内容は変更しない。
/// </summary>
public static class IdeaNestSearchHighlightBehavior
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(IdeaNestSearchHighlightBehavior),
        new PropertyMetadata(string.Empty, OnHighlightInputChanged));

    public static readonly DependencyProperty QueryProperty = DependencyProperty.RegisterAttached(
        "Query",
        typeof(string),
        typeof(IdeaNestSearchHighlightBehavior),
        new PropertyMetadata(string.Empty, OnHighlightInputChanged));

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    public static string GetQuery(DependencyObject obj) => (string)obj.GetValue(QueryProperty);
    public static void SetQuery(DependencyObject obj, string value) => obj.SetValue(QueryProperty, value);

    private static void OnHighlightInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        Apply(textBlock);
    }

    private static void Apply(TextBlock textBlock)
    {
        var text = GetText(textBlock) ?? string.Empty;
        var query = GetQuery(textBlock) ?? string.Empty;

        textBlock.Inlines.Clear();

        var segments = IdeaNestSearchHighlightHelper.Split(text, query);
        foreach (var segment in segments)
        {
            if (segment.Text.Length == 0)
                continue;

            var run = new Run(segment.Text);
            if (segment.IsMatch)
            {
                run.SetResourceReference(TextElement.BackgroundProperty, "SearchHighlightBrush");
            }

            textBlock.Inlines.Add(run);
        }
    }
}
