using System.Windows;
using NoteNest.Models;

namespace NoteNest.Services;

public class ThemeService
{
    public void Apply(AppTheme theme)
    {
        var name = theme == AppTheme.Dark ? "Dark" : "Light";
        var uri  = new Uri($"pack://application:,,,/Themes/{name}.xaml");
        var dict = new ResourceDictionary { Source = uri };

        var merged   = Application.Current.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("/Themes/") == true);
        if (existing != null) merged.Remove(existing);
        merged.Add(dict);
    }
}
