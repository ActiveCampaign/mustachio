using System;

namespace Morestachio.Attributes
{
	/// <summary>
	///		Marks an Parameter as the source object. That object is the source from where the formatter was called.
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public class SourceObjectAttribute : Attribute
	{
	}
}
