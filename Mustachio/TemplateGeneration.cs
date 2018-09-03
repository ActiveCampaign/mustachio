using System.Collections.Generic;
using System.IO;

namespace Mustachio
{
	/// <summary>
	///
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public delegate Stream TemplateGeneration(IDictionary<string, object> data);
}