#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Morestachio.Framework;

#endregion

namespace Morestachio
{

	/// <summary>
	///     The main entry point for this library. Use the static "Parse" methods to create template functions.
	///     Functions are safe for reuse, so you may parse and cache the resulting function.
	/// </summary>
	public class Parser
	{
		/// <summary>
		///		Defines a Action that cannot be awaited produced by the Parser
		/// </summary>
		/// <param name="outputStream">The output stream.</param>
		/// <param name="context">The context.</param>
		private delegate void SyncParserAction(ByteCounterStreamWriter outputStream, ContextObject context);

		/// <summary>
		///		Defines a Action that can be Awaited, produced by the Parser
		/// </summary>
		/// <param name="outputStream">The output stream.</param>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		internal delegate Task AsyncParserAction(ByteCounterStreamWriter outputStream, ContextObject context);

		private const int BufferSize = 2024;

		private class ParserActions
		{
			public ParserActions()
			{
				Elements = new List<Delegate>();
				Partials = new Dictionary<string, AsyncParserAction>();
			}

			public ICollection<Delegate> Elements { get; }

			public IDictionary<string, AsyncParserAction> Partials { get; set; }

			public void MakeAction(SyncParserAction syncAction)
			{
				Elements.Add(syncAction);
			}

			public void MakeAction(AsyncParserAction syncAction)
			{
				Elements.Add(syncAction);
			}

			public async Task ExecuteWith(ByteCounterStreamWriter builder, ContextObject context)
			{
				foreach (var a in Elements.TakeWhile(e => StopOrAbortBuilding(builder, context)))
				{
					if (a is SyncParserAction action)
					{
						action(builder, context);
					}
					else if (a is AsyncParserAction asyncAction)
					{
						await asyncAction(builder, context);
					}
					else
					{
						throw new InvalidOperationException($"The internal parser action was of a not recognized type '{a.GetType()}'");
					}
				}
			}
		}

		/// <summary>
		///		Hosts all infos about the current execution path of a part in the Template.
		///		Can be used for future parallel execution
		/// </summary>
		internal class ScopeData
		{
			public ScopeData()
			{
				Partials = new Dictionary<string, AsyncParserAction>();
				PartialDepth = new Stack<string>();
			}

			public IDictionary<string, AsyncParserAction> Partials { get; private set; }

			public Stack<string> PartialDepth { get; private set; }
		}

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

			var tokens = new Queue<TokenPair>(Tokenizer.Tokenize(parsingOptions));
			var inferredModel = new InferredTemplateModel();

			var extendedParseInformation = new ExtendedParseInformation(inferredModel, parsingOptions, tokens);

			if (parsingOptions.WithModelInference)
			{
				//we pre-parse the template once to get the model
				//ReSharper disable once UnusedVariable
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
		public static async Task<Stream> CreateTemplateStreamAsync([NotNull]ExtendedParseInformation parseOutput, [NotNull]object data, CancellationToken token)
		{
			var timeoutCancellation = new CancellationTokenSource();
			if (parseOutput.ParserOptions.Timeout != TimeSpan.Zero)
			{
				timeoutCancellation.CancelAfter(parseOutput.ParserOptions.Timeout);
				var anyCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCancellation.Token);
				token = anyCancellationToken.Token;
			}
			var sourceStream = parseOutput.ParserOptions.SourceFactory();
			try
			{
				if (!sourceStream.CanWrite)
				{
					throw new InvalidOperationException("The stream is ReadOnly");
				}

				using (var byteCounterStreamWriter = new ByteCounterStreamWriter(sourceStream,
					parseOutput.ParserOptions.Encoding, BufferSize, true))
				{
					var context = new ContextObject(parseOutput.ParserOptions, "")
					{
						Value = data,
						CancellationToken = token
					};
					await parseOutput.InternalTemplate.Value(byteCounterStreamWriter, context);
				}

				if (timeoutCancellation.IsCancellationRequested)
				{
					sourceStream.Dispose();
					throw new TimeoutException($"The requested timeout of {parseOutput.ParserOptions.Timeout:g} for report generation was reached");
				}
			}
			catch
			{
				//If there is any exception while generating the template we must dispose any data written to the stream as it will never returned and might 
				//create a memory leak with this. This is also true for a timeout
				sourceStream.Dispose();
				throw;
			}
			return sourceStream;
		}

