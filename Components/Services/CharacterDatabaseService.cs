using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StoryGameTools.Components.Services
{
    public class CharacterDatabaseService
    {
        private readonly ProjectService _projectService;

        private static readonly Regex EmotionCallRegex =
            new(@"\{(ChangeCharacter|Swap\d*)\(([^)]*)\)\}");
        private static readonly Regex QuotedArgRegex = new(@"""([^""]*)""");


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

        public async Task RenameEmotionAsync(string characterId, string oldEmotion, string newEmotion)
        {
            var db = await LoadAsync();
            var characterKey = NormalizeId(characterId);
            if (!db.Characters.TryGetValue(characterKey, out var entry))
                throw new InvalidOperationException($"Character '{characterKey}' not found.");

            var oldKey = NormalizeEmotion(oldEmotion);
            var newKey = NormalizeEmotion(newEmotion);

            if (string.IsNullOrWhiteSpace(newKey))
                throw new InvalidOperationException("Emotion name is required.");
            if (newKey == oldKey) return;
            if (entry.Emotions.ContainsKey(newKey))
                throw new InvalidOperationException($"'{newKey}' already exists on this character.");

            if (!entry.Emotions.TryGetValue(oldKey, out var imagePath))
                throw new InvalidOperationException($"Emotion '{oldKey}' not found on '{characterKey}'.");

            var scriptsPath = _projectService.GetProjectPath("dialogueScripts");
            var usedInScripts = scriptsPath != null
                && Directory.Exists(scriptsPath)
                && ScanEmotionsFromScripts(scriptsPath).Contains(oldKey);
            var usedByAnotherCharacter = db.Characters
                .Where(character => character.Key != characterKey)
                .Any(character => character.Value.Emotions.ContainsKey(oldKey));

            entry.Emotions[newKey] = imagePath;
            entry.Emotions.Remove(oldKey);
            if (!db.GlobalEmotions.Any(emotion => NormalizeEmotion(emotion) == newKey))
                db.GlobalEmotions.Add(newKey);
            if (!usedInScripts && !usedByAnotherCharacter)
                db.GlobalEmotions.RemoveAll(emotion => NormalizeEmotion(emotion) == oldKey);

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

        public HashSet<string> ScanEmotionsFromScripts(string scriptsFolder)
        {
            var found = new HashSet<string>();
            foreach (var file in Directory.GetFiles(scriptsFolder, "*.ink"))
            {
                var text = File.ReadAllText(file);
                foreach (Match call in EmotionCallRegex.Matches(text))
                {
                    var args = QuotedArgRegex.Matches(call.Groups[2].Value)
                                            .Select(m => NormalizeEmotion(m.Groups[1].Value))
                                            .Skip(1); // first quoted arg is the character id
                    foreach (var emo in args)
                        if (emo != "default") found.Add(emo);
                }
            }
            return found;
        }

        public async Task SyncGlobalEmotionsAsync(IEnumerable<string> scriptEmotions)
        {
            var db = await LoadAsync();
            db.GlobalEmotions = db.GlobalEmotions
                .Concat(scriptEmotions)
                .Concat(db.Characters.Values.SelectMany(c => c.Emotions.Keys))
                .Select(NormalizeEmotion)
                .Where(e => e != "default")
                .Distinct()
                .OrderBy(e => e)
                .ToList();
            await SaveAsync(db);
        }


        public async Task AssignImageAsync(string characterId, string emotion, byte[] pngBytes)
        {
            var db = await LoadAsync();
            var charKey = NormalizeId(characterId);
            var emoKey = NormalizeEmotion(emotion);

            if (!db.Characters.TryGetValue(charKey, out var entry))
                throw new InvalidOperationException($"Character '{charKey}' not found.");

            var relative = $"images/{charKey}/{charKey}_{emoKey}.png";

            // Overwriting would silently change any other emotion pointing at this file.
            if (entry.Emotions.Any(e => e.Key != emoKey && e.Value == relative))
                throw new InvalidOperationException($"'{relative}' is shared with another emotion.");

            var fullPath = _projectService.GetProjectPath(relative)
                ?? throw new InvalidOperationException("No active project.");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, pngBytes);

            entry.Emotions[emoKey] = relative;
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