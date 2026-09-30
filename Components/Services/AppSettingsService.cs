using System.Text.Json;

namespace StoryGameTools.Components.Services
{
    public class AppSettingsService
    {
        private readonly string _settingsPath;

        public AppSettingsService()
        {
            _settingsPath = Path.Combine(FileSystem.AppDataDirectory, "appSettings.json");
        }

        public async Task<AppSettings> LoadAsync()
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public async Task SaveAsync(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, json);
        }
    }
}