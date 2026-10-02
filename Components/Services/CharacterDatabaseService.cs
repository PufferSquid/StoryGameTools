using System;
using System.Text.Json;

namespace StoryGameTools.Components.Services
{
    public class CharacterDatabaseService
    {
        private readonly ProjectService _projectService;

        public CharacterDatabaseService(ProjectService projectService)
        {
            _projectService = projectService;
        }

        public async Task<CharacterDatabase> LoadAsync()
        {
            var path = _projectService.GetProjectPath("characters.json");
            if (path == null || !File.Exists(path))
                return new CharacterDatabase();

            var json = await File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<CharacterDatabase>(json, options) ?? new CharacterDatabase();
        }
    }
}


