using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatNest.Services
{
    public class SettingsService
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatNest");

        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        private const int MaxRecentFiles = 5;

        public List<string> GetRecentFiles()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new();
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json)?.RecentFiles ?? new();
            }
            catch { return new(); }
        }

        public void AddRecentFile(string path)
        {
            try
            {
                var files = GetRecentFiles();
                files.RemoveAll(f => string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
                files.Insert(0, path);
                Save(new AppSettings { RecentFiles = files.Take(MaxRecentFiles).ToList() });
            }
            catch { }
        }

        private static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }

    public class AppSettings
    {
        [JsonPropertyName("recentFiles")]
        public List<string> RecentFiles { get; set; } = new();
    }
}
