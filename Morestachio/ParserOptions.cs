#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
		public ParserOptions([NotNull]string template, Func<Stream> sourceStream, Encoding encoding)
		{
			Template = template;
			SourceFactory = sourceStream ?? (() => new MemoryStream());
			Encoding = encoding ?? Encoding.Default;
			Formatters = new Dictionary<Type, FormatTemplateElement>();
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
		public IDictionary<Type, FormatTemplateElement> Formatters { get; }

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

		///// <summary>
		///// The target Stream that should be targeted for writing the Template
		///// Default is an Empty MemoryStream
		///// </summary>
		//public Stream SourceStream { get; private set; }

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
		public void AddFormatter<T>([NotNull]Func<T, object, object> formatter, string description = null)
		{
			AddFormatter<T>(new FormatTemplateElement(description, (sourceObject, argument) =>
			{
				if (!(sourceObject is T))
				{
					return sourceObject;
				}

				return formatter((T)sourceObject, argument.FirstOrDefault().Value);
			}, typeof(T), null));
		}

		/// <summary>
		///     Adds a formatter with type check
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TArg"></typeparam>
		/// <param name="formatter"></param>
		/// <param name="description"></param>
		public void AddFormatter<T, TArg>([NotNull]Func<T, TArg, object> formatter, string description = null)
		{
			AddFormatter<T>(new FormatTemplateElement(description, (sourceObject, argument) =>
			{
				var singleArgument = argument.FirstOrDefault();
				if (!(sourceObject is T) || (singleArgument.Value != null && !(singleArgument.Value is TArg)))
				{
					return sourceObject;
				}

				return formatter((T)sourceObject, (TArg)singleArgument.Value);
			}, typeof(T), null, typeof(TArg)));
		}

		/// <summary>
		///     Adds a formatter with type check
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TArg"></typeparam>
		/// <typeparam name="TOut"></typeparam>
		/// <param name="formatter"></param>
		/// <param name="description"></param>
		public void AddFormatter<T, TArg, TOut>([NotNull]Func<T, TArg, TOut> formatter, string description = null)
		{
			AddFormatter<T>(new FormatTemplateElement(description, (sourceObject, argument) =>
			{
				var singleArgument = argument.FirstOrDefault();
				if (!(sourceObject is T) || (singleArgument.Value != null && !(singleArgument.Value is TArg)))
				{
					return sourceObject;
				}

				return formatter((T)sourceObject, (TArg)singleArgument.Value);
			}, typeof(T), typeof(TOut), typeof(TArg)));
		}

		/// <summary>
		///     Adds a formatter with type check and multiple arguments.
		///		Ether the first argument must by of type of <typeparamref name="T"/> or any object annotated with the <seealso cref="SourceObjectAttribute"/>.
		///		Must not return something. The delegate can take use of the <seealso cref="FormatterArgumentNameAttribute"/> to match names of arguments in the template
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="formatter"></param>
		/// <param name="description"></param>
		public void AddMultipleArgumentsFormatter<T>([NotNull]Delegate formatter, string description = null)
		{
			var arguments = formatter.Method.GetParameters().Select((e, index) => new MultiFormatterInfo()
			{
				Type = e.ParameterType,
				Name = e.GetCustomAttribute<FormatterArgumentNameAttribute>()?.Name ?? e.Name,
				IsOptional = e.IsOptional,
				IsSourceObject = e.GetCustomAttribute<SourceObjectAttribute>() != null,
				Index = index
			}).ToArray();

			var returnValue = formatter.Method.ReturnParameter?.ParameterType;

			//if there is no declared SourceObject then check if the first object is of type what we are formatting and use this one.
			if (!arguments.Any(e => e.IsSourceObject) && arguments.Any() && arguments[0].Type == typeof(T))
			{
				arguments[0].IsSourceObject = true;
			}

			var sourceValue = arguments.FirstOrDefault(e => e.IsSourceObject);
			if (sourceValue != null)
			{
				//if we have a source value in the arguments reduce the index of all following 
				//this is important as the source value is never ommited in the formatter string so we will not "count" it 
				for (int i = sourceValue.Index; i < arguments.Length; i++)
				{
					arguments[i].Index--;
				}

				sourceValue.Index = -1;
			}

			AddFormatter<T>(new FormatTemplateElement(description, (sourceObject, argument) =>
			{
				var values = new Dictionary<MultiFormatterInfo, object>();

				//var sourceValue = arguments.FirstOrDefault(e => e.IsSourceObject);
				
				foreach (var multiFormatterInfo in arguments)
				{
					object givenValue;
					//set ether the source object or the value from the given arguments
					if (multiFormatterInfo.IsSourceObject)
					{
						givenValue = sourceObject;
					}
					else
					{
						//match by index or name
						var index = 0;
						givenValue = argument.FirstOrDefault((g) =>
						{
							if (!string.IsNullOrWhiteSpace(g.Key))
							{
								index++;
								return g.Key.Equals(multiFormatterInfo.Name);
							}
							return (index++) == multiFormatterInfo.Index;
						}).Value;
					}

					values.Add(multiFormatterInfo, givenValue);
					if (multiFormatterInfo.IsOptional || multiFormatterInfo.IsSourceObject)
					{
						continue; //value and source object are optional so we do not to check for its existence 
					}

					if (Equals(givenValue, null))
					{
						//the delegates parameter is not optional so this formatter does not fit. Continue.
						return sourceObject;
					}
				}

				return formatter.DynamicInvoke(values.Select(e => e.Value).ToArray());
			}, typeof(T), returnValue, arguments.Select(e => e.Type).ToArray()));
		}

		/// <summary>
		///     Adds a formatter with type check
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="formatter"></param>
		public void AddFormatter<T>([NotNull]FormatTemplateElement formatter)
		{
			Formatters.Add(typeof(T), formatter);
		}
	}
}