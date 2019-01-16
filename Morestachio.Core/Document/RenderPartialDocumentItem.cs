using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	public class RenderPartialDocumentItem : DocumentItemBase
	{
		public RenderPartialDocumentItem(string value)
		{
			Value = value;
		}

		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			string partialName = Value;
			var currentPartial = partialName + "_" + scopeData.PartialDepth.Count;
			scopeData.PartialDepth.Push(currentPartial);
			if (scopeData.PartialDepth.Count >= context.Options.PartialStackSize)
			{
				switch (context.Options.StackOverflowBehavior)
				{
					case ParserOptions.PartialStackOverflowBehavior.FailWithException:
						throw new MustachioStackOverflowException(
							$"You have exceeded the maximum stack Size for nested Partial calls of '{context.Options.PartialStackSize}'. See Data for call stack")
						{
							Data =
							{
								{"Callstack", scopeData.PartialDepth}
							}
						};
						break;
					case ParserOptions.PartialStackOverflowBehavior.FailSilent:

						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			var scopeDataPartial = scopeData.Partials[partialName];
			return new DocumentItemExecution[]
			{
				new DocumentItemExecution(scopeDataPartial, context), 
			};
		}

		public string Value { get; private set; }
	}
}