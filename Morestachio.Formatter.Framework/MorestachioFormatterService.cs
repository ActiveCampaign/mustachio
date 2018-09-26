using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Morestachio.Helper;

namespace Morestachio.Formatter.Framework
{
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
			GloablFormatterModels = new List<MorestachioFormatterModel>();
			//AddGlobalFormatter();
			EmptyFormatter = new FormatterMatcher();
		}

		private FormatterMatcher EmptyFormatter { get; }

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
					new Func<object, object[], object>((sourceObject, argument) =>
						FormatConditonal(sourceObject, argument, formatterGroup)));
			}
		}

		private object FormatConditonal(object sourceObject, object argument,
			IEnumerable<MorestachioFormatterModel> formatterGroup)
		{
			var directMatch = formatterGroup.Where(e => (argument?.ToString().StartsWith(e.Name)).GetValueOrDefault());
			var orginalObject = sourceObject;

			foreach (var morestachioFormatterModel in directMatch)
			{
				var clearedArgument = argument?.ToString().Remove(0, (morestachioFormatterModel.Name).Length).Trim();
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

					sourceObject = morestachioFormatterModel.Function.Invoke(null, new[] { sourceObject, clearedArgument });
					if (sourceObject == null || !sourceObject.Equals(orginalObject))
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
					sourceObject = makeGenericMethod.Invoke(null, new[] { sourceObject, clearedArgument });
					if (sourceObject == null || !sourceObject.Equals(orginalObject))
					{
						return sourceObject;
					}
				}
				catch (Exception)
				{
					continue;
				}
			}

			return sourceObject == null ? orginalObject : FormatterFlow.Skip;
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

					MorestachioFormatterModel MorestachioFormatterModel;
					MorestachioFormatterModel = new MorestachioFormatterModel(morestachioFormatterAttribute.Name, morestachioFormatterAttribute.Description,
						method.GetParameters().FirstOrDefault().ParameterType,
						morestachioFormatterAttribute.OutputType ?? method.ReturnType,
						method.GetCustomAttributes<MorestachioFormatterInputAttribute>().Select(e => new InputDescription(e.Description, e.OutputType, e.Example)).ToArray(),
						morestachioFormatterAttribute.ReturnHint, method);
					GloablFormatterModels.Add(MorestachioFormatterModel);
				}

			}
		}

		//public void AddGlobalFormatter()
		//{
		//	AddFromType(typeof(GlobalFormatter));
		//	AddFromType(typeof(ListFormatter));
		//	AddFromType(typeof(StructualFormatter));
		//}

		/// <summary>
		/// Gets the gloabl formatter that are used always for any formatting run.
		/// </summary>
		public ICollection<MorestachioFormatterModel> GloablFormatterModels { get; private set; }

		//public ICollection<MorestachioFormatterModel> GetFormatterForUser(int userId)
		//{
		//	return GloablFormatterModels;
		//}
	}
}
