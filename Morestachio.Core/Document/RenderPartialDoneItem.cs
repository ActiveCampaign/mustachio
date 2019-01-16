using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class RenderPartialDoneItem : DocumentItemBase
	{
		public RenderPartialDoneItem(string partialName)
		{
			Value = partialName;
		}

		public string Value { get; private set; }

		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			scopeData.PartialDepth.Pop();
			return new DocumentItemExecution[0];
		}
	}
}