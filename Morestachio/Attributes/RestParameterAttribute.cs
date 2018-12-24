using System;

namespace Morestachio.Attributes
{
	/// <summary>
	///		Marks the Parameter as an Rest parameter. All non specify parameter will given here. 
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
	public sealed class RestParameterAttribute : Attribute
	{
	}
}