using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Morestachio.Formatter;

namespace Morestachio.Framework
{
	/// <summary>
	///     Reads in a mustache template and lexes it into tokens.
	/// </summary>
	/// <exception cref="IndexedParseException"></exception>
	internal class Tokenizer
	{
		private static readonly Regex TokenFinder = new Regex("([{]{2}[^{}]+?[}]{2})|([{]{3}[^{}]+?[}]{3})",
			RegexOptions.Compiled);

		private static readonly Regex FormatFinder
			= new Regex(@"(?:([\w.|$]+)*)+(\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\))"
				, RegexOptions.Compiled);

		private static readonly Regex FormatInExpressionFinder
			= new Regex(@"(\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\))"
				, RegexOptions.Compiled);

		private static readonly Regex NewlineFinder
			= new Regex("\n", RegexOptions.Compiled);

		private static readonly Regex FindSplitterRegEx
			= new Regex(
				@"(?!\s*$)\s*(?:'([^'\\]*(?:\\[\S\s][^'\\]*)*)'|""([^""\\]*(?:\\[\S\s][^""\\]*)*)""|([^,'""\s\\]*(?:\s+[^,'""\s\\]+)*))\s*(?:,|$)",
				RegexOptions.Compiled);

		private static readonly Regex NameFinder
			= new Regex(@"(\[[\w]*\])",
				RegexOptions.Compiled);

		/// <summary>
		///     Specifies combinations of paths that don't work.
		/// </summary>
		private static readonly Regex NegativePathSpec =
			new Regex(@"([.]{4,})|([^\w./_$?]+)|((?<![.]{2})[/])|([.]{2,}($|[^/]))",
				RegexOptions.Singleline | RegexOptions.Compiled);

		private static CharacterLocation HumanizeCharacterLocation(string content, int characterIndex, List<int> lines)
		{
			if (lines == null)
			{
				lines = new List<int>();
				lines.AddRange(NewlineFinder.Matches(content).OfType<Match>().Select(k => k.Index));
			}

			var line = Array.BinarySearch(lines.ToArray(), characterIndex);
			line = line < 0 ? ~line : line;

			var charIdx = characterIndex;
			//in both of these cases, we want to increment the char index by one to account for the '\n' that is skipped in the indexes.
			if (line < lines.Count && line > 0)
			{
				charIdx = characterIndex - (lines[line - 1] + 1);
			}
			else if (line > 0)
			{
				charIdx = characterIndex - (lines.LastOrDefault() + 1);
			}

			var retval = new CharacterLocation
			{
				//Humans count from 1, so let's do that, too (hence the "+1" on these).
				Line = line + 1,
				Character = charIdx + 1
			};
			return retval;
		}

		private static FormatterPart[] TokenizeFormatterHeader(string formatString)
		{
			var preMatch = 0;
			return FindSplitterRegEx
				.Matches(formatString)
				.OfType<Match>()
				.Select(e =>
				{
					var indexOfEndMatch = e.Groups[0].Captures[0].Index + e.Groups[0].Captures[0].Length; //get everything from the index of the regex to its end
					var formatterArgument = formatString.Substring(preMatch, indexOfEndMatch - preMatch);
					var name = NameFinder.Match(formatterArgument); //find the optional [Name] attribute on the formatters argument
					preMatch = indexOfEndMatch;
					var argument = formatterArgument.Remove(name.Index, name.Value.Length)
						//trim all commas from the formatter
						.Trim(',')
						//then trim all spaces that the user might have written
						.Trim()
						//then trim all quotes the user might have written for escaping. this will preserve the spaces inside the quotes
						.Trim('"', '\'');
					return new FormatterPart(name.Value.Trim('[', ']'), argument);
				})
				.Where(e => !string.IsNullOrWhiteSpace(e.Argument))
				.ToArray();
		}

		private static IEnumerable<TokenPair> TokenizeFormattables(string token, string templateString, Capture m,
			List<int> lines, List<IndexedParseException> parseErrors)
		{
			var tokesHandeld = 0;
			foreach (Match tokenFormats in FormatFinder.Matches(token))
			{
				var found = tokenFormats.Groups[0].Value;
				var scalarValue = tokenFormats.Groups[1].Value;
				var formatterArgument = tokenFormats.Groups[2].Value;

				if (string.IsNullOrEmpty(scalarValue))
				{
					continue;
				}

				tokesHandeld += found.Trim().Length;
				if (string.IsNullOrWhiteSpace(formatterArgument))
				{
					yield return new TokenPair(TokenType.Format,
						Validated(scalarValue, templateString, m.Index, lines, parseErrors));
				}
				else
				{
					yield return new TokenPair(TokenType.Format,
						ValidateArgumentHead(scalarValue, formatterArgument, templateString,
							m.Index, lines, parseErrors))
					{
						FormatString = TokenizeFormatterHeader(formatterArgument.Substring(1, formatterArgument.Length - 2))
					};
				}
			}

			if (tokesHandeld != token.Length)
			{
				yield return new TokenPair(TokenType.Format,
					Validated(token.Substring(tokesHandeld), templateString, m.Index, lines, parseErrors));
			}
		}

