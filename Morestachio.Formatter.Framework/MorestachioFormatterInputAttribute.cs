using System;

namespace Morestachio.Formatter.Framework
{
	/// <summary>
	///		Declares the input syntax of any formatter.
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	public sealed class MorestachioFormatterInputAttribute : Attribute
	{
		/// <summary>
		///		Shortly describes of what the input argument consists
		/// </summary>
		public string Description { get; }
		/// <summary>
		/// Gets or sets the example.
		/// </summary>
		/// <value>
		/// The example.
		/// </value>
		public string Example { get; set; }
		/// <summary>
		///		If used will the input return any subtype of the used type by the formatter.
		/// <example>
		///if the formatter returns object in its function header, but when this input is used it will return int
		/// </example>
		/// </summary>
		public Type OutputType { get; set; }

		/// <summary>
		///		More description of how the output is formatted. Freetext
		/// </summary>
		public string Output { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MorestachioFormatterInputAttribute"/> class.
		/// </summary>
		/// <param name="description">The description.</param>
		public MorestachioFormatterInputAttribute(string description)
		{
			Description = description;
		}
	}
}