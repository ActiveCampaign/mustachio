using System;
using JetBrains.Annotations;

namespace Morestachio
{
	/// <summary>
	///     Encapsulates a Format function
	/// </summary>
	public class FormatTemplateElement
	{
		/// <summary>
		///		Ctor
		/// </summary>
		/// <param name="desciption"></param>
		/// <param name="formatTemplate"></param>
		public FormatTemplateElement([CanBeNull]string desciption, [NotNull] FormatTemplateElementDelegate formatTemplate)
		{
			Desciption = desciption;
			Format = formatTemplate;
		}

		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="desciption"></param>
		/// <param name="formatTemplate"></param>
		/// <param name="inputTypes"></param>
		/// <param name="outputType"></param>
		/// <param name="argumentType"></param>
		public FormatTemplateElement([CanBeNull]string desciption, [NotNull]FormatTemplateElementDelegate formatTemplate, [CanBeNull]Type inputTypes,
			[CanBeNull]Type outputType, [NotNull, ItemNotNull] params Type[] argumentType) : this(desciption, formatTemplate)
		{
			InputTypes = inputTypes;
			OutputType = outputType;
			ArgumentType = argumentType;
		}

		/// <summary>
		///     delegate for formatting template pars
		/// </summary>
		[NotNull]
		public FormatTemplateElementDelegate Format { get; }

		/// <summary>
		///     Help Text for UI editors
		/// </summary>
		[CanBeNull]
		public string Desciption { get; }

		/// <summary>
		///     The type of the Argument that the formatter expects. Can be null.
		/// </summary>
		[CanBeNull]
		public Type[] ArgumentType { get; }

		/// <summary>
		///     The type of input the Formatter is able to accept. Can be null.
		/// </summary>
		[CanBeNull]
		public Type InputTypes { get; }

		/// <summary>
		///     The type that the formatter will return. Can be null.
		/// </summary>
		[CanBeNull]
		public Type OutputType { get; }

		/// <summary>
		///     Converts a FormatTemplateElementDelegate to a FormatTemplateElement
		/// </summary>
		/// <param name="x"></param>
		public static implicit operator FormatTemplateElement(FormatTemplateElementDelegate x)
		{
			return new FormatTemplateElement(string.Empty, x);
		}
	}
}