using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mustachio
{
    public enum Precedence
    {
        Low, Medium, High
    }

    public class TokenExpander
    {
        /// <summary>
        /// RegEx used to identify the custom token.
        /// </summary>
        public Regex RegEx { get; set; }

        /// <summary>
        /// Low precedence expanders will be evaluated after all Mustache syntax tokens (e.g.: "each" blocks, groups, etc.).
        /// Medium precedence expanders will be evaluated after "each" blocks and groups, but before unescaped variables {{{ var }}} syntax
        /// High precedence expanders will be evaluated before all Mustache syntax tokens.
        /// The order of the expanders passed in the array in ParsingOptions will be honored when applying them if more granularity is required.
        /// </summary>
        public Precedence Precedence { get; set; } = Precedence.Medium;

        /// <summary>
        /// A function that generates new tokens to be used in the parent template.
        /// Note that the custom token will be added before the expanded tokens.
        /// </summary>
        public Func<string, ParsingOptions, TokenizeResult> ExpandTokens { get; set; }

        /// <summary>
        /// A function that allows the rendering of the custom token and the following tokens.
        /// If this function is not provided, the custom token will not be rendered at all
        /// and the following tokens will be rendered using the default behaviour.
        /// </summary>
        public Func<string, Queue<TokenTuple>, ParsingOptions, InferredTemplateModel, Action<StringBuilder, ContextObject>> Renderer { get; set; }
    }
}
