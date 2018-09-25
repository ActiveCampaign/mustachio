#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using JetBrains.Annotations;

#endregion

namespace Morestachio
{
	/// <summary>
	///     The main entry point for this library. Use the static "Parse" methods to create template functions.
	///     Functions are safe for reuse, so you may parse and cache the resulting function.
	/// </summary>
	public class Parser
	{
		private const int BufferSize = 2024;

		/// <summary>
		///     Parses the Template with the given options
		/// </summary>
		/// <param name="parsingOptions">a set of options</param>
		/// <returns></returns>
		[ContractAnnotation("parsingOptions:null => halt")]
		[NotNull]
		[MustUseReturnValue("Use return value to create templates. Reuse return value if possible.")]
		[Pure]
		public static ExtendedParseInformation ParseWithOptions([NotNull]ParserOptions parsingOptions)
		{
			if (parsingOptions == null)
			{
				throw new ArgumentNullException(nameof(parsingOptions));
			}

			if (parsingOptions.SourceFactory == null)
			{
				throw new ArgumentNullException(nameof(parsingOptions), "The given Stream is null");
			}

			var tokens = new Queue<TokenPair>(Tokenizer.Tokenize(parsingOptions.Template));
			var inferredModel = new InferredTemplateModel();

			var extendedParseInformation = new ExtendedParseInformation(inferredModel, parsingOptions, tokens);

			if (parsingOptions.WithModelInference)
			{
				//we preparse the template once to get the model
				var s = extendedParseInformation.InternalTemplate.Value;
			}

			return extendedParseInformation;
		}

		/// <summary>
		///		Uses the <seealso cref="ExtendedParseInformation"/> object to parse the data to a template.
		/// </summary>
		/// <param name="parseOutput">The parse output.</param>
		/// <param name="data">The data.</param>
		/// <param name="token">The token.</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">The stream is ReadOnly</exception>
		[MustUseReturnValue("The Stream contains the template. Use Stringify(Encoding) to get the string of it")]
		[NotNull]
		public static Stream CreateTemplateStream([NotNull]ExtendedParseInformation parseOutput, [NotNull]object data, CancellationToken token)
		{
			var sourceStream = parseOutput.ParserOptions.SourceFactory();
			if (!sourceStream.CanWrite)
			{
				throw new InvalidOperationException("The stream is ReadOnly");
			}

			using (var streamWriter = new StreamWriter(sourceStream, parseOutput.ParserOptions.Encoding, BufferSize, true))
			{
				var context = new ContextObject
				{
					Value = data,
					Key = "",
					Options = parseOutput.ParserOptions,
					CancellationToken = token
				};
				parseOutput.InternalTemplate.Value(streamWriter, context);
				streamWriter.Flush();
			}

			return sourceStream;
		}

		internal static Action<StreamWriter, ContextObject> Parse(Queue<TokenPair> tokens, ParserOptions options,
			InferredTemplateModel currentScope = null)
		{
			var buildArray = new List<Action<StreamWriter, ContextObject>>();

			while (tokens.Any())
			{
				var currentToken = tokens.Dequeue();
				switch (currentToken.Type)
				{
					case TokenType.Comment:
						break;
					case TokenType.Content:
						buildArray.Add(HandleContent(currentToken.Value));
						break;
					case TokenType.CollectionOpen:
						buildArray.Add(HandleCollectionOpen(currentToken, tokens, options, currentScope));
						break;
					case TokenType.ElementOpen:
						buildArray.Add(HandleElementOpen(currentToken, tokens, options, currentScope));
						break;
					case TokenType.InvertedElementOpen:
						buildArray.Add(HandleInvertedElementOpen(currentToken, tokens, options, currentScope));
						break;
					case TokenType.CollectionClose:
					case TokenType.ElementClose:
						// This should immediately return if we're in the element scope,
						// and if we're not, this should have been detected by the tokenizer!
						return (builder, context) =>
						{
							foreach (var a in buildArray.TakeWhile(e => StopOrAbortBuilding(context)))
							{
								a(builder, context);
							}
						};
					case TokenType.Format:
						buildArray.Add(ParseFormatting(currentToken, tokens, options, currentScope));
						break;
					case TokenType.EscapedSingleValue:
					case TokenType.UnescapedSingleValue:
						buildArray.Add(HandleSingleValue(currentToken, options, currentScope));
						break;
				}
			}

			return (builder, context) =>
			{
				foreach (var a in buildArray.TakeWhile(e => StopOrAbortBuilding(context)))
				{
					a(builder, context);
				}
			};
		}

