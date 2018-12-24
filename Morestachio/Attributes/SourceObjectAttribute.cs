using System;

namespace Morestachio.Attributes
{
	/// <summary>
	///		Marks an Parameter as the source object. That object is the source from where the formatter was called.
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class SourceObjectAttribute : Attribute
	{
	}

	/// <summary>
	///		Marks the Parameter as an Rest parameter. All non specify parameter will given here. 
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
	public sealed class RestParameterAttribute : Attribute
	{
	}
}
