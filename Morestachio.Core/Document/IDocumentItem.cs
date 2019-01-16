using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Defines a Part in the Template that can be processed
	/// </summary>
	public interface IDocumentItem
	{
		/// <summary>
		///		Renders its Value into the <see cref="outputStream"/>.
		///		If there are any Document items that should be executed directly after they should be returned		
		/// </summary>
		/// <param name="outputStream">The output stream.</param>
		/// <param name="context">The context.</param>
		/// <param name="scopeData">The scope data.</param>
		/// <returns></returns>
		Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData);

		/// <summary>
		///		The list of Childs that are children of this Document item
		/// </summary>
		IList<IDocumentItem> Childs { get; }

		/// <summary>
		///		Adds the specified childs.
		/// </summary>
		void Add(params IDocumentItem[] childs);
	}
}