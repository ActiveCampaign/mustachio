using System.Diagnostics;
using JetBrains.Annotations;

namespace Morestachio.Formatter
{
	/// <summary>
	///		An Argument for a Formatter
	/// </summary>
	[DebuggerDisplay("[{Name ?? 'Unnamed'}] {Argument}")]
	public class FormatterPart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FormatterPart"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="argument">The argument.</param>
		public FormatterPart(string name, string argument)
		{
			Name = name;
			Argument = argument;
		}

		/// <summary>
		///		Ether the Name of the Argument or Null
		/// </summary>
		[CanBeNull]
		public string Name { get; set; }

		/// <summary>
		///		The value of the Argument
		/// </summary>
		[CanBeNull]
		public string Argument { get; set; }
	}
}