using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class CollectionDocumentItem : DocumentItemBase
	{
		public CollectionDocumentItem(IDocumentItem itemDocument, string value)
		{
			ItemDocument = itemDocument;
			Value = value;
		}

		public IDocumentItem ItemDocument { get; private set; }
		public string Value { get; private set; }

		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			//if we're in the same scope, just negating, then we want to use the same object
			var c = await context.GetContextForPath(Value, scopeData);

			//"falsey" values by Javascript standards...
			if (!await c.Exists())
			{
				return new DocumentItemExecution[0];
			}

			var value = c.Value as IEnumerable;
			if (value != null && !(value is string) && !(value is IDictionary<string, object>))
			{
				var scopes = new List<DocumentItemExecution>();

				//Use this "lookahead" enumeration to allow the $last keyword
				var index = 0;
				var enumumerator = value.GetEnumerator();
				if (!enumumerator.MoveNext())
				{
					return new DocumentItemExecution[0];
				}

				var current = enumumerator.Current;
				do
				{
					var next = enumumerator.MoveNext() ? enumumerator.Current : null;
					var innerContext = new ContextCollection(index, next == null, context.Options, $"[{index}]")
					{
						Value = current,
						Parent = c
					};
					scopes.Add(new DocumentItemExecution(ItemDocument, innerContext));
					index++;
					current = next;
				} while (current != null && StopOrAbortBuilding(outputStream, context));

				return scopes;
			}
			else
			{
				throw new IndexedParseException(
					"'{0}' is used like an array by the template, but is a scalar value or object in your model.",
					Value);
			}
		}
	}
}