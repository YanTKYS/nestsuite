using System.Windows;
using NestSuite.Models;

namespace NestSuite.Services;

public class ThemeService
{
    public static event EventHandler? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        var name = theme == AppTheme.Dark ? "Dark" : "Light";
        var uri  = new Uri($"pack://application:,,,/Themes/{name}.xaml");
        var dict = new ResourceDictionary { Source = uri };

        var merged = Application.Current.Resources.MergedDictionaries;
        foreach (var old in merged.Where(d =>
            d.Source?.OriginalString.Contains("Themes/") == true).ToList())
            merged.Remove(old);
        merged.Add(dict);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
}