		internal static AsyncParserAction Parse(Queue<TokenPair> tokens, ParserOptions options, ScopeData scopeData,
			InferredTemplateModel currentScope = null)
		{
			var buildArray = new ParserActions();

			while (tokens.Any())
			{
				var currentToken = tokens.Dequeue();
				switch (currentToken.Type)
				{
					case TokenType.Comment:
						break;
					case TokenType.Content:
						buildArray.MakeAction(HandleContent(currentToken.Value));
						break;
					case TokenType.CollectionOpen:
						buildArray.MakeAction(HandleCollectionOpen(currentToken, tokens, options, scopeData, currentScope));
						break;
					case TokenType.ElementOpen:
						buildArray.MakeAction(HandleElementOpen(currentToken, tokens, options, scopeData, currentScope));
						break;
					case TokenType.InvertedElementOpen:
						buildArray.MakeAction(HandleInvertedElementOpen(currentToken, tokens, options, scopeData, currentScope));
						break;
					case TokenType.CollectionClose:
					case TokenType.ElementClose:
						// This should immediately return if we're in the element scope,
						// -and if we're not, this should have been detected by the tokenizer!
						return async (builder, context) =>
						{
							await buildArray.ExecuteWith(builder, context);
						};
					case TokenType.Format:
						buildArray.MakeAction(ParseFormatting(currentToken, tokens, options, scopeData, currentScope));
						break;
					case TokenType.EscapedSingleValue:
					case TokenType.UnescapedSingleValue:
						buildArray.MakeAction(HandleSingleValue(currentToken, options, scopeData, currentScope));
						break;
					case TokenType.PartialOpen:
						// currently same named partials will override each other
						// to allow recursive calls of partials we first have to declare the partial and then load it as we would parse
						// -the partial as a whole and then add it to the list would lead to unknown calls of partials inside the partial
						AsyncParserAction handlePartialDeclaration = null;
						scopeData.Partials[currentToken.Value] = async (outputStream, context) =>
						{
							if (handlePartialDeclaration != null)
							{
								await handlePartialDeclaration(outputStream, context);
							}
							else
							{
								throw new InvalidOperationException($"Partial '{currentToken.Value}' was executed before created was completed.");
							}
						};
						handlePartialDeclaration = HandlePartialDeclaration(currentToken, tokens, options, scopeData, currentScope);
						break;
					case TokenType.RenderPartial:
						var partialName = currentToken.Value;
						var partialCode = scopeData.Partials[partialName];
						buildArray.MakeAction(async (a, f) =>
						{
							var currentPartial = partialName + "_" + scopeData.PartialDepth.Count;
							scopeData.PartialDepth.Push(currentPartial);
							if (scopeData.PartialDepth.Count >= options.PartialStackSize)
							{
								throw new MustachioStackOverflowException(
									$"You have exceeded the maximum stack Size for nested Partial calls of '{options.PartialStackSize}'. See Data for call stack")
								{
									Data =
									{
										{"Callstack", scopeData.PartialDepth}
									}
								};
							}

							await partialCode(a, f);
							scopeData.PartialDepth.Pop();
						});
						break;
				}
			}

			return async (builder, context) =>
			{
				await buildArray.ExecuteWith(builder, context);
			};
		}

		private static AsyncParserAction HandlePartialDeclaration(TokenPair currentToken,
			Queue<TokenPair> tokens,
			ParserOptions options,
			ScopeData scopeData,
			InferredTemplateModel currentScope)
		{
			var partialTokens = new Queue<TokenPair>();
			var token = currentToken;
			while (tokens.Any() &&
				   (token.Type != TokenType.PartialClose || token.Value != currentToken.Value)) //just look for the closing tag and buffer it seperate
			{
				token = tokens.Dequeue();
				partialTokens.Enqueue(token);
			}
			return Parse(partialTokens, options, scopeData, currentScope); //we have taken everything from the partial and created a executable function for it
		}

