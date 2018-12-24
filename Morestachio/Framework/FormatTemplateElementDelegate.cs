using System.Collections.Generic;
using JetBrains.Annotations;

namespace Morestachio
{
	/// <summary>
	///     delegate for formatting template pars
	/// </summary>
	/// <param name="sourceObject">the object that this formatter should be applyed to</param>
	/// <param name="argument">the string argument as given in the template</param>
	/// <returns>a new object or the same object or a string</returns>
	[CanBeNull]
	public delegate object FormatTemplateElementDelegate([CanBeNull]object sourceObject, [CanBeNull]params KeyValuePair<string, object>[] argument);
}