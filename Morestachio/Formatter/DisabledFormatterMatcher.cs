using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Morestachio.Formatter
{
	/// <summary>
	///		This is the Dummy formatter matcher that can be used to completely disable the Formatter syntax
	/// </summary>
	/// <seealso cref="Morestachio.Formatter.IFormatterMatcher" />
	[PublicAPI]
	public class DisabledFormatterMatcher : IFormatterMatcher
	{
		private FormatTemplateElement ConstructEmptyMatcher()
		{
			return new FormatTemplateElement(new Action(() => { }), typeof(object), typeof(object), new MultiFormatterInfo[0]);
		}

		/// <inheritdoc />
		public FormatTemplateElement AddFormatter<T>(Delegate formatterDelegate)
		{
			return ConstructEmptyMatcher();
		}

		/// <inheritdoc />
		public FormatTemplateElement AddFormatter(FormatTemplateElement formatter)
		{
			return ConstructEmptyMatcher();
		}

		/// <inheritdoc />
		public FormatTemplateElement AddFormatter(Type forType, Delegate formatterDelegate)
		{
			return ConstructEmptyMatcher();
		}

		/// <inheritdoc />
		public async Task<object> Execute(FormatTemplateElement formatter, object sourceObject, params KeyValuePair<string, object>[] templateArguments)
		{
			await Task.CompletedTask;
			return null;
		}

		/// <inheritdoc />
		public IEnumerable<FormatTemplateElement> GetMostMatchingFormatter(Type type, KeyValuePair<string, object>[] arguments)
		{
			yield break;
		}

		/// <inheritdoc />
		public async Task<object> CallMostMatchingFormatter(Type type, KeyValuePair<string, object>[] arguments, object value)
		{			
			await Task.CompletedTask;
			return null;
		}
	}
}