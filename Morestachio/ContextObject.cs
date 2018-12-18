using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Morestachio.Formatter;

namespace Morestachio
{
	/// <summary>
	///     The current context for any given expression
	/// </summary>
	public class ContextObject
	{
		static ContextObject()
		{
			DefaultFormatter = new FormatterMatcher();
			foreach (var type in new[]
			{
				typeof(IFormattable),
				typeof(string),
				typeof(bool),
				typeof(char),
				typeof(int),
				typeof(double),
				typeof(short),
				typeof(float),
				typeof(long),
				typeof(byte),
				typeof(sbyte),
				typeof(decimal),
				typeof(DateTime),
			})
			{
				//we have to use a proxy function to get around a changing delegate that maybe overwritten by the user
				//if the user overwrites the static DefaultToStringWithFormatting after we have added it to the list this would
				//have no effect
				DefaultFormatter.AddFormatter(type, new Func<object, object, object>(DefaultFormatterImpl));
			}
			DefaultDefinitionOfFalse = (value) => value != null &&
			                                      value as bool? != false &&
			                                      // ReSharper disable once CompareOfFloatsByEqualityOperator
			                                      value as double? != 0 &&
			                                      value as int? != 0 &&
			                                      value as string != string.Empty &&
			                                      // We've gotten this far, if it is an object that does NOT cast as enumberable, it exists
			                                      // OR if it IS an enumerable and .Any() returns true, then it exists as well
			                                      (!(value is IEnumerable) || ((IEnumerable) value).Cast<object>().Any()
			                                      );
			DefinitionOfFalse = DefaultDefinitionOfFalse;
		}

		/// <summary>
		///		Gets the Default Definition of false.
		///		This is ether Null, boolean false, 0 double or int, string.Empty or if collection not Any().
		///		This field can be used to define your own <see cref="DefinitionOfFalse"/> and then fallback to the default logic
		/// </summary>
		public static readonly Func<object, bool> DefaultDefinitionOfFalse;
		
		
		/// <summary>
		///		Gets the Definition of false on your Template.
		/// </summary>
		/// <value>
		///		Must no be null
		/// </value>
		/// <exception cref="InvalidOperationException">If the value is null</exception>
		[NotNull]
		public static Func<object, bool> DefinitionOfFalse
		{
			get { return _definitionOfFalse; }
			set
			{
				_definitionOfFalse = value ?? throw new InvalidOperationException("The value must not be null");
			}
		}

