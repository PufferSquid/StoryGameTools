using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace StoryGameTools.Components.Services
{
    public class CharacterDatabaseService
    {
        private readonly ProjectService _projectService;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

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
            return JsonSerializer.Deserialize<CharacterDatabase>(json, JsonOptions) ?? new CharacterDatabase();
        }

        public async Task SaveAsync(CharacterDatabase database)
        {
            var path = _projectService.GetProjectPath("characters.json");
            if (path == null)
                throw new InvalidOperationException("No active project.");

            var json = JsonSerializer.Serialize(database, JsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<CharacterEntry> AddCharacterAsync(string id, string displayName)
        {
            var db = await LoadAsync();
            var key = NormalizeId(id);

            if (db.Characters.ContainsKey(key))
                throw new InvalidOperationException($"Character '{key}' already exists.");

            var entry = new CharacterEntry
            {
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName
            };
            db.Characters[key] = entry;
            await SaveAsync(db);
            return entry;
        }

        public async Task RemoveCharacterAsync(string id)
        {
            var db = await LoadAsync();
            var key = NormalizeId(id);
            if (!db.Characters.ContainsKey(key)) return;

            db.Characters.Remove(key);
            await SaveAsync(db);
        }

        public async Task RenameCharacterAsync(string oldId, string newId, string displayName)
        {
            var db = await LoadAsync();
            var oldKey = NormalizeId(oldId);
            var newKey = NormalizeId(newId);

            if (!db.Characters.TryGetValue(oldKey, out var entry))
                throw new InvalidOperationException($"Character '{oldKey}' not found.");

            if (oldKey != newKey)
            {
                if (db.Characters.ContainsKey(newKey))
                    throw new InvalidOperationException($"Character '{newKey}' already exists.");

                db.Characters.Remove(oldKey);
                db.Characters[newKey] = entry;
            }

            entry.DisplayName = displayName;
            await SaveAsync(db);
        }

        public async Task SetEmotionAsync(string characterId, string emotion, string relativePath)
        {
            var db = await LoadAsync();
            var key = NormalizeId(characterId);

            if (!db.Characters.TryGetValue(key, out var entry))
                throw new InvalidOperationException($"Character '{key}' not found.");

            entry.Emotions[NormalizeEmotion(emotion)] = NormalizePath(relativePath);
            await SaveAsync(db);
        }

        public async Task RemoveEmotionAsync(string characterId, string emotion)
        {
            var db = await LoadAsync();
            var key = NormalizeId(characterId);

            if (!db.Characters.TryGetValue(key, out var entry)) return;

            entry.Emotions.Remove(NormalizeEmotion(emotion));
            await SaveAsync(db);
        }

        public async Task SetDefaultEmotionAsync(string characterId, string emotion)
        {
            var db = await LoadAsync();
            var key = NormalizeId(characterId);

            if (!db.Characters.TryGetValue(key, out var entry))
                throw new InvalidOperationException($"Character '{key}' not found.");

            var emo = NormalizeEmotion(emotion);
            if (!entry.Emotions.TryGetValue(emo, out var path))
                throw new InvalidOperationException($"Emotion '{emo}' not found on '{key}'.");

            entry.Emotions["default"] = path;
            await SaveAsync(db);
        }

        public static string NormalizeId(string id) =>
            (id ?? "").Trim().ToLowerInvariant().Replace(' ', '_');

        public static string NormalizeEmotion(string emotion) =>
            (emotion ?? "").Trim().ToLowerInvariant().Replace(' ', '_');

        public static string NormalizePath(string path) =>
            (path ?? "").Replace('\\', '/').TrimStart('/');
    }
}