using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public abstract class DocumentItemBase : IDocumentItem
	{
		public DocumentItemBase()
		{
			Childs = new List<IDocumentItem>();
		}

		public abstract Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData);

		
		public IDocumentItem Parent { get; set; }
		public IList<IDocumentItem> Childs { get; }
		
		protected static bool StopOrAbortBuilding(IByteCounterStream builder, ContextObject context)
		{
			return !context.AbortGeneration && !context.CancellationToken.IsCancellationRequested && !builder.ReachedLimit;
		}

		public void Add(params IDocumentItem[] childs)
		{
			foreach (var documentItem in childs)
			{
				//documentItem.Parent = this;
				Childs.Add(documentItem);
			}
		}
	}
}