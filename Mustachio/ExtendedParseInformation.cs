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
	    public TemplateGeneration ParsedTemplate
	    {
		    get { return data => ParsedTemplateWithCancelation(data, CancellationToken.None); }
	    }

	    public TemplateGenerationWithCancel ParsedTemplateWithCancelation { get; set; }

		public InferredTemplateModel InferredModel { get; set; }
    }
}
