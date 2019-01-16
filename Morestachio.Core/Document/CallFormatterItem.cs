using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Morestachio.Formatter;
using Morestachio.Framework;

namespace Morestachio
{
	public class CallFormatterItem : DocumentItemBase
	{
		public CallFormatterItem(FormatterPart[] formatString, string value)
		{
			FormatString = formatString;
			Value = value;
		}

		public FormatterPart[] FormatString { get; private set; }

		public string Value { get; private set; }

		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			if (context == null)
			{
				return new DocumentItemExecution[0];
			}
			var c = await context.GetContextForPath(Value, scopeData);

			if (FormatString != null && FormatString.Any())
			{
				var argList = new List<KeyValuePair<string, object>>();

				foreach (var formatterArgument in FormatString)
				{
					//if pre and suffixed by a $ its a reference to another field.
					//walk the path in the $ and use the value in the formatter
					var trimmedArg = formatterArgument.Argument.Trim();
					if (trimmedArg.StartsWith("$") &&
					    trimmedArg.EndsWith("$"))
					{
						var formatContext = await context.GetContextForPath(trimmedArg.Trim('$'), scopeData);
						await formatContext.EnsureValue();
						argList.Add(new KeyValuePair<string, object>(formatterArgument.Name, formatContext.Value));
					}
					else
					{
						argList.Add(new KeyValuePair<string, object>(formatterArgument.Name, formatterArgument.Argument));
					}
				}
				//we do NOT await the task here. We await the task only if we need the value
				context.Value = c.Format(argList.ToArray());
			}
			else
			{
				context.Value = c.Format(new KeyValuePair<string, object>[0]);
			}
			return Childs.WithScope(context);
		}
	}
}