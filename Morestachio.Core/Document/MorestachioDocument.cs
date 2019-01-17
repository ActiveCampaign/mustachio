using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Defines a document that can be rendered. Does only store its Children
	/// </summary>
	public class MorestachioDocument : DocumentItemBase
	{
		/// <inheritdoc />
		public override string Kind { get; } = "Document";

		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			await Task.CompletedTask;
			return Children.WithScope(context);
		}

		/// <summary>
		/// Processes the items and childs.
		/// </summary>
		/// <param name="documentItems">The document items.</param>
		/// <param name="outputStream">The output stream.</param>
		/// <param name="context">The context.</param>
		/// <param name="scopeData">The scope data.</param>
		/// <returns></returns>
		public static async Task ProcessItemsAndChilds(IEnumerable<IDocumentItem> documentItems, IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			var processStack = new Stack<DocumentItemExecution>();

			foreach (var documentItem in documentItems.TakeWhile(e => StopOrAbortBuilding(outputStream, context)))
			{
				processStack.Push(new DocumentItemExecution(documentItem, context));
				while (processStack.Any() && StopOrAbortBuilding(outputStream, context))
				{
					var currentDocumentItem = processStack.Pop();
					var next = await currentDocumentItem.DocumentItem.Render(outputStream, currentDocumentItem.ContextObject, scopeData);
					foreach (var item in next.Reverse()) //we have to reverse the list as the logical first item returned must be the last inserted to be the next that pops out
					{
						processStack.Push(item);
					}
				}
			}
		}
	}
}