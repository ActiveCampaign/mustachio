using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public interface IDocumentItem
	{
		Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData);
		IDocumentItem Parent { get; set; }
		IList<IDocumentItem> Childs { get; }

		void Add(params IDocumentItem[] childs);
	}
}