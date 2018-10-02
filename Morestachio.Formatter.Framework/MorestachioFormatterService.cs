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
		[RestParameter]params object[] arguments);

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
			foreach (var formatterGroup in listOfFormatter.GroupBy(e => e.InputType.Name).ToArray())
			{
				//if the object has a generic argument like lists, make it late bound
				FormatTemplateElement formatter;

				var type = formatterGroup.First().InputType;

				if (type.GetGenericArguments().Any())
				{
					formatter = options.Formatters.AddFormatter(type.GetGenericTypeDefinition(),
							new MorstachioFormatter((sourceObject, name, arguments) =>
								FormatConditonal(sourceObject, name, arguments, formatterGroup, options)));
				}
				else
				{
					formatter = options.Formatters.AddFormatter(type,
						new MorstachioFormatter((sourceObject, name, arguments) =>
							FormatConditonal(sourceObject, name, arguments, formatterGroup, options)));
				}


				formatter.MetaData
					.LastIsParams()
					.SetName("name", "Name");
			}
		}

		/// <summary>
		///		Add all formatter into the given options object
		/// </summary>
		/// <param name="options">The options.</param>
		[PublicAPI]
		public void AddFormatterToMorestachio(ParserOptions options)
		{
			AddFormatterToMorestachio(GlobalFormatterModels, options);
		}

		private object FormatConditonal(object sourceObject, string name, object[] arguments,
			IEnumerable<MorestachioFormatterModel> formatterGroup, ParserOptions options)
		{
			if (name == null)
			{
				options.Formatters.Write(() => nameof(MorestachioFormatterService) + " | Name is null. Skip formatter");
				return FormatterMatcher.FormatterFlow.Skip;
			}

			if (sourceObject == null)
			{
				options.Formatters.Write(() => nameof(MorestachioFormatterService) + " | Source Object is null. Skip formatter");
				return FormatterMatcher.FormatterFlow.Skip;
			}

			var directMatch = formatterGroup.Where(e => (name.ToString().Equals(e.Name))).ToArray();

			var type = sourceObject.GetType();
			var originalObject = sourceObject;
			if (!directMatch.Any())
			{
				options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)} | No match Found for name: '{name}' Possible values for '{type}' are [{formatterGroup.Select(e => e.Name).Aggregate((e, f) => e + "," + f)}]");
				return FormatterMatcher.FormatterFlow.Skip;
			}

			foreach (var morestachioFormatterModel in directMatch)
			{
				originalObject = sourceObject;
				options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)} | Test {morestachioFormatterModel.Name}");

				var target = morestachioFormatterModel.Function;
				var parameterInfos = target.GetParameters().Skip(1).ToArray();

				if (parameterInfos.Count(e => !e.IsOptional) != arguments.Length)
				{
					options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)} | Invalid count of parameter");
					continue;
				}

				var abort = false;

				for (var index = 0; index < parameterInfos.Length; index++)
				{
					var parameterInfo = parameterInfos[index];
					var paramVal = arguments[index];
					if (!parameterInfo.ParameterType.IsInstanceOfType(paramVal))
					{
						options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)} | Parameter at index {index} named {parameterInfo.Name} is not assignable from '{paramVal}'");
						abort = true;
						continue;
					}
				}

				if (abort)
				{
					continue;
				}

				var localGen = morestachioFormatterModel.InputType.GetGenericArguments();
				var templateGen = type.GetGenericArguments();

				if (morestachioFormatterModel.InputType.ContainsGenericParameters)
				{
					if (localGen.Any() != templateGen.Any())
					{
						if (type.IsArray)
						{
							templateGen = new[] { type.GetElementType() };
						}
						else
						{
							options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)}| Generic type mismatch");
							continue;
						}
					}

					if (!morestachioFormatterModel.InputType.ContainsGenericParameters)
					{
						options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)}| Type has Generic but Method not");
						continue;
					}

					if (localGen.Length != templateGen.LongLength)
					{
						options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)}| Generic type count mismatch");
						continue;
					}

					target = target.MakeGenericMethod(templateGen);
				}
				else
				{
					if (!morestachioFormatterModel.InputType.IsInstanceOfType(originalObject))
					{
						options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)}| Generic Type mismatch. Expected '{morestachioFormatterModel.InputType}' but got {originalObject.GetType()}");
						continue;
					}
				}
				//originalObject = morestachioFormatterModel.Function.Invoke(null, new[] { originalObject }.Concat(arguments).ToArray());
				//options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)}| Executed. Return '{originalObject}'");
				//return originalObject;
				try
				{

					options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)}| Execute");

					originalObject = target
						.Invoke(null, new[] { originalObject }.Concat(arguments).Take(parameterInfos.Length + 1)
							.ToArray());

					options.Formatters.Write(() => $"{nameof(MorestachioFormatterService)}| Formatter created '{originalObject}'");
					return originalObject;
				}
				catch (Exception)
				{
					continue;
				}
			}

			return FormatterMatcher.FormatterFlow.Skip;
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
					if (morestachioFormatterAttribute == null)
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
