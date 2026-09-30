using System.Text.Json;

namespace StoryGameTools.Components.Services
{
    public class ProjectService
    {
        private readonly AppSettingsService _appSettings;
        private readonly ILoggerService _logger;

        public string? ActiveProjectPath { get; private set; }
        public ProjectProfile? ActiveProject { get; private set; }

        public event Action? ActiveProjectChanged;

        public ProjectService(AppSettingsService appSettings, ILoggerService logger)
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task TryRestoreLastProjectAsync()
        {
            var settings = await _appSettings.LoadAsync();
            if (settings.LastOpenedProjectPath != null)
                await OpenProjectAsync(settings.LastOpenedProjectPath);
        }

        public async Task<List<string>> OpenProjectAsync(string folderPath)
        {
            var warnings = new List<string>();
            var projectFile = Path.Combine(folderPath, "project.json");

            if (!File.Exists(projectFile))
            {
                warnings.Add("No project.json found at this location.");
                return warnings;
            }

            var json = await File.ReadAllTextAsync(projectFile);
            var profile = JsonSerializer.Deserialize<ProjectProfile>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (profile == null)
            {
                warnings.Add("project.json could not be read.");
                return warnings;
            }

            // Warn about missing expected folders, but still open
            foreach (var folder in new[] { "images", "dialogueScripts" })
            {
                if (!Directory.Exists(Path.Combine(folderPath, folder)))
                    warnings.Add($"Expected folder '{folder}' not found. It will be created.");
            }

            EnsureFolderStructure(folderPath);
            SetActiveProject(folderPath, profile);

            var settings = await _appSettings.LoadAsync();
            settings.LastOpenedProjectPath = folderPath;
            await _appSettings.SaveAsync(settings);

            return warnings;
        }

        public async Task CreateProjectAsync(string folderPath, string projectName)
        {
            var profile = new ProjectProfile { Name = projectName };
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(folderPath, "project.json"), json);

            var emptyDb = new CharacterDatabase();
            var dbJson = JsonSerializer.Serialize(emptyDb, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(folderPath, "characters.json"), dbJson);

            EnsureFolderStructure(folderPath);
            SetActiveProject(folderPath, profile);

            var settings = await _appSettings.LoadAsync();
            settings.LastOpenedProjectPath = folderPath;
            await _appSettings.SaveAsync(settings);
        }

        private void EnsureFolderStructure(string root)
        {
            Directory.CreateDirectory(Path.Combine(root, "images"));
            Directory.CreateDirectory(Path.Combine(root, "dialogueScripts"));
        }

        private void SetActiveProject(string path, ProjectProfile profile)
        {
            ActiveProjectPath = path;
            ActiveProject = profile;
            ActiveProjectChanged?.Invoke();
        }

        // Helper other services will use to build paths within the active project
        public string? GetProjectPath(string relativePath)
        {
            if (ActiveProjectPath == null) return null;
            return Path.Combine(ActiveProjectPath, relativePath);
        }
    }
}