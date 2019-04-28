using System;

namespace Mustachio
{
    /// <summary>
    /// Indicates a parse error including line and character info. 
    /// </summary>
    public class IndexedParseException : ParseException
    {
        internal IndexedParseException(string sourceName, Tokenizer.CharacterLocation location, string message, params object[] replacements)
            : this(sourceName, message, replacements)
        {
            this.LineNumber = location.Line;
            this.CharacterOnLine = location.Character;
            this.SourceName = sourceName;
        }

        public IndexedParseException(string sourceName, string message, params object[] replacements)
            : base(String.Format(message, replacements))
        {
            this.SourceName = sourceName;
        }

        public int LineNumber { get; set; }
        public int CharacterOnLine { get; set; }
        public string SourceName { get; set; }
    }
}
