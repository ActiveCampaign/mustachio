using System;

namespace Mustachio
{
    /// <summary>
    /// The type of token produced in the lexing stage of template compilation.
    /// </summary>
    internal enum TokenType
    {
        EscapedSingleValue,
        UnescapedSingleValue,
        InvertedElementOpen,
        ElementOpen,
        ElementClose,
        Comment,
        Content,
        CollectionOpen,
        CollectionClose
    }

    /// <summary>
    /// The token that has been lexed out of template content.
    /// </summary>
    internal class TokenPair
    {
        public TokenPair(TokenType type, String value)
        {
            this.Type = type;
            this.Value = value;
        }

        public TokenType Type { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            return String.Format("{0}, {1}", this.Type, this.Value);
        }
    }
}
