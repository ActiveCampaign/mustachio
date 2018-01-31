using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mustachio
{
	/// <summary>
	/// delegate for formatting template pars
	/// </summary>
	/// <param name="sourceObject">the object that this formatter should be applyed to</param>
	/// <param name="argument">the string argument as given in the template</param>
	/// <returns>a new object or the same object or a string</returns>
	public delegate object FormatTemplateElement(object sourceObject, string argument);

	/// <summary>
	/// The current context for any given expression
	/// </summary>
	public class ContextObject
	{
		private static readonly Regex _pathFinder = new Regex("(\\.\\.[\\\\/]{1})|([^.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

		/// <summary>
		/// The parent of the current context or null if its the root context
		/// </summary>
		public ContextObject Parent { get; set; }

		/// <summary>
		/// The evaluated value of the expression
		/// </summary>
		public object Value { get; set; }

		/// <summary>
		/// is an abort currenty requested
		/// </summary>
		public bool AbortGeneration { get; set; }
		/// <summary>
		/// The name of the property or key inside the value or indexer expression for lists
		/// </summary>
		public string Key { get; set; }

		/// <summary>
		/// With what options are the template currently is running
		/// </summary>
		public ParserOptions Options { get; set; }

		/// <summary>
		///
		/// </summary>
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Defines the Format argument in the current Context
		/// </summary>
		public string FormatAs { get; set; }

		/// <summary>
		/// if overwritten by a class it returns a context object for any non standard key or operation.
		/// if non of that <value>null</value>
		/// </summary>
		/// <param name="elements"></param>
		/// <param name="currentElement"></param>
		/// <returns></returns>
		protected virtual ContextObject HandlePathContext(Queue<string> elements, string currentElement)
		{
			return null;
		}

		private ContextObject GetContextForPath(Queue<String> elements)
		{
			var retval = this;
			if (elements.Any())
			{
				var path = elements.Dequeue();
				var preHandeld = HandlePathContext(elements, path);
				if (preHandeld != null)
				{
					return preHandeld;
				}

				if (path.StartsWith(".."))
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
				else if (path.Equals("?"))
				{
					var innerContext = new ContextObject();
					innerContext.Options = Options;
					innerContext.Key = path;
					innerContext.Parent = this;
					innerContext.Value = CallMostMatchingFormatterForFormatter(Value.GetType(), FormatAs, Value);
					return innerContext;
				}
				//TODO: handle array accessors and maybe "special" keys.
				else
				{
					//ALWAYS return the context, even if the value is null.
					var innerContext = new ContextObject();
					innerContext.Options = Options;
					innerContext.Key = path;
					innerContext.Parent = this;
					var ctx = Value as IDictionary<string, object>;
					if (ctx != null)
					{
						object o;
						ctx.TryGetValue(path, out o);
						innerContext.Value = o;
					}
					else if (Value != null)
					{
						var propertyInfo = Value.GetType().GetProperty(path);
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

		/// <summary>
		/// Will walk the path by using the path seperator "." and evaluate the object at the end
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
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
				(!(Value is IEnumerable) || ((IEnumerable) Value).Cast<object>().Any()
				);
		}

		/// <summary>
		/// The default to string operator for any PrintableType
		/// </summary>
		public static FormatTemplateElement DefaultToStringWithFormatting = (value, formatArgument) =>
		{
			var o = value as IFormattable;
			return o != null ? o.ToString(formatArgument, null) : value.ToString();
		};


		/// <summary>
		/// The set of allowed types that may be printed. Complex types (such as arrays and dictionaries)
		/// should not be printed, or their printing should be specialized.
		/// Add a Null as Type to define a Default Output
		/// </summary>
		public static Dictionary<Type, FormatTemplateElement> PrintableTypes = new Dictionary<Type, FormatTemplateElement>()
		{
			{typeof(IFormattable), (value, formatArgument) => ((IFormattable) value).ToString(formatArgument, null)},
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
			if (Options.Formatters.FirstOrDefault(e => e.Key == type).Key != null)
			{
				return Options.Formatters.FirstOrDefault(e => e.Key == type).Key;
			}
			else
			{
				if (Options.Formatters.FirstOrDefault(e => e.Key.IsAssignableFrom(type)).Key != null)
				{
					return Options.Formatters.FirstOrDefault(e => { return e.Key != null && e.Key.IsAssignableFrom(type); }).Key;
				}
				if (PrintableTypes.FirstOrDefault(e => e.Key == type).Key != null)
				{
					return PrintableTypes.FirstOrDefault(e => e.Key == type).Key;
				}
				else
				{
					return PrintableTypes.FirstOrDefault(e => { return e.Key != null && e.Key.IsAssignableFrom(type); }).Key;
				}
			}
		}

		private FormatTemplateElement GetMostMatchingFormatter(Type type)
		{
			if (type == null)
			{
				return null;
			}

			FormatTemplateElement formatter;
			if (Options.Formatters.TryGetValue(type, out formatter))
			{
				return formatter;
			}

			if (PrintableTypes.TryGetValue(type, out formatter))
			{
				return formatter;
			}

			return null;
		}

		private object CallMostMatchingFormatterForFormatter(Type type, string arguments, object value)
		{
			if (string.IsNullOrWhiteSpace(arguments))
			{
				return value;
			}

			return CallMostMatchingFormatter(type, arguments, value);
		}

		private object CallMostMatchingFormatter(Type type, string arguments)
		{
			return CallMostMatchingFormatter(type, arguments, Value);
		}

		private object CallMostMatchingFormatter(Type type, string arguments, object value)
		{
			var hasFormatter = GetMostMatchingFormatter(type);
			if (hasFormatter == null)
			{
				return value;
			}
			return hasFormatter(value, arguments);
		}

		/// <summary>
		/// Parses the current object by using the current Formatting argument
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var retval = Value;
			if (Value != null)
			{
				retval = CallMostMatchingFormatter(GetMostMatchingType(Value.GetType()), FormatAs);
			}
			return retval.ToString();
		}

		/// <summary>
		/// Parses the current object by using the given argument
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public object Format(string argument)
		{
			object retval = Value;
			if (Value != null)
			{
				retval = CallMostMatchingFormatter(GetMostMatchingType(Value.GetType()), argument);
			}
			return retval;
		}

		/// <summary>
		/// Is the current object formatted or "real"
		/// </summary>
		/// <returns></returns>
		public bool IsFormattable()
		{
			return !string.IsNullOrWhiteSpace(FormatAs);
		}
	}
}
