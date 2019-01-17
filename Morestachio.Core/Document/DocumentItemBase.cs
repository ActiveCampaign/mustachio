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
			Children = new List<IDocumentItem>();
		}
		
		/// <inheritdoc />
		public abstract Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData);

		/// <inheritdoc />
		public abstract string Kind { get; }

		/// <inheritdoc />
		public IList<IDocumentItem> Children { get; }

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
		/// <param name="documentChildren">The childs.</param>
		public void Add(params IDocumentItem[] documentChildren)
		{
			foreach (var documentItem in documentChildren)
			{
				//documentItem.Parent = this;
				Children.Add(documentItem);
			}
		}
	}
}