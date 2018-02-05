using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Mustachio.Helper;

namespace Mustachio
{
	/// <summary>
	///
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public delegate Stream TemplateGeneration(IDictionary<string, object> data);
	/// <summary>
	/// The delegate used for Template generation
	/// </summary>
	/// <param name="data"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	public delegate Stream TemplateGenerationWithCancel(object data, CancellationToken token);

	/// <summary>
	/// Provided when parsing a template and getting information about the embedded variables.
	/// </summary>
	public class ExtendedParseInformation
    {
		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="inferredModel"></param>
		/// <param name="parserOptions"></param>
	    public ExtendedParseInformation(InferredTemplateModel inferredModel, ParserOptions parserOptions, TemplateGenerationWithCancel parsedTemplateWithCancelation)
	    {
		    InferredModel = inferredModel;
		    ParserOptions = parserOptions;
		    ParsedTemplateWithCancelation = parsedTemplateWithCancelation;
	    }

	    /// <summary>
		/// returns a delegate that can be used for generation of the template without Cancellation
		/// </summary>
	    public TemplateGeneration ParsedTemplate
	    {
		    get { return data => ParsedTemplateWithCancelation(data, CancellationToken.None); }
	    }

	    /// <summary>
	    /// returns a delegate that can be used for generation of the template with Cancellation
	    /// </summary>
		public TemplateGenerationWithCancel ParsedTemplateWithCancelation { get; private set; }

		/// <summary>
		/// The parser Options object that was used to create the Template Delegate
		/// </summary>
	    public ParserOptions ParserOptions { get; private set; }

	    /// <summary>
	    /// returns a model that contains all used placeholders and operations
	    /// </summary>
		public InferredTemplateModel InferredModel { get; private set; }

		/// <summary>
		/// Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
	    public Stream Create(object source, CancellationToken token)
	    {
		    return ParsedTemplateWithCancelation(source, token);
	    }

		/// <summary>
		/// Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
	    public Stream Create(object source)
	    {
		    return Create(source, CancellationToken.None);
	    }

		/// <summary>
		/// Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
	    public string CreateAndStringify(object source)
	    {
		    return Create(source).Stringify(true, ParserOptions.Encoding);
	    }

		/// <summary>
		/// Calls the Underlying Template Delegate and Produces a Stream
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
