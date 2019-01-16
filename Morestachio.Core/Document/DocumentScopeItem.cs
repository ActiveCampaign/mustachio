using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Defines the start of a Scope
	/// </summary>
	public class DocumentScopeItem : DocumentItemBase
	{
		/// <inheritdoc />
		public DocumentScopeItem(string value)
		{
			Value = value;
		}

		/// <summary>
		///		The expression for the value that should be scoped
		/// </summary>
		public string Value { get; private set; }

		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			var c = await context.GetContextForPath(Value, scopeData);
			//"falsey" values by Javascript standards...
			if (await c.Exists())
			{
				return Childs.WithScope(c);
			}
			return new DocumentItemExecution[0];
		}
	}
}