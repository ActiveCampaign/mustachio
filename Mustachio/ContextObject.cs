using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mustachio
{
	public delegate object FormatTemplateElement(object sourceObject, string argument);

	public class ContextObject
	{
		private static readonly Regex _pathFinder = new Regex("(\\.\\.[\\\\/]{1})|([^.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

		public ContextObject Parent { get; set; }

		public object Value { get; set; }

		public string Key { get; set; }
		public ParserOptions Options { get; set; }

		private ContextObject GetContextForPath(Queue<String> elements)
		{
			var retval = this;
			if (elements.Any())
			{
				var element = elements.Dequeue();
				if (element.StartsWith(".."))
				{
					if (Parent != null)
					{
						retval = Parent.GetContextForPath(elements);
					}
					else
					{
						//calling "../" too much may be "ok" in that if we're at root,
						//we may just stop recursion and traverse down the path.
						retval = GetContextForPath(elements);
					}
				}
				else if (element.Equals("?"))
				{
					var innerContext = new ContextObject();
					innerContext.Options = Options;
					innerContext.Key = element;
					innerContext.Parent = this;
					innerContext.Value = Value;
					return innerContext;
				}
				//TODO: handle array accessors and maybe "special" keys.
				else
				{
					//ALWAYS return the context, even if the value is null.
					var innerContext = new ContextObject();
					innerContext.Options = Options;
					innerContext.Key = element;
					innerContext.Parent = this;
					var ctx = this.Value as IDictionary<string, object>;
					if (ctx != null)
					{
						object o;
						ctx.TryGetValue(element, out o);
						innerContext.Value = o;
					}
					else if (this.Value != null)
					{
						var propertyInfo = Value.GetType().GetProperty(element);
						if (propertyInfo != null)
						{
							innerContext.Value = propertyInfo.GetValue(Value);
						}
					}

					retval = innerContext.GetContextForPath(elements);
				}
			}
			return retval;
		}

		public ContextObject GetContextForPath(string path)
		{
			var elements = new Queue<string>();
			foreach (var m in _pathFinder.Matches(path).OfType<Match>())
			{
				elements.Enqueue(m.Value);
			}
			return GetContextForPath(elements);
		}

		/// <summary>
		/// Determines if the value of this context exists.
		/// </summary>
		/// <returns></returns>
		public bool Exists()
		{
			return Value != null &&
				Value as bool? != false &&
				Value as double? != 0 &&
				Value as int? != 0 &&
				Value as string != String.Empty &&
				// We've gotten this far, if it is an object that does NOT cast as enumberable, it exists
				// OR if it IS an enumerable and .Any() returns true, then it exists as well
				(Value as IEnumerable == null || (Value as IEnumerable).Cast<object>().Any()
				);
		}

		public static FormatTemplateElement DefaultToString = (value, formatArgument) => value.ToString();

		public static FormatTemplateElement DefaultToStringWithFormatting = (value, formatArgument) =>
			(value as IFormattable)?.ToString(formatArgument, null) ?? value.ToString();

		/// <summary>
		/// The set of allowed types that may be printed. Complex types (such as arrays and dictionaries)
		/// should not be printed, or their printing should be specialized.
		/// Add a Null as Type to define a Default Output
		/// </summary>
		public static Dictionary<Type, FormatTemplateElement> PrintableTypes = new Dictionary<Type, FormatTemplateElement>()
		{
			{typeof(IFormattable), (value, formatArgument) => (value as IFormattable).ToString(formatArgument, null)},
			{typeof(string), DefaultToStringWithFormatting},
			{typeof(bool), DefaultToStringWithFormatting},
			{typeof(char), DefaultToStringWithFormatting},
			{typeof(int), DefaultToStringWithFormatting},
			{typeof(double), DefaultToStringWithFormatting},
			{typeof(short), DefaultToStringWithFormatting},
			{typeof(float), DefaultToStringWithFormatting},
			{typeof(long), DefaultToStringWithFormatting},
			{typeof(byte), DefaultToStringWithFormatting},
			{typeof(sbyte), DefaultToStringWithFormatting},
			{typeof(decimal), DefaultToStringWithFormatting},
			{typeof(DateTime), DefaultToStringWithFormatting},
		};

		private Type GetMostMatchingType(Type type)
		{
			return Options.Formatters.FirstOrDefault(e => e.Key.IsAssignableFrom(type)).Key
			       ?? (PrintableTypes.FirstOrDefault(e => e.Key == type).Key
			           ?? PrintableTypes.FirstOrDefault(e => e.Key.IsAssignableFrom(type)).Key);
		}

		private FormatTemplateElement GetMostMatchingFormatter(Type type)
		{
			FormatTemplateElement formatter;
			if (Options.Formatters.TryGetValue(type, out formatter))
			{
				return formatter;
			}

			return PrintableTypes.FirstOrDefault(e => e.Key == type).Value;
		}

		private object CallMostMatchingFormatter(Type type, string arguments)
		{
			var hasFormatter = GetMostMatchingFormatter(type);
			if (hasFormatter == null)
			{
				return Value;
			}
			return hasFormatter(Value, arguments);
		}

		public override string ToString()
		{
			var retval = Value;
			if (Value != null)
			{
				retval = CallMostMatchingFormatter(GetMostMatchingType(Value.GetType()), null);
			}
			return retval.ToString();
		}

		public object Format(string argument)
		{
			object retval = Value;
			if (Value != null)
			{
				retval = CallMostMatchingFormatter(GetMostMatchingType(Value.GetType()), argument);
			}
			return retval;
		}
	}
}
