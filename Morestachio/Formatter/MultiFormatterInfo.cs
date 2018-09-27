using System;
using JetBrains.Annotations;
using Morestachio.Attributes;

namespace Morestachio.Formatter
{
	/// <summary>
	///		Contains information about the Parameter of an Multi argument formatter
	/// </summary>
	public class MultiFormatterInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MultiFormatterInfo"/> class.
		/// </summary>
		public MultiFormatterInfo([NotNull]Type type, [CanBeNull]string name, bool isOptional, int index, bool isRestObject)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Name = name;
			IsOptional = isOptional;
			Index = index;
			IsRestObject = isRestObject;
		}

		/// <summary>
		///		Of what type is this parameter
		/// </summary>
		public Type Type { get; }
		/// <summary>
		///		Ether the name that is declared using the <seealso cref="FormatterArgumentNameAttribute"/> or the name of the Parameter from code ( in that order )
		/// </summary>
		public string Name { get; }
		/// <summary>
		///		Is the parameter optional
		/// </summary>
		public bool IsOptional { get; }
		/// <summary>
		///		Is this parameter the source object
		/// </summary>
		public bool IsSourceObject { get; internal set; }
		/// <summary>
		///		The index in what order the argument is present in the Formatter
		/// </summary>
		public int Index { get; internal set; }

		/// <summary>
		///		Is this parameter a params parameter. If so it will get all following not matched arguments
		/// </summary>
		public bool IsRestObject { get; }
	}
}
