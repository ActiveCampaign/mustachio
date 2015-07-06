using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mustachio
{
    /// <summary>
    /// Reads in a mustache template and lexes it into tokens.
    /// </summary>
    /// <exception cref="Mustachio.TemplateParseException"></exception>
    internal class Tokenizer
    {
        private static readonly Regex _tokenFinder = new Regex("([{]{2}[^{}]+?[}]{2})|([{]{3}[^{}]+?[}]{3})",
            RegexOptions.Compiled | RegexOptions.Compiled);

        public static IEnumerable<TokenPair> Tokenize(string templateString)
        {
            templateString = templateString ?? "";
            var matches = _tokenFinder.Matches(templateString);
            var scopestack = new Stack<string>();

            var idx = 0;

            foreach (Match m in matches)
            {
                //yield front content.
                if (m.Index > idx)
                {
                    yield return new TokenPair(TokenType.Content, templateString.Substring(idx, m.Index - idx));
                }
                if (m.Value.StartsWith("{{#each"))
                {
                    scopestack.Push(m.Value);
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim();
                    token = token.Substring(4);

                    if (token.StartsWith(" ") && token.Trim() != "")
                    {
                        yield return new TokenPair(TokenType.CollectionOpen, Validated(token).Trim());
                    }
                    else
                    {
                        throw new TemplateParseException(@"The 'each' block being opened near character {0} requires a model path to be specified in the form '{{{{#each <name>}}}}'.", m.Index);
                    }
                }
                else if (m.Value.StartsWith("{{/each"))
                {
                    if (m.Value != "{{/each}}")
                    {
                        throw new TemplateParseException(@"The syntax to close the 'each' block near character {0} should be: '{{{{/each}}}}'.", m.Index);
                    }
                    else if (scopestack.Any() && scopestack.Peek().StartsWith("{{#each"))
                    {
                        var token = scopestack.Pop();
                        yield return new TokenPair(TokenType.CollectionClose, token);
                    }
                    else
                    {
                        throw new TemplateParseException(@"An 'each' block is being closed near character {0}, but no corresponding opening element ('{{{{#each <name>}}}}') has was detected.", m.Index);
                    }
                }
                else if (m.Value.StartsWith("{{#"))
                {
                    //open group
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim();
                    if (scopestack.Any() && scopestack.Peek() == token)
                    {
                        yield return new TokenPair(TokenType.ElementClose, Validated(token));
                    }
                    else
                    {
                        scopestack.Push(token);
                    }
                    yield return new TokenPair(TokenType.ElementOpen, Validated(token));
                }
                else if (m.Value.StartsWith("{{^"))
                {
                    //open inverted group
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('^').Trim();

                    if (scopestack.Any() && scopestack.Peek() == token)
                    {
                        yield return new TokenPair(TokenType.ElementClose, Validated(token));
                    }
                    else
                    {
                        scopestack.Push(token);
                    }
                    yield return new TokenPair(TokenType.InvertedElementOpen, Validated(token));
                }
                else if (m.Value.StartsWith("{{/"))
                {
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('/').Trim();
                    //close group
                    if (scopestack.Any() && scopestack.Peek() == token)
                    {
                        scopestack.Pop();
                        yield return new TokenPair(TokenType.ElementClose, Validated(token));
                    }
                    else
                    {
                        throw new TemplateParseException("It appears that open and closing elements are mismatched near character index {0}.", idx);
                    }
                }
                else if (m.Value.StartsWith("{{{") | m.Value.StartsWith("{{&"))
                {
                    //escaped single element
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('&').Trim();
                    yield return new TokenPair(TokenType.UnescapedSingleValue, Validated(token));
                }
                else if (m.Value.StartsWith("{{!"))
                {
                    //it's a comment drop this on the floor, no need to even yield it.
                }
                else
                {
                    //unsingle value.
                    var token = m.Value.TrimStart('{').TrimEnd('}').Trim();
                    yield return new TokenPair(TokenType.EscapedSingleValue, Validated(token));
                }

                //move forward in the string.
                idx = m.Index + m.Length;
            }

            if (idx < templateString.Length)
            {
                yield return new TokenPair(TokenType.Content, templateString.Substring(idx));
            }

            #region Assert that any scopes opened must be closed.
            if (scopestack.Any())
            {
                var scopes = String.Join(",", scopestack.Select(k =>
                {
                    var value = k.Trim('{', '#', '}');
                    if (value.StartsWith("each "))
                    {
                        value = value.Substring(5);
                    }
                    return "'" + value + "'";
                }).ToArray());
                if (scopes.Length > 1)
                {
                    throw new TemplateParseException("Scope blocks to the following paths were opened but not closed: " + scopes +
                        ", please close them using appropriate syntax.");
                }
                else
                {
                    throw new TemplateParseException("A scope block to the following path was opened but not closed:" + scopes +
                        ", please close it using the appropriate syntax.");
                }
            }
            #endregion

            yield break;
        }

        /// <summary>
        /// Specifies combnations of paths that don't work.
        /// </summary>
        private static readonly Regex _negativePathSpec = new Regex("([.]{3,})|([^\\w./_]+)|((?<![.]{2})[/])|([.]{2,}($|[^/]))", RegexOptions.Singleline | RegexOptions.Compiled);

        private static string Validated(string token)
        {
            token = token.Trim();

            if (_negativePathSpec.Match(token).Success)
            {
                throw new TemplateParseException("The path '{0}' is not valid. Please see documentation for examples of valid paths.", token);
            }
            return token;
        }
    }
}