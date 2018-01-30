#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

#endregion

namespace Mustachio
{
	public delegate Stream TemplateGenerationWithCancel(IDictionary<string, object> data, CancellationToken token);

	/// <summary>
	///     The main entry point for this library. Use the static "Parse" methods to create template functions.
	///     Functions are safe for reuse, so you may parse and cache the resulting function.
	/// </summary>
	public class Parser
	{
		private const int BufferSize = 2042;

		/// <summary>
		///	Parses the Template with the given options
		/// </summary>
		/// <param name="parsingOptions">a set of options</param>
		/// <returns></returns>
		public static ExtendedParseInformation ParseWithOptions(ParserOptions parsingOptions)
		{
			if (parsingOptions == null)
			{
				throw new ArgumentNullException("parsingOptions");
			}

			if (parsingOptions.SourceStream == null)
			{
				throw new ArgumentNullException("parsingOptions", "The given Stream is null");
			}

			if (!parsingOptions.SourceStream.CanWrite)
			{
				throw new InvalidOperationException("The stream is ReadOnly");
			}

			var tokens = new Queue<TokenPair>(Tokenizer.Tokenize(parsingOptions.Template));
			var inferredModel = new InferredTemplateModel();

			var internalTemplate = Parse(tokens, parsingOptions, parsingOptions.WithModelInference ? inferredModel : null);
			TemplateGenerationWithCancel template = (model, token) =>
			{
				using (var streamWriter = new StreamWriter(parsingOptions.SourceStream, parsingOptions.Encoding, BufferSize, true))
				{
					var context = new ContextObject
					{
						Value = model,
						Key = "",
						Options = parsingOptions,
						CancellationToken = token,
					};
					internalTemplate(streamWriter, context);
					streamWriter.Flush();
				}
				return parsingOptions.SourceStream;
			};

			var result = new ExtendedParseInformation
			{
				InferredModel = inferredModel,
				ParsedTemplateWithCancelation = template
			};

			return result;
		}

		private static Action<StreamWriter, ContextObject> Parse(Queue<TokenPair> tokens, ParserOptions options,
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
						buildArray.Add(HandleFormattingValue(currentToken, options, currentScope));
						break;
					case TokenType.PrintFormatted:
						buildArray.Add(PrintFormattedValues(currentToken, options, currentScope));
						break;
					case TokenType.PrintSelf:
						buildArray.Add(HandleSingleValue(currentToken, options, currentScope));
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

		private static bool StopOrAbortBuilding(ContextObject context)
		{
			if (context.AbortGeneration || context.CancellationToken.IsCancellationRequested)
			{
				return false;
			}
			return true;
		}

		private static Action<StreamWriter, ContextObject> PrintFormattedValues(TokenPair currentToken, ParserOptions options, InferredTemplateModel currentScope)
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
					value = context.Value.ToString();
				}
				HandleContent(value)(builder, context);
			};
		}

		private static Action<StreamWriter, ContextObject> HandleFormattingValue(TokenPair currentToken, ParserOptions options, InferredTemplateModel scope)
		{
			return (builder, context) =>
			{
				if (scope != null)
				{
					scope = scope.GetInferredModelForPath(currentToken.Value, InferredTemplateModel.UsedAs.Scalar);
				}

				if (context == null)
				{
					return;
				}
				var c = context.GetContextForPath(currentToken.Value);
				context.Value = c.Format(currentToken.FormatAs);
			};
		}

		private static string HtmlEncodeString(string context)
		{
			return HttpUtility.HtmlEncode(context);
		}

		private static Action<StreamWriter, ContextObject> HandleSingleValue(TokenPair token, ParserOptions options,
			InferredTemplateModel scope)
		{
			if (scope != null)
			{
				scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Scalar);
			}

			return (builder, context) =>
			{
				if (context != null)
				{
					//try to locate the value in the context, if it exists, append it.
					var c = context.GetContextForPath(token.Value);
					if (c.Value != null)
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
				}
			};
		}

		internal static void WriteContent(StreamWriter builder, string content, ContextObject context)
		{
			content = content ?? context.Options.Null;

			var sourceCount = builder.BaseStream.Length;
			var binaryContent = context.Options.Encoding.GetBytes(content);

			var cl = binaryContent.Length;
			if (context.Options.MaxSize == 0)
			{
				builder.BaseStream.Write(binaryContent, 0, binaryContent.Length);
				return;
			}

			if (sourceCount >= context.Options.MaxSize - 1)
			{
				context.AbortGeneration = true;
				return;
			}
			var overflow = sourceCount + cl - context.Options.MaxSize;
			if (overflow < 0)
			{
				builder.BaseStream.Write(binaryContent, 0, binaryContent.Length);
				return;
			}
			if (overflow < content.Length)
			{
				builder.BaseStream.Write(binaryContent, 0, (int) (cl - overflow));
			}
			else
			{
				builder.BaseStream.Write(binaryContent, 0, binaryContent.Length);
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
			if (scope != null)
			{
				scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);
			}

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

		private static Action<StreamWriter, ContextObject> HandleCollectionOpen(TokenPair token, Queue<TokenPair> remainder,
			ParserOptions options, InferredTemplateModel scope)
		{
			if (scope != null)
			{
				scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Collection);
			}

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

				if (c.Value is IEnumerable && !(c.Value is string) && !(c.Value is IDictionary<string, object>))
				{
					var index = 0;
					var enumumerator = ((IEnumerable)c.Value).GetEnumerator();
					if (enumumerator.MoveNext())
					{
						var current = enumumerator.Current;
						object next;
						do
						{
							if (enumumerator.MoveNext())
							{
								next = enumumerator.Current;
							}
							else
							{
								next = null;
							}
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

					//var collection = (c.Value as IEnumerable).Cast<object>().ToArray();
					//for (int i = 0; i < collection.Length; i++)
					//{
					//	var innerContext = new ContextCollection(i, i == collection.Length)
					//	{
					//		Value = i,
					//		Key = string.Format("[{0}]", i),
					//		Options = options,
					//		Parent = c
					//	};
					//	innerTemplate(builder, innerContext);
					//}
				}
				else
				{
					throw new IndexedParseException(
					"'{0}' is used like an array by the template, but is a scalar value or object in your model.", token.Value);
				}
			};
		}

		private static Action<StreamWriter, ContextObject> HandleElementOpen(TokenPair token, Queue<TokenPair> remainder,
			ParserOptions options, InferredTemplateModel scope)
		{
			if (scope != null)
			{
				scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);
			}

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