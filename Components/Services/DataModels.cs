using System;
using System.Collections.Generic;



namespace StoryGameTools.Components.Services
{
    public class ProjectProfile
    {
        public string Name { get; set; } = "";
        public string SchemaVersion { get; set; } = "1.0";
    }

    public class AppSettings
    {
        public string? LastOpenedProjectPath { get; set; }
    }

    public class ScriptFile
    {
        public string FileName { get; set; } = "";
        public string InputText { get; set; } = "";
        public string? OutputText { get; set; }
        public List<string> Warnings { get; set; } = new();
        public bool HasOutput => OutputText != null;
    }

    public class DroppedFile
    {
        public string Name { get; set; } = "";
        public string Base64 { get; set; } = "";
    }

    public class CharacterDatabase
    {
        public Dictionary<string, CharacterEntry> Characters { get; set; } = new();
    }

    public class CharacterEntry
    {
        public string DisplayName { get; set; } = "";
        public Dictionary<string, string> Emotions { get; set; } = new();
    }

    public class ParseResult
    {
        public string InkOutput { get; set; } = "";
        public List<string> Warnings { get; set; } = new();
    }
}


