using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Morestachio.Attributes;

namespace Morestachio.Formatter
{
	/// <summary>
	///     Matches the Arguments from the Template to a Function from .net
	/// </summary>
	public class FormatterMatcher
	{
		/// <summary>
		/// </summary>
		public FormatterMatcher()
		{
			Formatter = new List<FormatTemplateElement>();
		}

		/// <summary>
		///     If set writes the Formatters log.
		/// </summary>
		public TextWriter FormatterLog { get; set; }

		/// <summary>
		///     The Enumeration of all formatter
		/// </summary>
		[NotNull]
		[ItemNotNull]
		public ICollection<FormatTemplateElement> Formatter { get; }

		private class FormatterGenericTypeCache
		{
			public FormatTemplateElement Formatter { get; set; }
			public Type GenericInputType { get; set; }
		}

		private IDictionary<FormatterGenericTypeCache, MethodInfo> _formatterCache;

		/// <summary>
		/// Writes the specified log.
		/// </summary>
		/// <param name="log">The log.</param>
		public void Write(Func<string> log)
		{
			FormatterLog?.WriteLine(log());
		}

		/// <summary>
		///     Gets the matching formatter.
		/// </summary>
		/// <param name="typeToFormat">The type to format.</param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		[CanBeNull]
		public virtual IEnumerable<FormatTemplateElement> GetMatchingFormatter([NotNull] Type typeToFormat,
			[NotNull] KeyValuePair<string, object>[] arguments)
		{
			Write(() =>
				$"Test Filter for '{typeToFormat}' with arguments '{arguments.Select(e => $"[{e.Key}]:\"{e.Value}\"").Aggregate((e, f) => e + " & " + f)}'");

			var filteredSourceList = new List<KeyValuePair<FormatTemplateElement, ulong>>();
			foreach (var formatTemplateElement in Formatter)
			{
				var formatter = formatTemplateElement;

				Write(() => $"Test filter: '{formatter.InputTypes} : {formatter.Format.Method.Name}'");

				if (formatTemplateElement.InputTypes != typeToFormat &&
					!formatTemplateElement.InputTypes.IsAssignableFrom(typeToFormat))
				{
					var typeToFormatGenerics = typeToFormat.GetGenericArguments();

					//explicit check for array support
					if (typeToFormat.HasElementType)
					{
						var elementType = typeToFormat.GetElementType();
						typeToFormatGenerics = typeToFormatGenerics.Concat(new[] { elementType }).ToArray();
					}

					//the type check has maybe failed because of generic parameter. Check if both the formatter and the typ have generic arguments

					var formatterGenerics = formatTemplateElement.InputTypes.GetGenericArguments();

					if (typeToFormatGenerics.Length <= 0 || formatterGenerics.Length <= 0 ||
						typeToFormatGenerics.Length != formatterGenerics.Length)
					{
						Write(() =>
							$"Exclude because formatter accepts '{formatTemplateElement.InputTypes}' is not assignable from '{typeToFormat}'");
						continue;
					}
				}

				//count rest arguments
				var mandatoryArguments = formatter.MetaData.Where(e => !e.IsRestObject && !e.IsOptional && !e.IsSourceObject).ToArray();
				if (mandatoryArguments.Length > arguments.Length)
				//if there are less arguments excluding rest then parameters
				{
					Write(() =>
						"Exclude because formatter has " +
						$"'{mandatoryArguments.Length}' " +
						"parameter and " +
						$"'{formatter.MetaData.Count(e => e.IsRestObject)}' " +
						"rest parameter but needs less or equals" +
						$"'{arguments.Length}'.");
					continue;
				}

				ulong score = 1L;
				if (formatter.Format.Method.ReturnParameter == null || formatter.Format.Method.ReturnParameter.ParameterType == typeof(void))
				{
					score++;
				}

				score = score + (ulong)(arguments.Length - mandatoryArguments.Length);
				Write(() => $"Take filter: '{formatter.InputTypes} : {formatter.Format}' Score {score}");
				filteredSourceList.Add(new KeyValuePair<FormatTemplateElement, ulong>(formatter, score));
			}

