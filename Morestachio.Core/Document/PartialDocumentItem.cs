using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class PartialDocumentItem : DocumentItemBase
	{
		public PartialDocumentItem(string partialName, IDocumentItem partial)
		{
			Value = partialName;
			Partial = partial;
		}

		public string Value { get; private set; }
		public IDocumentItem Partial { get; }

		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			scopeData.Partials[Value] = Partial;
			await Task.CompletedTask;
			return new DocumentItemExecution[0];
		}
	}
}