		private static Action<StreamWriter, ContextObject> ParseFormatting(TokenPair token, Queue<TokenPair> tokens,
			ParserOptions options, InferredTemplateModel currentScope = null)
		{
			var buildArray = new List<Action<StreamWriter, ContextObject>>();

			buildArray.Add(HandleFormattingValue(token, options, currentScope));
			var nonPrintToken = false;
			while (tokens.Any() && !nonPrintToken)
			{
				var currentToken = tokens.Peek();
				switch (currentToken.Type)
				{
					case TokenType.Format:
						buildArray.Add(HandleFormattingValue(tokens.Dequeue(), options, currentScope));
						break;
					case TokenType.PrintFormatted:
						buildArray.Add(PrintFormattedValues(tokens.Dequeue(), options, currentScope));
						break;
					case TokenType.CollectionOpen:
						buildArray.Add(HandleCollectionOpen(tokens.Dequeue(), tokens, options, currentScope));
						break;
					default:
						//The folloring cannot be formatted and the result of the formatting operation has used.
						//continue with the original Context
						nonPrintToken = true;
						break;
				}
			}

			return (builder, context) =>
			{
				//the formatting will may change the object. Clone the current Context to leave the root one untouched
				var contextClone = context.Clone();
				foreach (var a in buildArray.TakeWhile(e => StopOrAbortBuilding(context)))
				{
					a(builder, contextClone);
				}
			};
		}

		private static bool StopOrAbortBuilding(ContextObject context)
		{
			return !context.AbortGeneration && !context.CancellationToken.IsCancellationRequested;
		}

		private static Action<StreamWriter, ContextObject> PrintFormattedValues(TokenPair currentToken,
			ParserOptions options,
			InferredTemplateModel currentScope)
		{
			return (builder, context) =>
			{
				if (context == null)
				{
					return;
				}

				string value = null;
				if (context.Value != null)
				{
					value = context.ToString();
				}

				HandleContent(value)(builder, context);
			};
		}

		private static Action<StreamWriter, ContextObject> HandleFormattingValue(TokenPair currentToken,
			ParserOptions options, InferredTemplateModel scope)
		{
			return (builder, context) =>
			{
				scope = scope?.GetInferredModelForPath(currentToken.Value, InferredTemplateModel.UsedAs.Scalar);

				if (context == null)
				{
					return;
				}

				var c = context.GetContextForPath(currentToken.Value);
				if (!string.IsNullOrWhiteSpace(currentToken.FormatString))
				{
					//if pre and suffixed by a $ its a reference to another field.
					//walk the path in the $ and use the value in the formatter
					if (currentToken.FormatString.StartsWith("$") &&
					    currentToken.FormatString.EndsWith("$"))
					{
						var formatContext = context.GetContextForPath(currentToken.FormatString.Trim('$'));
						context.Value = c.Format(formatContext.Value);
					}
					else
					{
						context.Value = c.Format(currentToken.FormatString);
					}
				}
				else
				{
					context.Value = c.Format(currentToken.FormatString);
				}
			};
		}

		private static string HtmlEncodeString(string context)
		{
			return HttpUtility.HtmlEncode(context);
		}

