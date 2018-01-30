using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Mustachio
{
	public delegate Stream TemplateGeneration(IDictionary<string, object> data);
	/// <summary>
	/// Provided when parsing a template and getting information about the embedded variables.
	/// </summary>
	public class ExtendedParseInformation
    {
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
		public TemplateGenerationWithCancel ParsedTemplateWithCancelation { get; set; }

	    /// <summary>
	    /// returns a model that contains all used placeholders and operations
	    /// </summary>
		public InferredTemplateModel InferredModel { get; set; }
    }
}
