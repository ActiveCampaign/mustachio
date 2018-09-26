using System;
using JetBrains.Annotations;
using Morestachio.Formatter;

namespace Morestachio
{
	/// <summary>
	///     Encapsulates a Format function
	/// </summary>
	public class FormatTemplateElement
	{
		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="formatTemplate"></param>
		/// <param name="inputTypes"></param>
		/// <param name="outputType"></param>
		/// <param name="argumentMeta"></param>
		[PublicAPI]
		public FormatTemplateElement([NotNull]Delegate formatTemplate, [NotNull]Type inputTypes,
			[NotNull]Type outputType, [NotNull, ItemNotNull] params MultiFormatterInfo[] argumentMeta)
		{
			Format = formatTemplate;
			InputTypes = inputTypes;
			OutputType = outputType;
			MetaData = argumentMeta;
		}

		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="formatTemplate"></param>
		/// <param name="inputTypes"></param>
		/// <param name="outputType"></param>
		/// <param name="argumentMeta"></param>
		[PublicAPI]
		public FormatTemplateElement([NotNull]Func<object, object, object> formatTemplate, [NotNull]Type inputTypes,
			[NotNull]Type outputType, [NotNull, ItemNotNull] params MultiFormatterInfo[] argumentMeta)
		{
			Format = formatTemplate;
			InputTypes = inputTypes;
			OutputType = outputType;
			MetaData = argumentMeta;
		}

		/// <summary>
		///     delegate for formatting template pars
		/// </summary>
		[NotNull]
		public Delegate Format { get; }

		/// <summary>
		///		Gets the Meta data for the Arguments
		/// </summary>
		
		[NotNull]
		[ItemNotNull]
		public MultiFormatterInfo[] MetaData { get; }

		/// <summary>
		///     The type of input the Formatter is able to accept. Can be null.
		/// </summary>
		[NotNull]
		public Type InputTypes { get; }

		/// <summary>
		///     The type that the formatter will return. Can be null.
		/// </summary>
		[NotNull]
		public Type OutputType { get; }
	}
}