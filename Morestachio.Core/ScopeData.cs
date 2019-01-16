using System.Collections.Generic;

namespace Morestachio
{
	/// <summary>
	///		Hosts all infos about the current execution path of a part in the Template.
	///		Can be used for future parallel execution
	/// </summary>
	public class ScopeData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScopeData"/> class.
		/// </summary>
		public ScopeData()
		{
			Partials = new Dictionary<string, IDocumentItem>();
			PartialDepth = new Stack<string>();
		}

		/// <summary>
		///		List of all Partials
		/// </summary>
		public IDictionary<string, IDocumentItem> Partials { get; private set; }

		/// <summary>
		///		The Depth of current Partial usage
		/// </summary>
		public Stack<string> PartialDepth { get; private set; }
	}
}