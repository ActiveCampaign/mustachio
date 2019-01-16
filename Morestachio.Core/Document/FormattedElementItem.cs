using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Executes the childs with a cloned Context
	/// </summary>
	public class FormattedElementItem : DocumentItemBase
	{
		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			await Task.CompletedTask;
			context = context.Clone();
			return Childs.WithScope(context);
			//await MorestachioDocument.ProcessItemsAndChilds(Childs, outputStream, context, scopeData);
			//return new DocumentItemExecution[0];
		}
	}
}