using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class FormattedElementItem : DocumentItemBase
	{
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			context = context.Clone();
			return Childs.WithScope(context);
			//await MorestachioDocument.ProcessItemsAndChilds(Childs, outputStream, context, scopeData);
			//return new DocumentItemExecution[0];
		}
	}
}