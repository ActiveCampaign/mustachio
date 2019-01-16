using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class MorestachioDocument : DocumentItemBase
	{
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			await ProcessItemsAndChilds(Childs, outputStream, context, scopeData);
			return new DocumentItemExecution[0];
		}

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