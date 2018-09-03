using System;

namespace Mustachio
{
	/// <summary>
	///     Encapsulates a Format function
	/// </summary>
	public class FormatTemplateElement
	{
		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="desciption"></param>
		/// <param name="formatTemplate"></param>
		public FormatTemplateElement(string desciption, FormatTemplateElementDelegate formatTemplate)
		{
			Desciption = desciption;
			Format = formatTemplate;
		}

		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="desciption"></param>
		/// <param name="formatTemplate"></param>
		/// <param name="inputType"></param>
		/// <param name="outputType"></param>
		/// <param name="argumentType"></param>
		public FormatTemplateElement(string desciption, FormatTemplateElementDelegate formatTemplate, Type inputType,
			Type outputType, Type argumentType) : this(desciption, formatTemplate)
		{
			InputType = inputType;
			OutputType = outputType;
			ArgumentType = argumentType;
		}

		/// <summary>
		///     delegate for formatting template pars
		/// </summary>
		public FormatTemplateElementDelegate Format { get; private set; }

		/// <summary>
		///     Help Text for UI editors
		/// </summary>
		public string Desciption { get; private set; }

		/// <summary>
		///		The type of the Argument that the formatter expects. Can be null.
		/// </summary>
		public Type ArgumentType { get; private set; }

		/// <summary>
		///     The type of input the Formatter is able to accept. Can be null.
		/// </summary>
		public Type InputType { get; private set; }

		/// <summary>
		///     The type that the formatter will return. Can be null.
		/// </summary>
		public Type OutputType { get; private set; }

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