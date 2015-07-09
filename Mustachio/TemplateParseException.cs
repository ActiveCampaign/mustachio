using System;

namespace Mustachio
{
    public class TemplateParseException : Exception
    {
        public TemplateParseException(string message, params object[] replacements)
            : base(String.Format(message, replacements))
        {

        }

        public int? LineNumber { get; set; }
        public int? CharacterOnLine { get; set; }
    }
}