		private static Action<StreamWriter, ContextObject> HandleSingleValue(TokenPair token, ParserOptions options,
			InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Scalar);

			return (builder, context) =>
			{
				//try to locate the value in the context, if it exists, append it.
				var c = context?.GetContextForPath(token.Value);
				if (c?.Value != null)
				{
					if (token.Type == TokenType.EscapedSingleValue && !options.DisableContentEscaping)
					{
						HandleContent(HtmlEncodeString(c.ToString()))(builder, c);
					}
					else
					{
						HandleContent(c.ToString())(builder, c);
					}
				}
			};
		}

		internal static void WriteContent(StreamWriter builder, string content, ContextObject context)
		{
			content = content ?? context.Options.Null;

			var sourceCount = builder.BaseStream.Length;
		
			if (context.Options.MaxSize == 0)
			{
				builder.Write(content);
				return;
			}

			if (sourceCount >= context.Options.MaxSize - 1)
			{
				context.AbortGeneration = true;
				return;
			}

			var cl = context.Options.Encoding.GetByteCount(content);

			var overflow = sourceCount + cl - context.Options.MaxSize;
			if (overflow < 0)
			{
				//builder.BaseStream.Write(binaryContent, 0, binaryContent.Length);
				builder.Write(content);
				return;
			}

			if (overflow < content.Length)
			{
				builder.Write(content.ToCharArray(0, (int) (cl - overflow)));
			}
			else
			{
				builder.Write(content.ToCharArray(0, cl));
			}
		}

		private static Action<StreamWriter, ContextObject> HandleContent(string token)
		{
			return (builder, context) => { WriteContent(builder, token, context); };
		}

		private static Action<StreamWriter, ContextObject> HandleInvertedElementOpen(TokenPair token,
			Queue<TokenPair> remainder,
			ParserOptions options, InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);

			var innerTemplate = Parse(remainder, options, scope);

			return (builder, context) =>
			{
				var c = context.GetContextForPath(token.Value);
				//"falsey" values by Javascript standards...
				if (!c.Exists())
				{
					innerTemplate(builder, c);
				}
			};
		}


		private static Action<StreamWriter, ContextObject> HandleCollectionOpen(TokenPair token,
			Queue<TokenPair> remainder,
			ParserOptions options, InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Collection);

			var innerTemplate = Parse(remainder, options, scope);

			return (builder, context) =>
			{
				//if we're in the same scope, just negating, then we want to use the same object
				var c = context.GetContextForPath(token.Value);

				//"falsey" values by Javascript standards...
				if (!c.Exists())
				{
					return;
				}

				var value = c.Value as IEnumerable;
				if (value != null && !(value is string) && !(value is IDictionary<string, object>))
				{
					//Use this "lookahead" enumeration to allow the $last keyword
					var index = 0;
					var enumumerator = value.GetEnumerator();
					if (!enumumerator.MoveNext())
					{
						return;
					}

					var current = enumumerator.Current;
					do
					{
						var next = enumumerator.MoveNext() ? enumumerator.Current : null;
						var innerContext = new ContextCollection(index, next == null)
						{
							Value = current,
							Key = string.Format("[{0}]", index),
							Options = options,
							Parent = c
						};
						innerTemplate(builder, innerContext);
						index++;
						current = next;
					} while (current != null);
				}
				else
				{
					throw new IndexedParseException(
						"'{0}' is used like an array by the template, but is a scalar value or object in your model.",
						token.Value);
				}
			};
		}

		private static Action<StreamWriter, ContextObject> HandleElementOpen(TokenPair token,
			Queue<TokenPair> remainder,
			ParserOptions options, InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);

			var innerTemplate = Parse(remainder, options, scope);

			return (builder, context) =>
			{
				var c = context.GetContextForPath(token.Value);
				//"falsey" values by Javascript standards...
				if (c.Exists())
				{
					innerTemplate(builder, c);
				}
			};
		}
	}
}