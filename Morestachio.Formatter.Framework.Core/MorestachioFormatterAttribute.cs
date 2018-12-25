using System;

namespace Morestachio.Formatter.Framework
{
	/// <summary>
	///		When decorated by a function, it can be used to format in morestachio
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	public sealed class MorestachioFormatterAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MorestachioFormatterAttribute"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="description">The description.</param>
		public MorestachioFormatterAttribute(string name, string description)
		{
			Name = name;
			Description = description;
		}

		/// <summary>
		///		What is the "header" of the function in morestachio.
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>
		/// The description.
		/// </value>
		public string Description { get; private set; }
		/// <summary>
		/// Gets or sets the return hint.
		/// </summary>
		/// <value>
		/// The return hint.
		/// </value>
		public string ReturnHint { get; set; }
		/// <summary>
		/// Gets or sets the type of the output.
		/// </summary>
		/// <value>
		/// The type of the output.
		/// </value>
		public Type OutputType { get; set; }
	}
}