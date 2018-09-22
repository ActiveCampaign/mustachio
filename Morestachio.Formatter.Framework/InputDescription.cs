using System;

namespace Morestachio.Formatter.Framework
{
	/// <summary>
	///		Wrapper class for the input of an formatter function
	/// </summary>
	public class InputDescription
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InputDescription"/> class.
		/// </summary>
		/// <param name="description">The description.</param>
		/// <param name="outputType">Type of the output.</param>
		/// <param name="example">The example.</param>
		public InputDescription(string description, Type outputType, string example)
		{
			Description = description;
			OutputType = outputType;
			Example = example;
		}
		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>
		/// The description.
		/// </value>
		public string Description { get; private set; }
		/// <summary>
		/// Gets the example.
		/// </summary>
		/// <value>
		/// The example.
		/// </value>
		public string Example { get; private set; }
		/// <summary>
		/// Gets the type of the output if its not the same as the function returns.
		/// </summary>
		/// <value>
		/// The type of the output.
		/// </value>
		public Type OutputType { get; private set; }
	}
}