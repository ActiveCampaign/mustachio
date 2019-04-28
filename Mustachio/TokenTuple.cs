using System;
using System.Collections.Generic;
using System.Text;

namespace Mustachio
{
    /// <summary>
    /// The type of token produced in the lexing stage of template compilation.
    /// </summary>
    public enum TokenType
    {
        EscapedSingleValue,
        UnescapedSingleValue,
        InvertedElementOpen,
        ElementOpen,
        ElementClose,
        Comment,
        Content,
        CollectionOpen,
        CollectionClose,
        Custom
    }

    /// <summary>
    /// The token that has been lexed out of template content.
    /// </summary>
    public class TokenTuple
    {
        public TokenTuple(TokenType type, String value)
        {
            this.Type = type;
            this.Value = value;
        }

        public TokenTuple(TokenType type, String value, 
            Func<string, Queue<TokenTuple>, ParsingOptions, InferredTemplateModel, Action<StringBuilder, ContextObject>> renderTokens)
        {
            this.Type = type;
            this.Value = value;
            this.RenderTokens = renderTokens;
        }

        public TokenType Type { get; set; }

        public string Value { get; set; }

        public Func<string, Queue<TokenTuple>, ParsingOptions, InferredTemplateModel, Action<StringBuilder, ContextObject>> RenderTokens { get; set; }

        public override string ToString()
        {
            return $"{this.Type}, {this.Value}";
        }
    }
}