		private static AsyncParserAction ParseFormatting(TokenPair token, Queue<TokenPair> tokens,
			ParserOptions options, ScopeData scopeData, InferredTemplateModel currentScope = null)
		{
			var buildArray = new ParserActions();
			buildArray.MakeAction(HandleFormattingValue(token, currentScope, scopeData));

			var nonPrintToken = false;
			while (tokens.Any() && !nonPrintToken) //only take as few tokens we need for formatting. 
			{
				var currentToken = tokens.Peek();
				switch (currentToken.Type)
				{
					case TokenType.Format:
						//this will invoke the formatter and copy the scope.
						//we must copy the scope as the formatting action might break our chain and we are no longer able to 
						//construct a valid path up
						//after that there is always a PrintFormatted type that will print the "current" scope and
						//reset it to the origial scope before we have entered the scope
						buildArray.MakeAction(HandleFormattingValue(tokens.Dequeue(), currentScope, scopeData));
						break;
					case TokenType.PrintFormatted:
						tokens.Dequeue(); //this must be the flow token type that has no real value execpt for a dot
						buildArray.MakeAction(PrintFormattedValues());
						break;
					case TokenType.CollectionOpen: //in this case we are in a formatting expression followed by a #each.
												   //after this we need to reset the context so handle the open here
						buildArray.MakeAction(HandleCollectionOpen(tokens.Dequeue(), tokens, options, scopeData, currentScope));
						break;
					default:
						//The following cannot be formatted and the result of the formatting operation has used.
						//continue with the original Context
						nonPrintToken = true;
						break;
				}
			}

			return async (builder, context) =>
			{
				//the formatting will may change the object. Clone the current Context to leave the root one untouched
				var contextClone = context.Clone();
				await buildArray.ExecuteWith(builder, contextClone);
			};
		}

		private static bool StopOrAbortBuilding(ByteCounterStreamWriter builder, ContextObject context)
		{
			return !context.AbortGeneration && !context.CancellationToken.IsCancellationRequested && !builder.ReachedLimit;
		}

		/// <summary>
		///		This Gets the Current context and calls RenderToString
		/// </summary>
		/// <returns></returns>
		private static AsyncParserAction PrintFormattedValues()
		{
			return async (builder, context) =>
			{
				if (context == null)
				{
					return;
				}

				string value = null;
				await context.EnsureValue();
				if (context.Value != null)
				{
					value = await context.RenderToString();
				}

				HandleContent(value)(builder, context);
			};
		}

		private static AsyncParserAction HandleFormattingValue(TokenPair currentToken, InferredTemplateModel scope,
			ScopeData scopeData)
		{
			return async (builder, context) =>
			{
				scope = scope?.GetInferredModelForPath(currentToken.Value, InferredTemplateModel.UsedAs.Scalar);

				if (context == null)
				{
					return;
				}
				var c = await context.GetContextForPath(currentToken.Value, scopeData);

				if (currentToken.FormatString != null && currentToken.FormatString.Any())
				{
					var argList = new List<KeyValuePair<string, object>>();

					foreach (var formatterArgument in currentToken.FormatString)
					{
						//if pre and suffixed by a $ its a reference to another field.
						//walk the path in the $ and use the value in the formatter
						var trimmedArg = formatterArgument.Argument.Trim();
						if (trimmedArg.StartsWith("$") &&
							trimmedArg.EndsWith("$"))
						{
							var formatContext = await context.GetContextForPath(trimmedArg.Trim('$'), scopeData);
							await formatContext.EnsureValue();
							argList.Add(new KeyValuePair<string, object>(formatterArgument.Name, formatContext.Value));
						}
						else
						{
							argList.Add(new KeyValuePair<string, object>(formatterArgument.Name, formatterArgument.Argument));
						}
					}
					//we do NOT await the task here. We await the task only if we need the value
					context.Value = c.Format(argList.ToArray());
				}
				else
				{
					context.Value = c.Format(new KeyValuePair<string, object>[0]);
				}
			};
		}

		private static string HtmlEncodeString(string context)
		{
			return WebUtility.HtmlEncode(context);
		}

		private static AsyncParserAction HandleSingleValue(TokenPair token, ParserOptions options,
			ScopeData scopeData,
			InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Scalar);

			return async (builder, context) =>
			{
				//try to locate the value in the context, if it exists, append it.
				var c = context != null ? (await context.GetContextForPath(token.Value, scopeData)) : null;
				if (c?.Value != null)
				{
					await c.EnsureValue();
					if (token.Type == TokenType.EscapedSingleValue && !options.DisableContentEscaping)
					{
						HandleContent(HtmlEncodeString(await c.RenderToString()))(builder, c);
					}
					else
					{
						HandleContent(await c.RenderToString())(builder, c);
					}
				}
			};
		}

