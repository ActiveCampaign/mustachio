using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Morestachio.Attributes;
using Morestachio.Helper;

namespace Morestachio.Formatter.Framework
{
	/// <summary>
	///		Delegate for mapping formatter function of the Morestachio framework to the params argument
	/// </summary>
	/// <param name="originalObject">The original object.</param>
	/// <param name="name">The name.</param>
	/// <param name="arguments">The arguments.</param>
	/// <returns></returns>
	public delegate object MorstachioFormatter([SourceObject] object originalObject, [FormatterArgumentName("Name")]string name,
		params object[] arguments);

	/// <summary>
	///		The Formatter service that can be used to interpret the Native C# formatter.
	///		To use this kind of formatter you must create a public static class where all formatting functions are located.
	///		Then create a public static function that accepts n arguments of the type you want to format. For Example:
	///		If the formatter should be only used for int formatting and the argument will always be a string you have to create a function that has this header.
	///		It must not return a value.
	///		The function must have the MorestachioFormatter attribute
	/// </summary>
	public class MorestachioFormatterService
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MorestachioFormatterService"/> class.
		/// </summary>
		public MorestachioFormatterService()
		{
			GlobalFormatterModels = new List<MorestachioFormatterModel>();
		}

		/// <summary>
		///		Add all formatter into the given options object
		/// </summary>
		/// <param name="listOfFormatter">The list of formatter.</param>
		/// <param name="options">The options.</param>
		[PublicAPI]
		public void AddFormatterToMorestachio(IEnumerable<MorestachioFormatterModel> listOfFormatter, ParserOptions options)
		{
			foreach (var formatterGroup in listOfFormatter.GroupBy(e => e.InputType).ToArray())
			{
				options.Formatters.AddFormatter(formatterGroup.Key,
					new MorstachioFormatter((sourceObject, name, arguments) =>
						FormatConditonal(sourceObject, name, arguments, formatterGroup)));
			}
		}

		private object FormatConditonal(object sourceObject, string name, object[] arguments,
			IEnumerable<MorestachioFormatterModel> formatterGroup)
		{
			if (name == null)
			{
				return FormatterMatcher.FormatterFlow.Skip;
			}
			var directMatch = formatterGroup.Where(e => (name.ToString().Equals(e.Name)));
			var originalObject = sourceObject;

			foreach (var morestachioFormatterModel in directMatch)
			{
				if (sourceObject == null)
				{
					continue;
				}

				var type = sourceObject.GetType();
				if (!morestachioFormatterModel.InputType.ContainsGenericParameters)
				{
					if (!morestachioFormatterModel.InputType.IsInstanceOfType(sourceObject))
					{
						continue;
					}

					sourceObject = morestachioFormatterModel.Function.Invoke(null, new[] { sourceObject }.Concat(arguments).ToArray());
					if (sourceObject == null || !sourceObject.Equals(originalObject))
					{
						return sourceObject;
					}

					continue;
				}


				var localGen = morestachioFormatterModel.InputType.GetGenericArguments();
				var templateGen = type.GetGenericArguments();

				if (localGen.Any() != templateGen.Any())
				{
					if (type.IsArray)
					{
						templateGen = new[] { type.GetElementType() };
					}
					else
					{
						continue;
					}
				}

				if (!morestachioFormatterModel.InputType.ContainsGenericParameters)
				{
					continue;
				}

				if (localGen.Length != templateGen.LongLength)
				{
					continue;
				}

				try
				{
					var makeGenericMethod = morestachioFormatterModel.Function.MakeGenericMethod(templateGen);
					sourceObject = makeGenericMethod.Invoke(null, new[] { sourceObject }.Concat(arguments).ToArray());
					if (sourceObject == null || !sourceObject.Equals(originalObject))
					{
						return sourceObject;
					}
				}
				catch (Exception)
				{
					continue;
				}
			}

			return sourceObject == null ? originalObject : FormatterMatcher.FormatterFlow.Skip;
		}

		/// <summary>
		///		Adds all formatter that are decorated with the <see cref="MorestachioFormatterAttribute"/>
		/// </summary>
		/// <param name="type">The type.</param>
		public void AddFromType(Type type)
		{
			foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				var hasFormatterAttr = method.GetCustomAttributes<MorestachioFormatterAttribute>();
				foreach (var morestachioFormatterAttribute in hasFormatterAttr)
				{
					if (morestachioFormatterAttribute == null || method.ReturnType == typeof(void) || method.GetParameters().Length != 2)
					{
						continue;
					}

					var morestachioFormatterModel = new MorestachioFormatterModel(morestachioFormatterAttribute.Name, morestachioFormatterAttribute.Description,
						method.GetParameters().FirstOrDefault()?.ParameterType,
						morestachioFormatterAttribute.OutputType ?? method.ReturnType,
						method.GetCustomAttributes<MorestachioFormatterInputAttribute>().Select(e => new InputDescription(e.Description, e.OutputType, e.Example)).ToArray(),
						morestachioFormatterAttribute.ReturnHint, method);
					GlobalFormatterModels.Add(morestachioFormatterModel);
				}
			}
		}

		/// <summary>
		/// Gets the gloabl formatter that are used always for any formatting run.
		/// </summary>
		public ICollection<MorestachioFormatterModel> GlobalFormatterModels { get; private set; }
	}
}
