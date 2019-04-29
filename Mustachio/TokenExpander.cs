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
        /// <summary>
        /// Prefix used to identify the custom token.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Low precedence expanders will be evaluated after Mustache syntax tokens (e.g.: each blocks, groups, etc.).
        /// High precedence expanders will be evaluated before Mustache syntax tokens.
        /// </summary>
        public Precedence Precedence { get; set; }

        /// <summary>
        /// A function that generates new tokens to be used in the parent template.
        /// Note that the custom token will be added before the expanded tokens.
        /// To add a partial, you can plug in data and return Tokenizer.Tokenize(dataString, options).
        /// </summary>
        public Func<string, ParsingOptions, TokenizeResult> ExpandTokens { get; set; }

        /// <summary>
        /// A function that allows to render the custom token and the following tokens.
        /// If this function is not provided, the custom token will not be rendered at all
        /// and the following tokens will be rendered using the default behaviour.
        /// </summary>
        public Func<string, Queue<TokenTuple>, ParsingOptions, InferredTemplateModel, Action<StringBuilder, ContextObject>> RenderTokens { get; set; }
    }
}
