using System;
using System.Reflection;

namespace Morestachio.Formatter.Framework
{
	/// <summary>
	///		Wrapper class for a function call
	/// </summary>
	public class MorestachioFormatterModel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MorestachioFormatterModel"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="description">The description.</param>
		/// <param name="inputType">Type of the input.</param>
		/// <param name="inputDescription">The input description.</param>
		/// <param name="output">The output.</param>
		/// <param name="function">The function.</param>
		public MorestachioFormatterModel(string name, string description,
			Type inputType,
			InputDescription[] inputDescription,
			string output,
			MethodInfo function)
		{
			Name = name;
			Description = description;
			InputDescription = inputDescription;
			Output = output;
			Function = function;
			InputType = inputType;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="MorestachioFormatterModel"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="description">The description.</param>
		/// <param name="inputType">Type of the input.</param>
		/// <param name="outputType">Type of the output.</param>
		/// <param name="inputDescription">The input description.</param>
		/// <param name="output">The output.</param>
		/// <param name="function">The function.</param>
		public MorestachioFormatterModel(string name, string description,
			Type inputType,
			Type outputType,
			InputDescription[] inputDescription,
			string output,
			MethodInfo function)
			: this(name, description, inputType, inputDescription, output, function)
		{
			OutputType = outputType;
		}
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		public string Name { get; private set; }
		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>
		/// The description.
		/// </value>
		public string Description { get; private set; }
		/// <summary>
		/// Gets the input description.
		/// </summary>
		/// <value>
		/// The input description.
		/// </value>
		public InputDescription[] InputDescription { get; private set; }
		/// <summary>
		/// Gets the type of the input.
		/// </summary>
		/// <value>
		/// The type of the input.
		/// </value>
		public Type InputType { get; private set; }
		/// <summary>
		/// Gets the output.
		/// </summary>
		/// <value>
		/// The output.
		/// </value>
		public string Output { get; private set; }
		/// <summary>
		/// Gets the type of the output.
		/// </summary>
		/// <value>
		/// The type of the output.
		/// </value>
		public Type OutputType { get; private set; }
		/// <summary>
		/// Gets the function.
		/// </summary>
		/// <value>
		/// The function.
		/// </value>
		public MethodInfo Function { get; private set; }
	}
}