		/// <summary>
		///		Internal class to ensure that the given limit of bytes to write is never extended to ensure template quotas
		/// </summary>
		/// <seealso cref="System.IDisposable" />
		internal class ByteCounterStreamWriter : IDisposable
		{
			public ByteCounterStreamWriter([NotNull] Stream stream, [NotNull] Encoding encoding, int bufferSize, bool leaveOpen)
			{
				BaseWriter = new StreamWriter(stream, encoding, bufferSize, leaveOpen);
			}

			public StreamWriter BaseWriter { get; set; }

			public long BytesWritten { get; set; }
			public bool ReachedLimit { get; set; }

			public void Write(string value, long sizeOfContent)
			{
				BytesWritten += sizeOfContent;
				BaseWriter.Write(value);
			}

			public void Write(string value)
			{
				BaseWriter.Write(value);
			}

			public void Write(char[] value, long sizeOfContent)
			{
				BytesWritten += sizeOfContent;
				BaseWriter.Write(value);
			}

			public void Dispose()
			{
				BaseWriter.Flush();
				BaseWriter.Dispose();
			}
		}

		private static void WriteContent(ByteCounterStreamWriter builder, string content, ContextObject context)
		{
			content = content ?? context.Options.Null;

			var sourceCount = builder.BytesWritten;

			if (context.Options.MaxSize == 0)
			{
				builder.Write(content);
				return;
			}

			if (sourceCount >= context.Options.MaxSize - 1)
			{
				builder.ReachedLimit = true;
				return;
			}
			//TODO this is a performance critical operation. As we might deal with variable-length encodings this cannot be compute initial
			var cl = context.Options.Encoding.GetByteCount(content);

			var overflow = sourceCount + cl - context.Options.MaxSize;
			if (overflow <= 0)
			{
				builder.Write(content, cl);
				return;
			}

			if (overflow < content.Length)
			{
				builder.Write(content.ToCharArray(0, (int)(cl - overflow)), cl - overflow);
			}
			else
			{
				builder.Write(content, cl);
			}
		}

		private static SyncParserAction HandleContent(string token)
		{
			return (builder, context) => { WriteContent(builder, token, context); };
		}

		private static AsyncParserAction HandleInvertedElementOpen(TokenPair token,
			Queue<TokenPair> remainder,
			ParserOptions options,
			ScopeData scopeData,
			InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);

			var innerTemplate = Parse(remainder, options, scopeData, scope);

			return async (builder, context) =>
			{
				var c = await context.GetContextForPath(token.Value, scopeData);
				//"falsey" values by Javascript standards...
				if (!await c.Exists())
				{
					await innerTemplate(builder, c);
				}
			};
		}


		private static AsyncParserAction HandleCollectionOpen(TokenPair token,
			Queue<TokenPair> remainder,
			ParserOptions options,
			ScopeData scopeData,
			InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Collection);

			var innerTemplate = Parse(remainder, options, scopeData, scope);

			return async (builder, context) =>
			{
				//if we're in the same scope, just negating, then we want to use the same object
				var c = await context.GetContextForPath(token.Value, scopeData);

				//"falsey" values by Javascript standards...
				if (!await c.Exists())
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
						var innerContext = new ContextCollection(index, next == null, options, $"[{index}]")
						{
							Value = current,
							Parent = c
						};
						await innerTemplate(builder, innerContext);
						index++;
						current = next;
					} while (current != null && StopOrAbortBuilding(builder, context));
				}
				else
				{
					throw new IndexedParseException(
						"'{0}' is used like an array by the template, but is a scalar value or object in your model.",
						token.Value);
				}
			};
		}

		private static AsyncParserAction HandleElementOpen(TokenPair token,
			Queue<TokenPair> remainder,
			ParserOptions options,
			ScopeData scopeData,
			InferredTemplateModel scope)
		{
			scope = scope?.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);

			var innerTemplate = Parse(remainder, options, scopeData, scope);

			return async (builder, context) =>
			{
				var c = await context.GetContextForPath(token.Value, scopeData);
				//"falsey" values by Javascript standards...
				if (await c.Exists())
				{
					await innerTemplate(builder, c);
				}
			};
		}
	}
}