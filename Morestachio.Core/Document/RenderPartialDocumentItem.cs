using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Prints a partial
	/// </summary>
	public class RenderPartialDocumentItem : DocumentItemBase
	{
		/// <inheritdoc />
		public RenderPartialDocumentItem(string value)
		{
			Value = value;
		}

		/// <inheritdoc />
		public override string Kind { get; } = "Include";
		
		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			await Task.CompletedTask;
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

		/// <summary>
		///		The name of the Partial to print
		/// </summary>
		public string Value { get; private set; }
	}
}