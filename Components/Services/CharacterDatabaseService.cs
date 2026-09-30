using System;
using System.Text.Json;


namespace StoryGameTools.Components.Services
{
    public class CharacterDatabaseService
    {
        private readonly string _dataPath;

        public CharacterDatabaseService()
        {
            _dataPath = Path.Combine(FileSystem.AppDataDirectory, "testCharacters.json");
        }

        public async Task<CharacterDatabase> LoadAsync()
        {
            if (!File.Exists(_dataPath))
                return new CharacterDatabase();

            var json = await File.ReadAllTextAsync(_dataPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<CharacterDatabase>(json, options) ?? new CharacterDatabase();
        }
    }
}