		public static IEnumerable<TokenPair> Tokenize(ParserOptions parserOptions)
		{
			var templateString = parserOptions.Template;
			var matches = TokenFinder.Matches(templateString);
			var scopestack = new Stack<Tuple<string, int>>();

			var idx = 0;

			var parseErrors = new List<IndexedParseException>();
			var lines = new List<int>();
			var partialsNames = new List<string>();

			foreach (Match m in matches)
			{
				//yield front content.
				if (m.Index > idx)
				{
					yield return new TokenPair(TokenType.Content, templateString.Substring(idx, m.Index - idx));
				}

				if (m.Value.StartsWith("{{#declare"))
				{
					scopestack.Push(Tuple.Create(m.Value, m.Index));
					var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim().Substring("declare".Length);
					if (string.IsNullOrWhiteSpace(token))
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							@"The syntax to open the 'declare' block should be: '{{{{#declare name}}}}'. Missing the Name."));
					}
					else
					{
						partialsNames.Add(token);
						yield return new TokenPair(TokenType.PartialOpen, token);
					}
				}
				else if (m.Value.StartsWith("{{/declare"))
				{
					if (m.Value != "{{/declare}}")
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							@"The syntax to close the 'declare' block should be: '{{{{/declare}}}}'."));
					}
					else if (scopestack.Any() && scopestack.Peek().Item1.StartsWith("{{#declare"))
					{
						var token = scopestack.Pop().Item1.TrimStart('{').TrimEnd('}').TrimStart('#').Trim()
							.Substring("declare".Length);
						yield return new TokenPair(TokenType.PartialClose, token);
					}
					else
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							@"An 'declare' block is being closed, but no corresponding opening element ('{{{{#declare <name>}}}}') was detected."));
					}
				}
				else if (m.Value.StartsWith("{{#include"))
				{
					var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim().Substring("include".Length);
					if (string.IsNullOrWhiteSpace(token) || !partialsNames.Contains(token))
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							@"The syntax to use the 'include' statement should be: '{{{{/include name}}}}'.
There is no Partial declared '{0}'.
Partial names are case sensitive and must be declared before an include.", token));
					}
					else
					{
						yield return new TokenPair(TokenType.RenderPartial, token);
					}
				}
				else if (m.Value.StartsWith("{{#each"))
				{
					scopestack.Push(Tuple.Create(m.Value, m.Index));
					var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim().Substring(4);

					if (token.StartsWith(" ") && token.Trim() != "")
					{
						token = token.Trim();
						if (FormatInExpressionFinder.IsMatch(token))
						{
							foreach (var tokenizeFormattable in TokenizeFormattables(token, templateString, m, lines,
								parseErrors))
							{
								yield return tokenizeFormattable;
							}

							yield return new TokenPair(TokenType.CollectionOpen, ".");
						}
						else
						{
							yield return new TokenPair(TokenType.CollectionOpen,
								Validated(token, templateString, m.Index, lines, parseErrors).Trim());
						}
					}
					else
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							@"The 'each' block being opened requires a model path to be specified in the form '{{{{#each <name>}}}}'."));
					}
				}
				else if (m.Value.StartsWith("{{/each"))
				{
					if (m.Value != "{{/each}}")
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							@"The syntax to close the 'each' block should be: '{{{{/each}}}}'."));
					}
					else if (scopestack.Any() && scopestack.Peek().Item1.StartsWith("{{#each"))
					{
						var token = scopestack.Pop().Item1;
						yield return new TokenPair(TokenType.CollectionClose, token);
					}
					else
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							@"An 'each' block is being closed, but no corresponding opening element ('{{{{#each <name>}}}}') was detected."));
					}
				}
				else if (m.Value.StartsWith("{{#"))
				{
					//open group
					var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('#').Trim();


					if (scopestack.Any() && scopestack.Peek().Item1 == token)
					{
						yield return new TokenPair(TokenType.ElementClose,
							Validated(token, templateString, m.Index, lines, parseErrors));
					}
					else
					{
						scopestack.Push(Tuple.Create(token, m.Index));
					}

					yield return new TokenPair(TokenType.ElementOpen,
						Validated(token, templateString, m.Index, lines, parseErrors));
				}
				else if (m.Value.StartsWith("{{^"))
				{
					//open inverted group
					var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('^').Trim();

					if (scopestack.Any() && scopestack.Peek().Item1 == token)
					{
						yield return new TokenPair(TokenType.ElementClose,
							Validated(token, templateString, m.Index, lines, parseErrors));
					}
					else
					{
						scopestack.Push(Tuple.Create(token, m.Index));
					}

					yield return new TokenPair(TokenType.InvertedElementOpen,
						Validated(token, templateString, m.Index, lines, parseErrors));
				}
				else if (m.Value.StartsWith("{{/"))
				{
					var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('/').Trim();
					//close group
					if (scopestack.Any() && scopestack.Peek().Item1 == token)
					{
						scopestack.Pop();
						yield return new TokenPair(TokenType.ElementClose,
							Validated(token, templateString, m.Index, lines, parseErrors));
					}
					else
					{
						var location = HumanizeCharacterLocation(templateString, m.Index, lines);
						parseErrors.Add(new IndexedParseException(location,
							"It appears that open and closing elements are mismatched."));
					}
				}
				else if (m.Value.StartsWith("{{{") | m.Value.StartsWith("{{&"))
				{
					//escaped single element
					var token = m.Value.TrimStart('{').TrimEnd('}').TrimStart('&').Trim();
					yield return new TokenPair(TokenType.UnescapedSingleValue,
						Validated(token, templateString, m.Index, lines, parseErrors));
				}
				else if (m.Value.StartsWith("{{!"))
				{
					//it's a comment drop this on the floor, no need to even yield it.
				}
				else
				{
					//unsingle value.
					var token = m.Value.TrimStart('{').TrimEnd('}').Trim();
					if (FormatInExpressionFinder.IsMatch(token))
					{
						foreach (var tokenizeFormattable in TokenizeFormattables(token, templateString, m, lines,
							parseErrors).ToArray())
						{
							yield return tokenizeFormattable;
						}

						yield return new TokenPair(TokenType.PrintFormatted, ".");
					}
					else
					{
						yield return new TokenPair(TokenType.EscapedSingleValue,
							Validated(token, templateString, m.Index, lines, parseErrors));
					}
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

						return new
						{
							scope = value,
							location = HumanizeCharacterLocation(templateString, k.Item2, lines)
						};
					}).Reverse()
					.ToArray();

				parseErrors.AddRange(scopes.Select(unclosedScope => new IndexedParseException(unclosedScope.location,
					"A scope block to the following path was opened but not closed: '{0}', please close it using the appropriate syntax.",
					unclosedScope.scope)));
			}

			#endregion

			//We want to throw an aggregate template exception, but in due time.
			if (!parseErrors.Any())
			{
				yield break;
			}

			var innerExceptions = parseErrors.OrderBy(k => k.LineNumber).ThenBy(k => k.CharacterOnLine).ToArray();
			throw new AggregateException(innerExceptions);
		}

		private static string Validated(string token, string content, int index, List<int> lines,
			List<IndexedParseException> exceptions)
		{
			token = token.Trim();

			if (!NegativePathSpec.Match(token).Success)
			{
				return token;
			}

			var location = HumanizeCharacterLocation(content, index, lines);
			exceptions.Add(new IndexedParseException(location,
				"The path '{0}' on line:char '{1}:{2}' is not valid. Please see documentation for examples of valid paths.", token, location.Line, location.Character));

			return token;
		}

		private static string ValidateArgumentHead(string token, string argument, string content,
			int index, List<int> lines, List<IndexedParseException> exceptions)
		{
			token = token.Trim();

			Validated(token, content, index, lines, exceptions);

			//if (!PositiveArgumentSpec.Match(argument).Success)
			//{
			//	var location = HumanizeCharacterLocation(content, index, lines);
			//	exceptions.Add(new IndexedParseException(location,
			//		"The argument '{0}' is not valid. Please see documentation for examples of valid paths.", token));
			//}

			return token;
		}

		internal class CharacterLocation
		{
			public int Line { get; set; }
			public int Character { get; set; }

			public override string ToString()
			{
				return $"Line: {Line}, Column: {Character}";
			}
		}
	}
}