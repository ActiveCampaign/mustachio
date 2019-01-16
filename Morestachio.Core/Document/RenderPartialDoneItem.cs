using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		The end of a Partial declaration
	/// </summary>
	public class RenderPartialDoneItem : DocumentItemBase
	{
		/// <inheritdoc />
		public RenderPartialDoneItem(string partialName)
		{
			Value = partialName;
		}

		/// <summary>
		///		The name of the Partial
		/// </summary>
		public string Value { get; private set; }
		
		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			await Task.CompletedTask;
			scopeData.PartialDepth.Pop();
			return new DocumentItemExecution[0];
		}
	}
}