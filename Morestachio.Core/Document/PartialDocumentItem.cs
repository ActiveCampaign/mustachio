using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Contains the Declaration of a Partial item
	/// </summary>
	public class PartialDocumentItem : DocumentItemBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PartialDocumentItem"/> class.
		/// </summary>
		/// <param name="partialName">The partial name.</param>
		/// <param name="partial">The partial.</param>
		public PartialDocumentItem(string partialName, IDocumentItem partial)
		{
			Value = partialName;
			Partial = partial;
		}
		/// <summary>
		///		The name of the Partial
		/// </summary>
		public string Value { get; private set; }

		/// <summary>
		///		The partial Document
		/// </summary>
		public IDocumentItem Partial { get; }

		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			scopeData.Partials[Value] = Partial;
			await Task.CompletedTask;
			return new DocumentItemExecution[0];
		}
	}
}