using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		If a formatter ends without using its value it is printed
	/// </summary>
	public class PrintFormattedItem : DocumentItemBase
	{

		/// <inheritdoc />
		public override string Kind { get; } = "PrintExpression";

		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			if (context != null)
			{
				string value = null;
				await context.EnsureValue();
				if (context.Value != null)
				{
					value = await context.RenderToString();
				}

				ContentDocumentItem.WriteContent(outputStream, value, context);
			}
			
			return Children.WithScope(context);
		}
	}
}