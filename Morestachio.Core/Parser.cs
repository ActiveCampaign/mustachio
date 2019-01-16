#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Morestachio.Framework;

#endregion

namespace Morestachio
{
	/// <summary>
	///     The main entry point for this library. Use the static "Parse" methods to create template functions.
	///     Functions are safe for reuse, so you may parse and cache the resulting function.
	/// </summary>
	public static class Parser
	{
		/// <summary>
		///     Parses the Template with the given options
		/// </summary>
		/// <param name="parsingOptions">a set of options</param>
		/// <returns></returns>
		[ContractAnnotation("parsingOptions:null => halt")]
		[NotNull]
		[MustUseReturnValue("Use return value to create templates. Reuse return value if possible.")]
		[Pure]
		public static MorestachioDocumentInfo ParseWithOptions([NotNull]ParserOptions parsingOptions)
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
			var extendedParseInformation = new MorestachioDocumentInfo(parsingOptions, tokens);
			extendedParseInformation.Document = Parse(tokens, parsingOptions);
			return extendedParseInformation;
		}

		internal static IDocumentItem Parse(Queue<TokenPair> tokens, ParserOptions options)
		{
			var buildArray = new MorestachioDocument();
			while (tokens.Any())
			{
				var currentToken = tokens.Dequeue();
				switch (currentToken.Type)
				{
					case TokenType.Comment:
						break;
					case TokenType.Content:
						buildArray.Add(new ContentDocumentItem(currentToken.Value));
						break;
					case TokenType.CollectionOpen:
						buildArray.Add(new CollectionDocumentItem(Parse(tokens, options), currentToken.Value));
						break;
					case TokenType.ElementOpen:
						var singleElement = new DocumentScopeItem(currentToken.Value);
						singleElement.Add(Parse(tokens, options));
						buildArray.Add(singleElement);
						break;
					case TokenType.InvertedElementOpen:
						var invertedScope = new InvertedDocumentScopeItem(currentToken.Value);
						invertedScope.Add(Parse(tokens, options));
						buildArray.Add(invertedScope);
						break;
					case TokenType.CollectionClose:
					case TokenType.ElementClose:

						// This should immediately return if we're in the element scope,
						// -and if we're not, this should have been detected by the tokenizer!
						return buildArray;
					case TokenType.Format:
						buildArray.Add(ParseFormatting(currentToken, tokens, options));
						break;
					case TokenType.EscapedSingleValue:
					case TokenType.UnescapedSingleValue:
						buildArray.Add(new PathDocumentItem(currentToken.Value, currentToken.Type == TokenType.EscapedSingleValue));
						break;
					case TokenType.PartialDeclarationOpen:
						// currently same named partials will override each other
						// to allow recursive calls of partials we first have to declare the partial and then load it as we would parse
						// -the partial as a whole and then add it to the list would lead to unknown calls of partials inside the partial
						buildArray.Add(new PartialDocumentItem(currentToken.Value, HandlePartialDeclaration(currentToken, tokens, options)));
						break;
					case TokenType.RenderPartial:
						var partialName = currentToken.Value;
						buildArray.Add(new RenderPartialDocumentItem(partialName));
						break;
					case TokenType.PartialDeclarationClose:
						partialName = currentToken.Value;
						buildArray.Add(new RenderPartialDoneItem(partialName));
						break;
				}
			}

			return buildArray;

			//return async (builder, context) =>
			//{
			//	await buildArray.ExecuteWith(builder, context);
			//};
		}

		private static IDocumentItem HandlePartialDeclaration(TokenPair currentToken,
			Queue<TokenPair> tokens,
			ParserOptions options)
		{
			var partialTokens = new Queue<TokenPair>();
			var token = currentToken;
			while (tokens.Any() &&
				   (token.Type != TokenType.PartialDeclarationClose || token.Value != currentToken.Value)) //just look for the closing tag and buffer it separate
			{
				token = tokens.Dequeue();
				partialTokens.Enqueue(token);
			}
			return Parse(partialTokens, options); //we have taken everything from the partial and created a executable function for it
		}

		private static IDocumentItem ParseFormatting(TokenPair token, Queue<TokenPair> tokens,
			ParserOptions options)
		{
			var buildArray = new FormattedElementItem();
			buildArray.Add(new CallFormatterItem(token.FormatString, token.Value));

			var nonPrintToken = false;
			while (tokens.Any() && !nonPrintToken) //only take as few tokens we need for formatting. 
			{
				var currentToken = tokens.Peek(); //do not dequeue then right now because if this is no printable token we go back to caller
				switch (currentToken.Type)
				{
					case TokenType.Format:
						//this will invoke the formatter and copy the scope.
						//we must copy the scope as the formatting action might break our chain and we are no longer able to 
						//construct a valid path up
						//after that there is always a PrintFormatted type that will print the "current" scope and
						//reset it to the original scope before we have entered the scope
						currentToken = tokens.Dequeue();
						buildArray.Add(new CallFormatterItem(currentToken.FormatString, currentToken.Value));
						break;
					case TokenType.PrintFormatted:
						tokens.Dequeue(); //this must be the flow token type that has no real value except for a dot
						buildArray.Add(new PrintFormattedItem());
						break;
					case TokenType.CollectionOpen: //in this case we are in a formatting expression followed by a #each.
												   //after this we need to reset the context so handle the open here
						buildArray.Add(new CollectionDocumentItem(Parse(tokens, options), tokens.Dequeue().Value));
						break;
					case TokenType.ElementOpen: //in this case we are in a formatting expression followed by a #.
												//after this we need to reset the context so handle the open here
						var singleElement = new DocumentScopeItem(tokens.Dequeue().Value);
						singleElement.Add(Parse(tokens, options));
						buildArray.Add(singleElement);
						break;
					case TokenType.InvertedElementOpen: //in this case we are in a formatting expression followed by a ^.
														//after this we need to reset the context so handle the open here
						var invertedScope = new InvertedDocumentScopeItem(tokens.Dequeue().Value);
						invertedScope.Add(Parse(tokens, options));
						buildArray.Add(invertedScope);
						break;
					default:
						//The following cannot be formatted and the result of the formatting operation has used.
						//continue with the original Context
						nonPrintToken = true;
						break;
				}
			}

			return buildArray;
		}
	}

	/// <summary>
	///		Internal class to ensure that the given limit of bytes to write is never extended to ensure template quotas
	/// </summary>
	/// <seealso cref="System.IDisposable" />
	internal class ByteCounterStream : IByteCounterStream
	{
		public ByteCounterStream([NotNull] Stream stream, [NotNull] Encoding encoding, int bufferSize, bool leaveOpen)
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

	/// <summary>
	///		Defines the output that can count on written bytes into a stream
	/// </summary>
	public interface IByteCounterStream : IDisposable
	{
		/// <summary>
		/// Gets or sets the bytes written.
		/// </summary>
		/// <value>
		/// The bytes written.
		/// </value>
		long BytesWritten { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [reached limit].
		/// </summary>
		/// <value>
		///   <c>true</c> if [reached limit]; otherwise, <c>false</c>.
		/// </value>
		bool ReachedLimit { get; set; }

		/// <summary>
		/// Writes the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="sizeOfContent">Content of the size of.</param>
		void Write(string value, long sizeOfContent);

		/// <summary>
		/// Writes the specified value. Without counting its bytes.
		/// </summary>
		/// <param name="value">The value.</param>
		void Write(string value);

		/// <summary>
		/// Writes the specified value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="sizeOfContent">Content of the size of.</param>
		void Write(char[] value, long sizeOfContent);
	}
}