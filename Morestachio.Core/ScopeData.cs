using System.Collections.Generic;

namespace Morestachio
{
	/// <summary>
	///		Hosts all infos about the current execution path of a part in the Template.
	///		Can be used for future parallel execution
	/// </summary>
	public class ScopeData
	{
		public ScopeData()
		{
			Partials = new Dictionary<string, IDocumentItem>();
			PartialDepth = new Stack<string>();
		}

		public IDictionary<string, IDocumentItem> Partials { get; private set; }

		public Stack<string> PartialDepth { get; private set; }
	}
}