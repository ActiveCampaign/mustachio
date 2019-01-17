#region

using System;
using System.Collections.Generic;
using System.Linq;
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

		/// <summary>
		///		Parses the Tokens into a Document.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		/// <param name="options">The options.</param>
		/// <returns></returns>
		internal static IDocumentItem Parse(Queue<TokenPair> tokens, ParserOptions options)
		{
			var buildStack = new Stack<IDocumentItem>(); //instead of recursive calling the parse function we stack the current document 
			buildStack.Push(new MorestachioDocument()); 

			var inFormat = false;

			while (tokens.Any())
			{
				var currentToken = tokens.Dequeue();
				var currentDocumentItem = buildStack.Peek(); //get the latest document

				if (currentToken.Type == TokenType.Comment)
				{
					//just ignore this part and print nothing
				}
				else if (currentToken.Type == TokenType.Content)
				{
					currentDocumentItem.Add(new ContentDocumentItem(currentToken.Value));
				}
				else if (currentToken.Type == TokenType.CollectionOpen)
				{
					var nestedDocument = new CollectionDocumentItem(currentToken.Value);
					buildStack.Push(nestedDocument);
					currentDocumentItem.Add(nestedDocument);
				}
				else if (currentToken.Type == TokenType.ElementOpen)
				{
					var nestedDocument = new ExpressionScopeDocumentItem(currentToken.Value);
					buildStack.Push(nestedDocument);
					currentDocumentItem.Add(nestedDocument);
				}
				else if (currentToken.Type == TokenType.InvertedElementOpen)
				{
					var invertedScope = new InvertedExpressionScopeDocumentItem(currentToken.Value);
					buildStack.Push(invertedScope);
					currentDocumentItem.Add(invertedScope);
				}
				else if (currentToken.Type == TokenType.CollectionClose || currentToken.Type == TokenType.ElementClose)
				{
					// remove the last document from the stack and go back to the parents
					buildStack.Pop();
					if (inFormat && (buildStack.Peek() is IsolatedContextDocumentItem))
					{
						buildStack.Pop();
						inFormat = false;
					}
				}
				else if (currentToken.Type == TokenType.PrintFormatted)
				{
					currentDocumentItem.Add(new PrintFormattedItem());
					buildStack.Pop(); //Print formatted can only be followed by a Format and if not the parser should have not emited it
					inFormat = false;
				}
				else if (currentToken.Type == TokenType.Format)
				{
					if (inFormat && (buildStack.Peek() is IsolatedContextDocumentItem))
					{
						buildStack.Pop();
					}
					inFormat = true;
					var formatterItem = new IsolatedContextDocumentItem();
					buildStack.Push(formatterItem);
					formatterItem.Add(new CallFormatterDocumentItem(currentToken.FormatString, currentToken.Value));
					currentDocumentItem.Add(formatterItem);
				}
				else if (currentToken.Type == TokenType.EscapedSingleValue ||
						 currentToken.Type == TokenType.UnescapedSingleValue)
				{
					currentDocumentItem.Add(new PathDocumentItem(currentToken.Value,
						currentToken.Type == TokenType.EscapedSingleValue));
				}
				else if (currentToken.Type == TokenType.PartialDeclarationOpen)
				{
					// currently same named partials will override each other
					// to allow recursive calls of partials we first have to declare the partial and then load it as we would parse
					// -the partial as a whole and then add it to the list would lead to unknown calls of partials inside the partial
					var nestedDocument = new MorestachioDocument();
					buildStack.Push(nestedDocument);
					currentDocumentItem.Add(new PartialDocumentItem(currentToken.Value, nestedDocument));
				}
				else if (currentToken.Type == TokenType.PartialDeclarationClose)
				{
					currentDocumentItem.Add(new RenderPartialDoneDocumentItem(currentToken.Value));
					buildStack.Pop();
				}
				else if (currentToken.Type == TokenType.RenderPartial)
				{
					currentDocumentItem.Add(new RenderPartialDocumentItem(currentToken.Value));
				}
			}

			return buildStack.Pop();
		}
	}
}