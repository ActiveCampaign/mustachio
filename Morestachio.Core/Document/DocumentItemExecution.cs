using Morestachio.Framework;

namespace Morestachio
{
	public struct DocumentItemExecution
	{
		public DocumentItemExecution(IDocumentItem documentItem, ContextObject contextObject)
		{
			DocumentItem = documentItem;
			ContextObject = contextObject;
		}

		public IDocumentItem DocumentItem { get; private set; }
		public ContextObject ContextObject { get; private set; }
	}
}