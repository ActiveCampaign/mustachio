using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mustachio
{
    /// <summary>
    /// Reads in a mustache template and lexes it into tokens.
    /// </summary>
    /// <exception cref="Mustachio.IndexedParseException"></exception>
    internal class Tokenizer
    {
        internal class CharacterLocation
        {
            public int Line { get; set; }
            public int Character { get; set; }
        }

        private static readonly Regex _tokenFinder = new Regex("([{]{2}[^{}]+?[}]{2})|([{]{3}[^{}]+?[}]{3})",
            RegexOptions.Compiled | RegexOptions.Compiled);

        private static readonly Regex _newlineFinder = new Regex("\n", RegexOptions.Compiled);

        private static CharacterLocation HumanizeCharacterLocation(string content, int characterIndex, ref int[] lines)
        {
            if (lines == null)
            {
                lines = _newlineFinder.Matches(content).OfType<Match>().Select(k => k.Index).ToArray();
            }
            var line = Array.BinarySearch(lines, characterIndex);
            line = line < 0 ? ~line : line;

            var charIdx = characterIndex;
            //in both of these cases, we want to increment the char index by one to account for the '\n' that is skipped in the indexes.
            if (line < lines.Length && line > 0)
            {
                charIdx = characterIndex - (lines[line - 1] + 1);
            }
            else if (line > 0)
            {
                charIdx = characterIndex - (lines.LastOrDefault() + 1);
            }

            var retval = new CharacterLocation
            {
                //Humans count from 1, so let's do that, too (hence the "++" on these).
                Line = line + 1,
                Character = charIdx + 1
            };
            return retval;
        }

        public static IEnumerable<TokenPair> Tokenize(string templateString)
        {
            templateString = templateString ?? "";
            var matches = _tokenFinder.Matches(templateString);
            var scopestack = new Stack<Tuple<string, int>>();

            var idx = 0;

            var parseErrors = new List<IndexedParseException>();
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
                        yield return new TokenPair(TokenType.CollectionOpen, Validated(token, templateString, m.Index, ref lines, ref parseErrors).Trim());
                    }
                    else
                    {
                        var location = HumanizeCharacterLocation(templateString, m.Index, ref lines);
                        parseErrors.Add(new IndexedParseException(location, @"The 'each' block being opened requires a model path to be specified in the form '{{{{#each <name>}}}}'."));
                    }
                }
                else if (m.Value.StartsWith("{{/each"))
                {
                    if (m.Value != "{{/each}}")
                    {
                        var location = HumanizeCharacterLocation(templateString, m.Index, ref lines);
                        parseErrors.Add(new IndexedParseException(location, @"The syntax to close the 'each' block should be: '{{{{/each}}}}'."));
                    }
                    else if (scopestack.Any() && scopestack.Peek().Item1.StartsWith("{{#each"))
                    {
                        var token = scopestack.Pop().Item1;
                        yield return new TokenPair(TokenType.CollectionClose, token);
                    }
                    else
                    {
                        var location = HumanizeCharacterLocation(templateString, m.Index, ref lines);
                        parseErrors.Add(new IndexedParseException(location, @"An 'each' block is being closed, but no corresponding opening element ('{{{{#each <name>}}}}') was detected."));
                    }
                }
                else if (m.Value.StartsWith("{{#"))
                {
                    //open group
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim();
                    if (scopestack.Any() && scopestack.Peek().Item1 == token)
                    {
                        yield return new TokenPair(TokenType.ElementClose, Validated(token, templateString, m.Index, ref lines, ref parseErrors));
                    }
                    else
                    {
                        scopestack.Push(Tuple.Create(token, m.Index));
                    }
                    yield return new TokenPair(TokenType.ElementOpen, Validated(token, templateString, m.Index, ref lines, ref parseErrors));
                }
                else if (m.Value.StartsWith("{{^"))
                {
                    //open inverted group
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('^').Trim();

                    if (scopestack.Any() && scopestack.Peek().Item1 == token)
                    {
                        yield return new TokenPair(TokenType.ElementClose, Validated(token, templateString, m.Index, ref lines, ref parseErrors));
                    }
                    else
                    {
                        scopestack.Push(Tuple.Create(token, m.Index));
                    }
                    yield return new TokenPair(TokenType.InvertedElementOpen, Validated(token, templateString, m.Index, ref lines, ref parseErrors));
                }
                else if (m.Value.StartsWith("{{/"))
                {
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('/').Trim();
                    //close group
                    if (scopestack.Any() && scopestack.Peek().Item1 == token)
                    {
                        scopestack.Pop();
                        yield return new TokenPair(TokenType.ElementClose, Validated(token, templateString, m.Index, ref lines, ref parseErrors));
                    }
                    else
                    {
                        var location = HumanizeCharacterLocation(templateString, m.Index, ref lines);
                        parseErrors.Add(new IndexedParseException(location, "It appears that open and closing elements are mismatched."));
                    }
                }
                else if (m.Value.StartsWith("{{{") | m.Value.StartsWith("{{&"))
                {
                    //escaped single element
                    var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('&').Trim();
                    yield return new TokenPair(TokenType.UnescapedSingleValue, Validated(token, templateString, m.Index, ref lines, ref parseErrors));
                }
                else if (m.Value.StartsWith("{{!"))
                {
                    //it's a comment drop this on the floor, no need to even yield it.
                }
                else
                {
                    //unsingle value.
                    var token = m.Value.TrimStart('{').TrimEnd('}').Trim();
                    yield return new TokenPair(TokenType.EscapedSingleValue, Validated(token, templateString, m.Index, ref lines, ref parseErrors));
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
                var scopes = scopestack.Select(k =>
                {
                    var value = k.Item1.Trim('{', '#', '}');
                    if (value.StartsWith("each "))
                    {
                        value = value.Substring(5);
                    }
                    return new { scope = value, location = HumanizeCharacterLocation(templateString, k.Item2, ref lines) };
                }).Reverse()
                .ToArray();

                foreach (var unclosedScope in scopes)
                {
                    //var line = FindLineForLocation(templateString, m.Index, ref lines);
                    parseErrors.Add(new IndexedParseException(unclosedScope.location,
                        "A scope block to the following path was opened but not closed: '{0}', please close it using the appropriate syntax.",
                        unclosedScope.scope));
                }
            }
            #endregion

            //We want to throw an aggregate template exception, but in due time.
            if (parseErrors.Any())
            {
                var innerExceptions = parseErrors.OrderBy(k => k.LineNumber).ThenBy(k => k.CharacterOnLine).ToArray();
                throw new AggregateException(innerExceptions);
            }
            yield break;
        }

        /// <summary>
        /// Specifies combnations of paths that don't work.
        /// </summary>
        private static readonly Regex _negativePathSpec = new Regex("([.]{3,})|([^\\w./_]+)|((?<![.]{2})[/])|([.]{2,}($|[^/]))", RegexOptions.Singleline | RegexOptions.Compiled);

        private static string Validated(string token, string content, int index, ref int[] lines, ref List<IndexedParseException> exceptions)
        {
            token = token.Trim();

            if (_negativePathSpec.Match(token).Success)
            {
                var location = HumanizeCharacterLocation(content, index, ref lines);
                exceptions.Add(new IndexedParseException(location, "The path '{0}' is not valid. Please see documentation for examples of valid paths.", token));
            }
            return token;
        }
    }
}
