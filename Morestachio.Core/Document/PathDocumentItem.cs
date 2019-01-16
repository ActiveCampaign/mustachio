using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		An single Value expression
	/// </summary>
	public class PathDocumentItem : DocumentItemBase
	{
		/// <inheritdoc />
		public PathDocumentItem(string value, bool escapeValue)
		{
			Value = value;
			EscapeValue = escapeValue;
		}

		/// <summary>
		///		The Path of the Expression
		/// </summary>
		public string Value { get; private set; }

		/// <summary>
		/// Gets a value indicating whether [escape value].
		/// </summary>
		/// <value>
		///   <c>true</c> if [escape value]; otherwise, <c>false</c>.
		/// </value>
		public bool EscapeValue { get; private set; }

		private static string HtmlEncodeString(string context)
		{
			return WebUtility.HtmlEncode(context);
		}
		
		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			//try to locate the value in the context, if it exists, append it.
			var contextObject = context != null ? (await context.GetContextForPath(Value, scopeData)) : null;
			if (contextObject?.Value != null)
			{
				await contextObject.EnsureValue();
				if (EscapeValue && !context.Options.DisableContentEscaping)
				{
					ContentDocumentItem.WriteContent(outputStream, HtmlEncodeString(await contextObject.RenderToString()), contextObject);
				}
				else
				{
					ContentDocumentItem.WriteContent(outputStream, await contextObject.RenderToString(), contextObject);
				}
			}
			
			return Childs.WithScope(contextObject);
		}
	}
}