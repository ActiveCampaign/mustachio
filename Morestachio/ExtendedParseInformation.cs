using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Threading;
using JetBrains.Annotations;
using Morestachio.Helper;

namespace Morestachio
{
	/// <summary>
	///     Provided when parsing a template and getting information about the embedded variables.
	/// </summary>
	public class ExtendedParseInformation
	{
		/// <summary>
		/// Serilize Constructor. Should not be used by user code.
		/// </summary>
		[Obsolete("This is an Serilization only constructor and should not be used in user code!", true)]
		public ExtendedParseInformation()
		{
			InternalTemplate = new Lazy<Action<StreamWriter, ContextObject>>(() => Parser.Parse(TemplateTokens, ParserOptions, ParserOptions.WithModelInference ? InferredModel : null));
		}

		/// <summary>
		///     ctor
		/// </summary>
		/// <param name="inferredModel"></param>
		/// <param name="parserOptions"></param>
		/// <param name="tokens"></param>
		internal ExtendedParseInformation(InferredTemplateModel inferredModel, ParserOptions parserOptions, Queue<TokenPair> tokens)
		{
			InferredModel = inferredModel;
			ParserOptions = parserOptions;
			TemplateTokens = tokens;
			InternalTemplate = new Lazy<Action<StreamWriter, ContextObject>>(() => Parser.Parse(TemplateTokens, ParserOptions, ParserOptions.WithModelInference ? InferredModel : null));
		}

		internal Lazy<Action<StreamWriter, ContextObject>> InternalTemplate;

		/// <summary>
		///		The generated tokes from the tokeniser
		/// </summary>
		internal Queue<TokenPair> TemplateTokens { get; }

		/// <summary>
		///     The parser Options object that was used to create the Template Delegate
		/// </summary>

		[NotNull] 
		public ParserOptions ParserOptions { get; }

		/// <summary>
		///     returns a model that contains all used placeholders and operations
		/// </summary>
		[CanBeNull] 
		public InferredTemplateModel InferredModel { get; }

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		[MustUseReturnValue("The Stream contains the template. Use Stringify(Encoding) to get the string of it")]
		[NotNull]
		public Stream Create([NotNull]object source, CancellationToken token)
		{
			return Parser.CreateTemplateStream(this, source, token);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[MustUseReturnValue("The Stream contains the template. Use Stringify(Encoding) to get the string of it")]
		[NotNull]
		public Stream Create([NotNull]object source)
		{
			return Create(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[NotNull]
		public string CreateAndStringify([NotNull]object source)
		{
			return Create(source).Stringify(true, ParserOptions.Encoding);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		[NotNull]
		public string CreateAndStringify([NotNull]object source, CancellationToken token)
		{
			return Create(source, token).Stringify(true, ParserOptions.Encoding);
		}
	}
}