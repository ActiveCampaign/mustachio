using System.IO;
using System.Threading;
using Morestachio.Helper;

namespace Morestachio
{
	/// <summary>
	///     Provided when parsing a template and getting information about the embedded variables.
	/// </summary>
	public class ExtendedParseInformation
	{
		/// <summary>
		///     ctor
		/// </summary>
		/// <param name="inferredModel"></param>
		/// <param name="parserOptions"></param>
		public ExtendedParseInformation(InferredTemplateModel inferredModel, ParserOptions parserOptions,
			TemplateGenerationWithCancel parsedTemplateWithCancelation)
		{
			InferredModel = inferredModel;
			ParserOptions = parserOptions;
			ParsedTemplateWithCancelation = parsedTemplateWithCancelation;
		}

		/// <summary>
		///     returns a delegate that can be used for generation of the template without Cancellation
		/// </summary>
		public TemplateGeneration ParsedTemplate
		{
			get { return data => ParsedTemplateWithCancelation(data, CancellationToken.None); }
		}

		/// <summary>
		///     returns a delegate that can be used for generation of the template with Cancellation
		/// </summary>
		public TemplateGenerationWithCancel ParsedTemplateWithCancelation { get; }

		/// <summary>
		///     The parser Options object that was used to create the Template Delegate
		/// </summary>
		public ParserOptions ParserOptions { get; }

		/// <summary>
		///     returns a model that contains all used placeholders and operations
		/// </summary>
		public InferredTemplateModel InferredModel { get; }

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public Stream Create(object source, CancellationToken token)
		{
			return ParsedTemplateWithCancelation(source, token);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public Stream Create(object source)
		{
			return Create(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public string CreateAndStringify(object source)
		{
			return Create(source).Stringify(true, ParserOptions.Encoding);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public string CreateAndStringify(object source, CancellationToken token)
		{
			return Create(source, token).Stringify(true, ParserOptions.Encoding);
		}
	}
}