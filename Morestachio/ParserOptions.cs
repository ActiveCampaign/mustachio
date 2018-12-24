#region

using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Morestachio.Attributes;
using Morestachio.Formatter;

#endregion

namespace Morestachio
{
	/// <summary>
	///     Options for Parsing run
	/// </summary>
	[PublicAPI]
	[Serializable]
	public class ParserOptions
	{
		[NotNull]
		private IFormatterMatcher _formatters;

		/// <summary>
		///     ctor
		/// </summary>
		/// <param name="template"></param>
		public ParserOptions([NotNull]string template)
			: this(template, null)
		{
		}

		/// <summary>
		///     ctor
		/// </summary>
		/// <param name="template"></param>
		/// <param name="sourceStream">The factory that is used for each template generation</param>
		public ParserOptions([NotNull]string template, Func<Stream> sourceStream)
			: this(template, sourceStream, null)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ParserOptions" /> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="sourceStream">The source stream.</param>
		/// <param name="encoding">The encoding.</param>
		public ParserOptions([CanBeNull]string template, Func<Stream> sourceStream, Encoding encoding)
		{
			Template = template ?? "";
			SourceFactory = sourceStream ?? (() => new MemoryStream());
			Encoding = encoding ?? Encoding.Default;
			_formatters = new FormatterMatcher();
			Null = string.Empty;
			MaxSize = 0;
			DisableContentEscaping = false;
			WithModelInference = false;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ParserOptions" /> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="sourceStream">The source stream.</param>
		/// <param name="encoding">The encoding.</param>
		/// <param name="maxSize">The maximum size.</param>
		/// <param name="disableContentEscaping">if set to <c>true</c> [disable content escaping].</param>
		/// <param name="withModelInference">if set to <c>true</c> [with model inference].</param>
		public ParserOptions([NotNull]string template, Func<Stream> sourceStream, Encoding encoding, long maxSize,
			bool disableContentEscaping = false, bool withModelInference = false)
			: this(template, sourceStream, encoding)
		{
			MaxSize = maxSize;
			DisableContentEscaping = disableContentEscaping;
			WithModelInference = withModelInference;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ParserOptions" /> class.
		/// </summary>
		/// <param name="template">The template.</param>
		/// <param name="sourceStream">The source stream.</param>
		/// <param name="encoding">The encoding.</param>
		/// <param name="disableContentEscaping">if set to <c>true</c> [disable content escaping].</param>
		/// <param name="withModelInference">if set to <c>true</c> [with model inference].</param>
		public ParserOptions([NotNull]string template, Func<Stream> sourceStream, Encoding encoding,
			bool disableContentEscaping = false, bool withModelInference = false)
			: this(template, sourceStream, encoding, 0, disableContentEscaping, withModelInference)
		{
		}

		/// <summary>
		///     Adds an Formatter overwrite or new Formatter for an Type
		/// </summary>
		[NotNull]
		public IFormatterMatcher Formatters
		{
			get { return _formatters; }
			set
			{
				_formatters = value ?? throw new InvalidOperationException("You must set the Formatters matcher");
			}
		}

		/// <summary>
		///     The template content to parse.
		/// </summary>
		[NotNull]
		public string Template { get; }

		/// <summary>
		///     In some cases, content should not be escaped (such as when rendering text bodies and subjects in emails).
		///     By default, we use no content escaping, but this parameter allows it to be enabled.
		/// </summary>
		public bool DisableContentEscaping { get; }

		/// <summary>
		///     Parse the template, and capture paths used in the template to determine a suitable structure for the required
		///     model.
		/// </summary>
		public bool WithModelInference { get; }

		/// <summary>
		///     Defines a Max size for the Generated Template.
		///     Zero for unlimited
		/// </summary>
		public long MaxSize { get; }

		/// <summary>
		///     SourceFactory can be used to create a new stream for each template. Default is
		///     <code>() => new MemoryStream()</code>
		/// </summary>
		[NotNull]
		public Func<Stream> SourceFactory { get; }

		/// <summary>
		///     In what encoding should the text be written
		///     Default is <code>Encoding.Default</code>
		/// </summary>
		[NotNull]
		public Encoding Encoding { get; }

		/// <summary>
		///     Defines how NULL values are exposed to the Template default is <code>String.Empty</code>
		/// </summary>
		[NotNull]
		public string Null { get; set; }

		/// <summary>
		///     Adds a formatter with type check
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="formatter"></param>
		/// <param name="description"></param>
		[Obsolete("This function is obsolete. Call Formatters.AddFormatter directly")]
		public void AddFormatter<T>([NotNull]Func<T, object, object> formatter, string description = null)
		{
			Formatters.AddFormatter(typeof(T), formatter);
		}

		/// <summary>
		///     Adds a formatter with type check
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="formatter"></param>
		/// <param name="description"></param>
		[Obsolete("This function is obsolete. Call Formatters.AddFormatter directly")]
		public void AddFormatter<T>([NotNull]Func<T, object> formatter, string description = null)
		{
			Formatters.AddFormatter(typeof(T), formatter);
		}

		/// <summary>
		///     Adds a formatter with type check and multiple arguments.
		///		Ether the first argument must by of type of <typeparamref name="T"/> or any object annotated with the <seealso cref="SourceObjectAttribute"/>.
		///		Must not return something. The delegate can take use of the <seealso cref="FormatterArgumentNameAttribute"/> to match names of arguments in the template
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="formatter"></param>
		/// <param name="description"></param>
		[Obsolete("This function is obsolete. Call Formatters.AddFormatter directly")]
		public void AddFormatter<T>([NotNull]Delegate formatter, string description = null)
		{
			Formatters.AddFormatter(typeof(T), formatter);
		}
	}
}