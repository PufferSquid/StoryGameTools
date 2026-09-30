using System.Text;
using System.Text.RegularExpressions;

namespace StoryGameTools.Components.Services
{
    public class ScriptParserService
    {
        private static readonly Regex CharacterLineRegex = new(@"^([A-Za-z0-9_]+):\s*$");
        private static readonly Regex EmotionTagRegex = new(@"#\s*(.+)$");
        private static readonly Regex AutoLineRegex = new(@"^#\s*auto\s+(start|stop|end)\s*$", RegexOptions.IgnoreCase);

        public ParseResult Parse(string scriptText, CharacterDatabase db)
        {
            var result = new ParseResult();
            var lines = scriptText.Replace("\r\n", "\n").Split('\n');
            var output = new StringBuilder();
            string? currentCharacter = null;
            bool expectingEmotionTag = false;
            string? pendingTag = null;
            bool isFirstCharacterBlock = true;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                var lineNum = i + 1;

                var charMatch = CharacterLineRegex.Match(line.Trim());
                if (charMatch.Success)
                {
                    if (pendingTag != null)
                    {
                        result.Warnings.Add($"Line {lineNum}: emotion tag '{pendingTag}' was never applied to a dialogue line before the character changed.");
                        pendingTag = null;
                    }

                    currentCharacter = charMatch.Groups[1].Value;
                    expectingEmotionTag = true;
                    ValidateCharacter(currentCharacter, db, result.Warnings, lineNum);

                    if (!isFirstCharacterBlock)
                        output.AppendLine();
                    isFirstCharacterBlock = false;

                    continue;
                }

                // FIX ASAP - Needs to check whether there's an "auto end" if there's an "auto start" (wouldn't want auto to ever be left running indefinitely)
                // also, should check whether there's an # auto start BEFORE an # auto end.
                var autoMatch = AutoLineRegex.Match(line.Trim());  
                if (autoMatch.Success)
                {
                    bool starting = autoMatch.Groups[1].Value.Equals("start", StringComparison.OrdinalIgnoreCase);
                    output.AppendLine($"~autoAdvance = {(starting ? "true" : "false")}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (currentCharacter == null)
                {
                    result.Warnings.Add($"Line {lineNum}: dialogue found before any character was set.");
                    output.AppendLine(line);
                    continue;
                }

                var tagMatch = EmotionTagRegex.Match(line);
                bool hasTag = tagMatch.Success;
                string dialogueText = hasTag ? line[..tagMatch.Index].TrimEnd() : line;
                string? inlineTag = hasTag ? tagMatch.Groups[1].Value : null;

                if (hasTag && dialogueText.Length == 0)
                {
                    // Tag-only line — defer it to whatever dialogue line comes next.
                    if (pendingTag != null)
                        result.Warnings.Add($"Line {lineNum}: emotion tag '{pendingTag}' was overwritten by a new tag before being applied.");
                    pendingTag = inlineTag;
                    continue;
                }

                string? tagContent;
                if (pendingTag != null)
                {
                    if (hasTag)
                    {
                        result.Warnings.Add($"Line {lineNum}: a pending tag from an earlier line and an inline tag both apply here; using the inline tag.");
                        tagContent = inlineTag;
                    }
                    else
                    {
                        tagContent = pendingTag;
                    }
                    pendingTag = null;
                }
                else
                {
                    tagContent = inlineTag;
                }

                if (expectingEmotionTag && tagContent == null)
                {
                    tagContent = "default";
                    result.Warnings.Add($"Line {lineNum}: missing emotion tag on first line after '{currentCharacter}' started talking. Defaulting to 'default'.");
                }

                if (tagContent != null)
                    EmitEmotionCall(currentCharacter, tagContent, db, result.Warnings, lineNum, output);

                expectingEmotionTag = false;
                output.AppendLine(dialogueText);
            }

            result.InkOutput = output.ToString();
            return result;
        }

        private void EmitEmotionCall(string character, string tagContent, CharacterDatabase db,
    List<string> warnings, int lineNum, StringBuilder output)
        {
            var parts = tagContent.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            var charKey = character.ToLowerInvariant();

            if (parts.Count == 1)
            {
                ValidateEmotion(character, parts[0], db, warnings, lineNum);
                output.AppendLine($"{{ChangeCharacter(\"{charKey}\", \"{parts[0]}\")}}");
                return;
            }

            if (parts.Count % 2 == 0)
            {
                warnings.Add($"Line {lineNum}: malformed emotion tag '{tagContent}' — expected an emotion, then alternating duration/emotion pairs.");
                return;
            }

            for (int i = 0; i + 2 < parts.Count; i += 2)
            {
                if (string.Equals(parts[i], parts[i + 2], StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"Line {lineNum}: swap tag '{tagContent}' transitions to the same emotion ('{parts[i + 2]}') twice in a row.");
                }
            }

            var callArgs = new List<string> { $"\"{charKey}\"" };
            for (int i = 0; i < parts.Count; i++)
            {
                if (i % 2 == 0)
                {
                    ValidateEmotion(character, parts[i], db, warnings, lineNum);
                    callArgs.Add($"\"{parts[i]}\"");
                }
                else
                {
                    if (!double.TryParse(parts[i], out _))
                        warnings.Add($"Line {lineNum}: expected a number for duration but got '{parts[i]}' in tag '{tagContent}'.");
                    callArgs.Add(parts[i]);
                }
            }

            int transitions = (parts.Count - 1) / 2;
            string functionName = transitions == 1 ? "Swap" : $"Swap{transitions}";
            output.AppendLine($"{{{functionName}({string.Join(", ", callArgs)})}}");
        }

        private void ValidateCharacter(string name, CharacterDatabase db, List<string> warnings, int lineNum)
        {
            if (!db.Characters.ContainsKey(name.ToLowerInvariant()))
                warnings.Add($"Line {lineNum}: character '{name}' not found in character database.");
        }

        private void ValidateEmotion(string name, string emotion, CharacterDatabase db, List<string> warnings, int lineNum)
        {
            var key = name.ToLowerInvariant();
            if (!db.Characters.TryGetValue(key, out var entry)) return; // already warned above
            if (!entry.Emotions.ContainsKey(emotion))
                warnings.Add($"Line {lineNum}: emotion '{emotion}' not found for character '{name}'.");
        }
    }
}