using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mustachio
{
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
				//if (path.StartsWith("..."))
				//{
				//	var parent = Parent;
				//	var lastParent = parent;
				//	while (parent != null)
				//	{
				//		parent = parent.Parent;
				//		if (parent != null)
				//		{
				//			lastParent = parent;
				//		}
				//	}
					
				//	retval = lastParent.GetContextForPath(elements);
				//}
				//else 
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
				(!(Value is IEnumerable) || ((IEnumerable)Value).Cast<object>().Any()
				);
		}

		/// <summary>
		/// The default to string operator for any PrintableType
		/// </summary>
		public static FormatTemplateElement DefaultToStringWithFormatting = new FormatTemplateElement("Default string formatter", (value, formatArgument) =>
		{
			var o = value as IFormattable;
			if (o != null && formatArgument != null)
			{
				return o.ToString(formatArgument.ToString(), null);
			}
			else
			{
				return value.ToString();
			}
		});


		/// <summary>
		/// The set of allowed types that may be printed. Complex types (such as arrays and dictionaries)
		/// should not be printed, or their printing should be specialized.
		/// Add an typeof(object) entry as Type to define a Default Output
		/// </summary>
		public static readonly IDictionary<Type, FormatTemplateElement> PrintableTypes = new Dictionary<Type, FormatTemplateElement>()
		{
			{typeof(IFormattable), DefaultToStringWithFormatting},
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

		private Type SearchInCollectionForFormatter(Type type, IDictionary<Type, FormatTemplateElement> source)
		{
			FormatTemplateElement formatter;
			//look for exactly this type
			if (source.TryGetValue(type, out formatter))
			{
				return type;
			}
			//this excact type was not found. Look for derivations
			return source.FirstOrDefault(e => e.Key != null && e.Key.IsAssignableFrom(type)).Key;
		}

		private Type GetMostMatchingType(Type type)
		{
			var optionFormatterType = SearchInCollectionForFormatter(type, Options.Formatters);
			if (optionFormatterType != null)
			{
				return optionFormatterType;
			}

			return SearchInCollectionForFormatter(type, PrintableTypes);
		}

		///  <summary>
		/// 		Gets the Formatter that matches the given type most from ether the Options.Formatter or the global or null
		///  </summary>
		///  <param name="type"></param>
		/// <param name="additionalFormatters"></param>
		/// <returns></returns>
		public static FormatTemplateElement GetMostMatchingFormatter(Type type, IDictionary<Type, FormatTemplateElement> additionalFormatters)
		{
			FormatTemplateElement formatter;
			if (type == null)
			{
				if (additionalFormatters.TryGetValue(typeof(object), out formatter))
				{
					return formatter;
				}

				return PrintableTypes.TryGetValue(typeof(object), out formatter) ? formatter : null;
			}

			if (additionalFormatters.TryGetValue(type, out formatter))
			{
				return formatter;
			}

			return PrintableTypes.TryGetValue(type, out formatter) ? formatter : null;
		}

		private object CallMostMatchingFormatter(Type type, object arguments)
		{
			return CallMostMatchingFormatter(type, arguments, Value);
		}

		private object CallMostMatchingFormatter(Type type, object arguments, object value)
		{
			var hasFormatter = GetMostMatchingFormatter(type, Options.Formatters);
			if (hasFormatter == null)
			{
				return value;
			}
			return hasFormatter.Format(value, arguments);
		}

		/// <summary>
		/// Parses the current object by using the current Formatting argument
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var retval = Value;
			return retval.ToString();
		}

		/// <summary>
		/// Parses the current object by using the given argument
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public object Format(object argument)
		{
			object retval = Value;
			if (Value != null)
			{
				retval = CallMostMatchingFormatter(GetMostMatchingType(Value.GetType()), argument);
			}
			return retval;
		}

		/// <summary>
		/// Clones the ContextObject into a new Detached object
		/// </summary>
		/// <returns></returns>
		public ContextObject Clone()
		{
			var contextClone = new ContextObject();
			contextClone.CancellationToken = CancellationToken;
			contextClone.Parent = Parent;
			contextClone.Options = Options;
			contextClone.AbortGeneration = AbortGeneration;
			contextClone.Key = Key;

			if (Value is ICloneable)
			{
				contextClone.Value = (Value as ICloneable).Clone();
			}
			else
			{
				contextClone.Value = Value;
			}
			return contextClone;
		}
	}
}
