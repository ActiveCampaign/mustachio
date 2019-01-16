using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Base class for Document items
	/// </summary>
	public abstract class DocumentItemBase : IDocumentItem
	{
		/// <inheritdoc />
		public DocumentItemBase()
		{
			Childs = new List<IDocumentItem>();
		}
		
		/// <inheritdoc />
		public abstract Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData);

		
		/// <inheritdoc />
		public IDocumentItem Parent { get; set; }
		/// <inheritdoc />
		public IList<IDocumentItem> Childs { get; }

		/// <summary>
		///		Can be called to check if any stop is requested. If return true no stop is requested
		/// </summary>
		protected static bool StopOrAbortBuilding(IByteCounterStream builder, ContextObject context)
		{
			return !context.AbortGeneration && !context.CancellationToken.IsCancellationRequested && !builder.ReachedLimit;
		}

		/// <summary>
		/// Adds the specified childs.
		/// </summary>
		/// <param name="childs">The childs.</param>
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