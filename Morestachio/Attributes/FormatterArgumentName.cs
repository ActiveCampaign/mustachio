using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morestachio.Attributes
{
	/// <summary>
	///		Sets the name for a Formatter named Argument
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class FormatterArgumentNameAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FormatterArgumentNameAttribute"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public FormatterArgumentNameAttribute(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		public string Name { get; private set; }
	}
}
