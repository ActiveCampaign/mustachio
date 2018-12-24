using System.Collections.Generic;
using JetBrains.Annotations;

namespace Morestachio
{
	/// <summary>
	///     Delegate for the Can Execute method on a FormatTemplateElement
	/// </summary>
	/// <param name="sourceObject">The source object.</param>
	/// <param name="parameter">
	///     The parameters from template matched to the formatters
	///     <seealso cref="FormatTemplateElement.Format" />.
	/// </param>
	/// <returns></returns>
	public delegate bool CanExecute([CanBeNull] object sourceObject, [NotNull] KeyValuePair<string, object>[] parameter);
}