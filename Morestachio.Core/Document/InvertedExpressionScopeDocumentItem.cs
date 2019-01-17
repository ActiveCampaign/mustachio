using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Defines an inverted scope
	/// </summary>
	/// <seealso cref="ExpressionScopeDocumentItem"/>
	public class InvertedExpressionScopeDocumentItem : DocumentItemBase
	{
		/// <inheritdoc />
		public InvertedExpressionScopeDocumentItem(string value)
		{
			Value = value;
		}

		/// <inheritdoc />
		public override string Kind { get; } = "InvertedExpressionScope";

		/// <summary>
		///		The expression for the value that should be scoped
		/// </summary>
		public string Value { get; private set; }
		
		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			var c = await context.GetContextForPath(Value, scopeData);
			//"falsey" values by Javascript standards...
			if (!await c.Exists())
			{
				return Children.WithScope(c);
			}
			return new DocumentItemExecution[0];
		}
	}
}