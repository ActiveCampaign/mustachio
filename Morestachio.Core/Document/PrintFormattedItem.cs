using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class PrintFormattedItem : DocumentItemBase
	{
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			if (context != null)
			{
				string value = null;
				await context.EnsureValue();
				if (context.Value != null)
				{
					value = await context.RenderToString();
				}

				ContentDocumentItem.WriteContent(outputStream, value, context);
			}
			
			return Childs.WithScope(context);
		}
	}
}