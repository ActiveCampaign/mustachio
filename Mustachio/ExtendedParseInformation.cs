using System;
using System.Collections.Generic;

namespace Mustachio
{
    /// <summary>
    /// Provided when parsing a template and getting information about the embedded variables.
    /// </summary>
    public class ExtendedParseInformation
    {
        public Func<IDictionary<string, object>, String> ParsedTemplate { get; set; }

        public InferredTemplateModel InferredModel { get; set; }
    }
}
