using System.IO;
using System.Threading;

namespace Morestachio
{
	/// <summary>
	///     The delegate used for Template generation
	/// </summary>
	/// <param name="data"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	public delegate Stream TemplateGenerationWithCancel(object data, CancellationToken token);
}