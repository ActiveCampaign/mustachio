using System;
using System.Collections.Generic;
using System.Text;

namespace Mustachio
{
    public enum Precedence
    {
        Low, High
    }

    public class TokenExpander
    {
        public string Prefix { get; set; }
        public Precedence Precedence { get; set; }
        public Func<string, ParsingOptions, TokenizeResult> ExpandTokens { get; set; }
        public Func<string, Queue<TokenTuple>, ParsingOptions, InferredTemplateModel, Action<StringBuilder, ContextObject>> RenderTokens { get; set; }
    }
}
