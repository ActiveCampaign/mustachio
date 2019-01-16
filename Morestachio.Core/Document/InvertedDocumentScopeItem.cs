using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class InvertedDocumentScopeItem : DocumentItemBase
	{
		public InvertedDocumentScopeItem(string value)
		{
			Value = value;
		}

		public string Value { get; private set; }

		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			var c = await context.GetContextForPath(Value, scopeData);
			//"falsey" values by Javascript standards...
			if (!await c.Exists())
			{
				return Childs.WithScope(c);
			}
			return new DocumentItemExecution[0];
		}
	}
}