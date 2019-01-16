using System.Collections.Generic;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Helper Functions for Document creation
	/// </summary>
	public static class DocumentExtenstions
	{
		/// <summary>
		///		
		/// </summary>
		public static IEnumerable<DocumentItemExecution> WithScope(this IEnumerable<IDocumentItem> items, ContextObject contextObject)
		{
			foreach (var documentItem in items)
			{
				yield return new DocumentItemExecution(documentItem, contextObject);
			}
		}
	}
}