		internal static readonly Regex PathFinder = new Regex("(\\.\\.[\\\\/]{1})|([^.]+)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

		/// <summary>
		///		Calls <seealso cref="DefaultToStringWithFormatting"/>
		/// </summary>
		/// <param name="sourceValue">The source value.</param>
		/// <param name="formatterArgument">The formatter argument.</param>
		/// <returns></returns>
		public static object DefaultFormatterImpl(object sourceValue, object formatterArgument = null)
		{
			return DefaultToStringWithFormatting(sourceValue, formatterArgument);
		}

		/// <summary>
		///     The default to string operator for any PrintableType.
		///		Can be overwritten to support an alternative formatting of all templates.
		/// </summary>
		[NotNull]
		public static Func<object, object, object> DefaultToStringWithFormatting = new Func<object, object, object>(
			(value, formatArgument) =>
			{
				var o = value as IFormattable;
				if (o != null && formatArgument != null)
				{
					return o.ToString(formatArgument.ToString(), null);
				}

				return value.ToString();
			});

		private static Func<object, bool> _definitionOfFalse;

		/// <summary>
		/// Initializes a new instance of the <see cref="ContextObject"/> class.
		/// </summary>
		/// <param name="options">The options.</param>
		/// <param name="key">The key as seen in the Template</param>
		public ContextObject([NotNull]ParserOptions options, [NotNull]string key)
		{
			Options = options;
			Key = key;
		}

		/// <summary>
		///     The set of allowed types that may be printed. Complex types (such as arrays and dictionaries)
		///     should not be printed, or their printing should be specialized.
		///     Add an typeof(object) entry as Type to define a Default Output
		/// </summary>
		[NotNull]
		public static FormatterMatcher DefaultFormatter { get; private set; }

		/// <summary>
		///     The parent of the current context or null if its the root context
		/// </summary>
		[CanBeNull]
		public ContextObject Parent { get; set; }

		/// <summary>
		///     The evaluated value of the expression
		/// </summary>
		[CanBeNull]
		public object Value { get; set; }

		/// <summary>
		///	Ensures that the Value is loaded if needed
		/// </summary>
		/// <returns></returns>
		public async Task EnsureValue()
		{
			if (Value is Task task)
			{
				await task;
				if (task is Task<object>)
				{
					Value = ((Task<object>) Value).Result;
				}
				else
				{
					Value = null;
				}
			}
		}

		/// <summary>
		///     is an abort currently requested
		/// </summary>
		public bool AbortGeneration { get; set; }

		/// <summary>
		///     The name of the property or key inside the value or indexer expression for lists
		/// </summary>
		[NotNull]
		public string Key { get; set; }

		/// <summary>
		///     With what options are the template currently is running
		/// </summary>
		[NotNull]
		public ParserOptions Options { get; }

		/// <summary>
		/// </summary>
		public CancellationToken CancellationToken { get; set; }


		/// <summary>
		///     if overwritten by a class it returns a context object for any non standard key or operation.
		///     if non of that
		///     <value>null</value>
		/// </summary>
		/// <param name="elements"></param>
		/// <param name="currentElement"></param>
		/// <returns></returns>
		protected virtual ContextObject HandlePathContext(Queue<string> elements, string currentElement)
		{
			return null;
		}

		private async Task<ContextObject> GetContextForPath(Queue<string> elements)
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
				if (path.StartsWith("~")) //go the root object
				{
					var parent = Parent;
					var lastParent = parent;
					while (parent != null)
					{
						parent = parent.Parent;
						if (parent != null)
						{
							lastParent = parent;
						}
					}

					retval = await lastParent?.GetContextForPath(elements);
				}
				else if (path.StartsWith("..")) //go one level up
				{
					if (Parent != null)
					{
						var parentsRetVal = (await Parent.GetContextForPath(elements));
						if (parentsRetVal != null)
						{
							retval = parentsRetVal;
						}
						else
						{
							retval = await GetContextForPath(elements);
						}
					}
					else
					{
						retval = await GetContextForPath(elements);
					}
				}
				//TODO: handle array accessors and maybe "special" keys.
				else
				{
					//ALWAYS return the context, even if the value is null.
					var innerContext = new ContextObject(Options, path)
					{
						Parent = this
					};
					await EnsureValue();
					if (Value is IDictionary<string, object> ctx)
					{
						ctx.TryGetValue(path, out var o);
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

					retval = await innerContext.GetContextForPath(elements);
				}
			}

			return retval;
		}

		/// <summary>
		///     Will walk the path by using the path seperator "." and evaluate the object at the end
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public async Task<ContextObject> GetContextForPath(string path)
		{
			var elements = new Queue<string>();
			foreach (var m in PathFinder.Matches(path).OfType<Match>())
			{
				elements.Enqueue(m.Value);
			}

			return await GetContextForPath(elements);
		}

		/// <summary>
		///     Determines if the value of this context exists.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> Exists()
		{
			await EnsureValue();
			return DefinitionOfFalse(Value);
		}

		/// <summary>
		///		Renders the Current value to a string or if null to the Null placeholder in the Options
		/// </summary>
		/// <returns></returns>
		public async Task<string> RenderToString()
		{
			await EnsureValue();
			return Value?.ToString() ?? Options.Null;
		}

		/// <summary>
		///     Parses the current object by using the current Formatting argument
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Value?.ToString() ?? Options.Null;
		}

		/// <summary>
		///     Parses the current object by using the given argument
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public async Task<object> Format(KeyValuePair<string, object>[] argument)
		{
			await EnsureValue();
			var retval = Value;
			if (Value == null)
			{
				return retval;
			}

			//call formatters that are given by the Options for this run
			retval = await Options.Formatters.CallMostMatchingFormatter(Value.GetType(), argument, Value);
			if ((retval as FormatterMatcher.FormatterFlow) != FormatterMatcher.FormatterFlow.Skip)
			{
				//one formatter has returned a valid value so use this one.
				return retval;
			}

			//all formatters in the options object have rejected the value so try use the global ones
			retval = await DefaultFormatter.CallMostMatchingFormatter(Value.GetType(), argument, Value);
			if ((retval as FormatterMatcher.FormatterFlow) != FormatterMatcher.FormatterFlow.Skip)
			{
				return retval;
			}
			return Value;
		}

		/// <summary>
		///     Clones the ContextObject into a new Detached object
		/// </summary>
		/// <returns></returns>
		public ContextObject Clone()
		{
			var contextClone = new ContextObject(Options,Key)
			{
				CancellationToken = CancellationToken,
				Parent = Parent,
				AbortGeneration = AbortGeneration,
				Value = Value
			};

			return contextClone;
		}
	}
}