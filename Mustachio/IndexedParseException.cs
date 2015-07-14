using System;

namespace Mustachio
{
    /// <summary>
    /// Indicates a parse error including line and character info. 
    /// </summary>
    public class IndexedParseException : ParseException
    {
        internal IndexedParseException(Mustachio.Tokenizer.CharacterLocation location, string message, params object[] replacements)
            : this(message, replacements)
        {
            this.LineNumber = location.Line;
            this.CharacterOnLine = location.Character;
        }

        public IndexedParseException(string message, params object[] replacements)
            : base(String.Format(message, replacements))
        {

        }

        public int LineNumber { get; set; }
        public int CharacterOnLine { get; set; }
    }
}