			foreach (var formatTemplateElement in filteredSourceList.OrderBy(e => e.Value))
			{
				yield return formatTemplateElement.Key;
			}
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public virtual FormatTemplateElement AddFormatter<T>([NotNull] Delegate formatterDelegate)
		{
			return AddFormatter(typeof(T), formatterDelegate);
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public virtual FormatTemplateElement AddFormatter<T>([NotNull] Func<T> formatterDelegate)
		{
			return AddFormatter(typeof(T), formatterDelegate);
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public virtual FormatTemplateElement AddFormatter<T, TResult>([NotNull] Func<T, TResult> formatterDelegate)
		{
			return AddFormatter(typeof(T), formatterDelegate);
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <typeparam name="TArgument"></typeparam>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public virtual FormatTemplateElement AddFormatter<T, TArgument, TResult>(
			[NotNull] Func<T, TArgument, TResult> formatterDelegate)
		{
			return AddFormatter(typeof(T), formatterDelegate);
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <param name="formatter">The formatter.</param>
		public virtual FormatTemplateElement AddFormatter([NotNull] FormatTemplateElement formatter)
		{
			Formatter.Add(formatter);
			return formatter;
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <param name="forType">For type.</param>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public virtual FormatTemplateElement AddFormatter([NotNull] Type forType, [NotNull] Delegate formatterDelegate)
		{
			var arguments = formatterDelegate.Method.GetParameters().Select((e, index) =>
				new MultiFormatterInfo(
					e.ParameterType,
					e.GetCustomAttribute<FormatterArgumentNameAttribute>()?.Name ?? e.Name,
					e.IsOptional,
					index,
					e.GetCustomAttribute<ParamArrayAttribute>() != null || e.GetCustomAttribute<RestParameterAttribute>() != null)
				{
					IsSourceObject = e.GetCustomAttribute<SourceObjectAttribute>() != null
				}).ToArray();

			var returnValue = formatterDelegate.Method.ReturnParameter?.ParameterType;

			//if there is no declared SourceObject then check if the first object is of type what we are formatting and use this one.
			if (!arguments.Any(e => e.IsSourceObject) && arguments.Any() && arguments[0].Type.IsAssignableFrom(forType))
			{
				arguments[0].IsSourceObject = true;
			}

			var sourceValue = arguments.FirstOrDefault(e => e.IsSourceObject);
			if (sourceValue != null)
			{
				//if we have a source value in the arguments reduce the index of all following 
				//this is important as the source value is never in the formatter string so we will not "count" it 
				for (var i = sourceValue.Index; i < arguments.Length; i++)
				{
					arguments[i].Index--;
				}

				sourceValue.Index = -1;
			}

			return AddFormatter(new FormatTemplateElement(
				formatterDelegate,
				forType,
				returnValue,
				arguments));
		}

		/// <summary>
		///     Composes the values into a Dictionary for each formatter. If returns null, the formatter will not be called.
		/// </summary>
		/// <param name="formatter">The formatter.</param>
		/// <param name="sourceObject">The source object.</param>
		/// <param name="templateArguments">The template arguments.</param>
		/// <returns></returns>
		public virtual IDictionary<MultiFormatterInfo, object> ComposeValues([NotNull] FormatTemplateElement formatter,
			[CanBeNull] object sourceObject, [NotNull] params KeyValuePair<string, object>[] templateArguments)
		{
			Write(() =>
				$"Compose values for object '{sourceObject}' with formatter '{formatter.InputTypes}' targets '{formatter.Format.Method.Name}'");
			var values = new Dictionary<MultiFormatterInfo, object>();
			var matched = new Dictionary<MultiFormatterInfo, KeyValuePair<string, object>>();

			foreach (var multiFormatterInfo in formatter.MetaData.Where(e => !e.IsRestObject))
			{
				Write(() => $"Match parameter '{multiFormatterInfo.Type}' [{multiFormatterInfo.Name}]");
				object givenValue;
				//set ether the source object or the value from the given arguments
				if (multiFormatterInfo.IsSourceObject)
				{
					Write(() => "Is Source object");
					givenValue = sourceObject;
				}
				else
				{
					//match by index or name
					Write(() => "Match by Name");
					//match by name
					var match = templateArguments.FirstOrDefault(e =>
						!string.IsNullOrWhiteSpace(e.Key) && e.Key.Equals(multiFormatterInfo.Name));

					if (default(KeyValuePair<string, object>).Equals(match))
					{
						Write(() => "Match by Index");
						//match by index
						var index = 0;
						match = templateArguments.FirstOrDefault(g => index++ == multiFormatterInfo.Index);
					}

					givenValue = match.Value;
					Write(() => $"Matched '{match.Key}': '{match.Value}' by Name/Index");

					//check for matching types
					if (!multiFormatterInfo.Type.IsInstanceOfType(match.Value))
					{
						Write(() => "Skip: Match is Invalid because types from Template and Formatter mismatch. Abort.");
						//The type in the template and the type defined in the formatter do not match. Abort
						return null;
					}

					matched.Add(multiFormatterInfo, match);
				}

				values.Add(multiFormatterInfo, givenValue);
				if (multiFormatterInfo.IsOptional || multiFormatterInfo.IsSourceObject)
				{
					continue; //value and source object are optional so we do not to check for its existence 
				}

				if (Equals(givenValue, null))
				{
					Write(() =>
						"Skip: Match is Invalid because template value is null where the Formatter does not have a optional value");
					//the delegates parameter is not optional so this formatter does not fit. Continue.
					return null;
				}
			}

			var hasRest = formatter.MetaData.FirstOrDefault(e => e.IsRestObject);
			if (hasRest == null)
			{
				return values;
			}

			Write(() => $"Match Rest argument '{hasRest.Type}'");

			//only use the values that are not matched.
			var restValues = templateArguments.Except(matched.Values);

			if (typeof(KeyValuePair<string, object>[]) == hasRest.Type)
			{
				//keep the name value pairs
				values.Add(hasRest, restValues);
			}
			else if (typeof(object[]).IsAssignableFrom(hasRest.Type))
			{
				//its requested to transform the rest values and truncate the names from it.
				values.Add(hasRest, restValues.Select(e => e.Value).ToArray());
			}
			else
			{
				Write(() => $"Skip: Match is Invalid because  '{hasRest.Type}' is no supported rest parameter");
				//unknown type in params argument cannot call
				return null;
			}

			return values;
		}

		/// <summary>
		///     Executes the specified formatter.
		/// </summary>
		/// <param name="formatter">The formatter.</param>
		/// <param name="sourceObject">The source object.</param>
		/// <param name="templateArguments">The template arguments.</param>
		/// <returns></returns>
		public virtual object Execute([NotNull] FormatTemplateElement formatter,
			[CanBeNull] object sourceObject,
			params KeyValuePair<string, object>[] templateArguments)
		{
			var values = ComposeValues(formatter, sourceObject, templateArguments);

			if (values == null)
			{
				Write(() => "Skip: Execute skip as Compose Values returned an invalid value");
				return FormatterFlow.Skip;
			}

			if (formatter.CanFormat != null)
			{
				if (!formatter.CanFormat(sourceObject, templateArguments))
				{
					Write(() => "Skip: Execute skip as CanExecute is false.");
					return FormatterFlow.Skip;
				}
			}

			Write(() => $"Execute");
			return formatter.Format.DynamicInvoke(values.Select(e => e.Value).ToArray());
		}

		/// <summary>
		///     Gets the Formatter that matches the type or is assignable to that type. If null it will search for a object
		///     formatter
		/// </summary>
		/// <param name="type"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public IEnumerable<FormatTemplateElement> GetMostMatchingFormatter([CanBeNull] Type type,
			KeyValuePair<string, object>[] arguments)
		{
			if (type == null)
			{
				return GetMatchingFormatter(typeof(object), arguments);
			}

			return GetMatchingFormatter(type, arguments);
		}

		/// <summary>
		///     Searches for the first formatter does not reject the value.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="arguments">The arguments.</param>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public object CallMostMatchingFormatter(Type type, KeyValuePair<string, object>[] arguments, object value)
		{
			Write(() => "---------------------------------------------------------------------------------------------");
			Write(() => $"Call Formatter for Type '{type}' on '{value}'");
			var hasFormatter = GetMostMatchingFormatter(type, arguments).Where(e => e != null);

			foreach (var formatTemplateElement in hasFormatter)
			{
				Write(() => $"Try formatter '{formatTemplateElement.InputTypes}' on '{formatTemplateElement.Format.Method.Name}'");
				var executeFormatter = Execute(formatTemplateElement, value, arguments);
				if (executeFormatter as FormatterFlow != FormatterFlow.Skip)
				{
					Write(() => $"Success. return object {executeFormatter}");
					return executeFormatter;
				}
				Write(() => $"Formatter returned '{executeFormatter}'. Try another");
			}

			Write(() => "No Formatter has matched. Skip and return Source Value.");

			return FormatterFlow.Skip;
		}

		/// <summary>
		///     Can be returned by a Formatter to control what formatter should be used
		/// </summary>
		public class FormatterFlow
		{
			private FormatterFlow()
			{
			}

			/// <summary>
			///     Return code for all formatters to skip the execution of the current formatter and try another one that could also
			///     match
			/// </summary>
			public static FormatterFlow Skip { get; } = new FormatterFlow();
		}
	}
}