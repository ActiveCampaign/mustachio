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

        private static readonly Regex _newlineFinder = new Regex("\n");

        private static int FindLineForLocation(string content, int character, ref int[] lines)
        {
            if (lines == null)
            {
                lines = _newlineFinder.Matches(content).OfType<Match>().Select(k => k.Index).ToArray();
            }
            var line = Array.BinarySearch(lines, character);
            return line < 0 ? ~line : line;
        }

        public static IEnumerable<TokenPair> Tokenize(string templateString)
        {
            templateString = templateString ?? "";
            var matches = _tokenFinder.Matches(templateString);
            var scopestack = new Stack<Tuple<string, int>>();

            var idx = 0;

            int[] lines = null;

            foreach (Match m in matches)
            {
                //yield front content.
                if (m.Index > idx)
                {
                    yield return new TokenPair(TokenType.Content, templateString.Substring(idx, m.Index - idx));
                }
                if (m.Value.StartsWith("{{#each"))
                {
                    scopestack.Push(Tuple.Create(m.Value, m.Index));
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim();
                    token = token.Substring(4);

                    if (token.StartsWith(" ") && token.Trim() != "")
                    {
                        yield return new TokenPair(TokenType.CollectionOpen, Validated(token, templateString, m.Index, ref lines).Trim());
                    }
                    else
                    {
                        var line = FindLineForLocation(templateString, m.Index, ref lines);
                        throw new TemplateParseException(@"The 'each' block being opened near on line {0} requires a model path to be specified in the form '{{{{#each <name>}}}}'.", line) { LineNumber = line };
                    }
                }
                else if (m.Value.StartsWith("{{/each"))
                {
                    if (m.Value != "{{/each}}")
                    {
                        var line = FindLineForLocation(templateString, m.Index, ref lines);
                        throw new TemplateParseException(@"The syntax to close the 'each' block on line {0} should be: '{{{{/each}}}}'.", line) { LineNumber = line };
                    }
                    else if (scopestack.Any() && scopestack.Peek().Item1.StartsWith("{{#each"))
                    {
                        var token = scopestack.Pop().Item1;
                        yield return new TokenPair(TokenType.CollectionClose, token);
                    }
                    else
                    {
                        var line = FindLineForLocation(templateString, m.Index, ref lines);
                        throw new TemplateParseException(@"An 'each' block is being closed on line {0}, but no corresponding opening element ('{{{{#each <name>}}}}') has was detected.", line) { LineNumber = line };
                    }
                }
                else if (m.Value.StartsWith("{{#"))
                {
                    //open group
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim();
                    if (scopestack.Any() && scopestack.Peek().Item1 == token)
                    {
                        yield return new TokenPair(TokenType.ElementClose, Validated(token, templateString, m.Index, ref lines));
                    }
                    else
                    {
                        scopestack.Push(Tuple.Create(token, m.Index));
                    }
                    yield return new TokenPair(TokenType.ElementOpen, Validated(token, templateString, m.Index, ref lines));
                }
                else if (m.Value.StartsWith("{{^"))
                {
                    //open inverted group
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('^').Trim();

                    if (scopestack.Any() && scopestack.Peek().Item1 == token)
                    {
                        yield return new TokenPair(TokenType.ElementClose, Validated(token, templateString, m.Index, ref lines));
                    }
                    else
                    {
                        scopestack.Push(Tuple.Create(token, m.Index));
                    }
                    yield return new TokenPair(TokenType.InvertedElementOpen, Validated(token, templateString, m.Index, ref lines));
                }
                else if (m.Value.StartsWith("{{/"))
                {
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('/').Trim();
                    //close group
                    if (scopestack.Any() && scopestack.Peek().Item1 == token)
                    {
                        scopestack.Pop();
                        yield return new TokenPair(TokenType.ElementClose, Validated(token, templateString, m.Index, ref lines));
                    }
                    else
                    {
                        var line = FindLineForLocation(templateString, m.Index, ref lines);
                        throw new TemplateParseException("It appears that open and closing elements are mismatched on line {0}.", line) { LineNumber = line };
                    }
                }
                else if (m.Value.StartsWith("{{{") | m.Value.StartsWith("{{&"))
                {
                    //escaped single element
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('&').Trim();
                    yield return new TokenPair(TokenType.UnescapedSingleValue, Validated(token, templateString, m.Index, ref lines));
                }
                else if (m.Value.StartsWith("{{!"))
                {
                    //it's a comment drop this on the floor, no need to even yield it.
                }
                else
                {
                    //unsingle value.
                    var token = m.Value.TrimStart('{').TrimEnd('}').Trim();
                    yield return new TokenPair(TokenType.EscapedSingleValue, Validated(token, templateString, m.Index, ref lines));
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
                var lineNumbers = scopestack.Reverse().Select(k => FindLineForLocation(templateString, k.Item2, ref lines));

                var scopes = String.Join(",", scopestack.Select(k =>
                {
                    var value = k.Item1.Trim('{', '#', '}');
                    if (value.StartsWith("each "))
                    {
                        value = value.Substring(5);
                    }
                    return "'" + value + "'";
                }).Reverse().ToArray());
                if (scopes.Length > 1)
                {

                    throw new TemplateParseException("Scope blocks to the following paths were opened but not closed: " + scopes +
                        ", please close them using appropriate syntax.") { LineNumber = lineNumbers.First() };
                }
                else
                {
                    //var line = FindLineForLocation(templateString, m.Index, ref lines);
                    throw new TemplateParseException("A scope block to the following path was opened but not closed:" + scopes +
                        ", please close it using the appropriate syntax.") { LineNumber = lineNumbers.First() };
                }
            }
            #endregion

            yield break;
        }

        /// <summary>
        /// Specifies combnations of paths that don't work.
        /// </summary>
        private static readonly Regex _negativePathSpec = new Regex("([.]{3,})|([^\\w./_]+)|((?<![.]{2})[/])|([.]{2,}($|[^/]))", RegexOptions.Singleline | RegexOptions.Compiled);

        private static string Validated(string token, string content, int index, ref int[] lines)
        {
            token = token.Trim();

            if (_negativePathSpec.Match(token).Success)
            {
                var line = FindLineForLocation(content, index, ref lines);
                throw new TemplateParseException("The path '{0}' on line {1} is not valid. Please see documentation for examples of valid paths.", token, line) { LineNumber = line };
            }
            return token;
        }
    }
}