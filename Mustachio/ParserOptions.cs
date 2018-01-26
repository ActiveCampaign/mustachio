using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mustachio
{
	/// <summary>
	/// Options for Parsing run
	/// </summary>
	public class ParserOptions
	{
		public ParserOptions(string template)
			: this(template, null)
		{
		}

		public ParserOptions(string template, Stream sourceStream)
			: this(template, sourceStream, null)
		{
		}

		public ParserOptions(string template, Stream sourceStream, Encoding encoding)
		{
			Template = template;
			SourceStream = sourceStream ?? new MemoryStream();
			Encoding = encoding ?? Encoding.UTF8;
			Formatters = new Dictionary<Type, FormatTemplateElement>();
		}

		public ParserOptions(string template, Stream sourceStream, Encoding encoding, long maxSize, bool disableContentEscaping = false, bool withModelInference = false)
			: this(template, sourceStream, encoding)
		{
			MaxSize = maxSize;
			DisableContentEscaping = disableContentEscaping;
			WithModelInference = withModelInference;
		}

		public ParserOptions(string template, Stream sourceStream, Encoding encoding, bool disableContentEscaping = false, bool withModelInference = false)
			: this(template, sourceStream, encoding, 0, disableContentEscaping, withModelInference)
		{

		}

		/// <summary>
		/// Adds an Formatter overwrite or new Formatter for an Type
		/// </summary>
		public IDictionary<Type, FormatTemplateElement> Formatters { get; private set; }

		/// <summary>
		///		The template content to parse.
		/// </summary>
		public string Template { get; private set; }

		/// <summary>
		///     In some cases, content should not be escaped (such as when rendering text bodies and subjects in emails).
		///     By default, we use content escaping, but this parameter allows it to be disabled.
		/// </summary>
		public bool DisableContentEscaping { get; private set; }

		/// <summary>
		///     Parse the template, and capture paths used in the template to determine a suitable structure for the required
		///     model.
		/// </summary>
		public bool WithModelInference { get; private set; }

		/// <summary>
		/// Defines a Max size for the Generated Template.
		/// Zero for no unlimited
		/// </summary>
		public long MaxSize { get; private set; }


		/// <summary>
		/// The target Stream that should be targeted for writing the Template
		/// Default is an Empty MemoryStream
		/// </summary>
		public Stream SourceStream { get; private set; }

		/// <summary>
		/// In what encoding should the text be written
		/// Default is UTF8
		/// </summary>
		public Encoding Encoding { get; private set; }

		/// <summary>
		/// Defines how NULL values are exposed to the Template default is String.Empty
		/// </summary>
		public string Null { get; set; } = string.Empty;
	}
}