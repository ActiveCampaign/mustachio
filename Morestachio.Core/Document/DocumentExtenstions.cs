using System.Collections.Generic;
using Morestachio.Framework;

namespace Morestachio
{
	public static class DocumentExtenstions
	{
		public static IEnumerable<DocumentItemExecution> WithScope(this IEnumerable<IDocumentItem> items, ContextObject contextObject)
		{
			foreach (var documentItem in items)
			{
				yield return new DocumentItemExecution(documentItem, contextObject);
			}
		}
	}
}