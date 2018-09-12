using System.Collections.Generic;
using System.IO;

namespace Morestachio
{
	/// <summary>
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public delegate Stream TemplateGeneration(IDictionary<string, object> data);
}