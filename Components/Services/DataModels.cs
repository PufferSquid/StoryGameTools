using System;
using System.Collections.Generic;



namespace StoryGameTools.Components.Services
{
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


