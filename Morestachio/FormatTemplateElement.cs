using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Morestachio.Formatter;

namespace Morestachio
{
	/// <summary>
	///     Delegate for the Can Execute method on a FormatTemplateElement
	/// </summary>
	/// <param name="sourceObject">The source object.</param>
	/// <param name="parameter">
	///     The parameters from template matched to the formatters
	///     <seealso cref="FormatTemplateElement.Format" />.
	/// </param>
	/// <returns></returns>
	public delegate bool CanExecute([CanBeNull] object sourceObject, [NotNull] KeyValuePair<string, object>[] parameter);

	/// <summary>
	///     Encapsulates a Format function
	/// </summary>
	public class FormatTemplateElement : IFreezable
	{
		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="formatTemplate"></param>
		/// <param name="inputTypes"></param>
		/// <param name="outputType"></param>
		/// <param name="argumentMeta"></param>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Delegate formatTemplate,
			[NotNull] Type inputTypes,
			[CanBeNull] Type outputType,
			[NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this(formatTemplate, null, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Func<object, object, object> formatTemplate, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Func<object, object[], object> formatTemplate, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Action<object, object[]> formatTemplate, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Action<object, object> formatTemplate, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="formatTemplate">The Delegate that will be invoked when the formatters type is matched by the <see cref="FormatterMatcher"/></param>
		/// <param name="canFormat">An optional Delegate of type <see cref="CanExecute"/> that will be invoked when the match was successfull, all arguments of the FormatTemplate are provided</param>
		/// <param name="inputTypes">The type that this formatter is attached to</param>
		/// <param name="outputType">The type of object this formatter returns</param>
		/// <param name="argumentMeta">Meta informations of the Formatters Delegate</param>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Delegate formatTemplate,
			[CanBeNull] CanExecute canFormat,
			[NotNull] Type inputTypes,
			[CanBeNull] Type outputType,
			[NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
		{
			Format = formatTemplate ?? throw new ArgumentNullException(nameof(formatTemplate));
			CanFormat = canFormat;
			InputTypes = inputTypes ?? throw new ArgumentNullException(nameof(inputTypes));
			OutputType = outputType ?? throw new ArgumentNullException(nameof(outputType));
			MetaData = new MultiFormatterInfoCollection(argumentMeta ?? throw new ArgumentNullException(nameof(argumentMeta)));
			if (MetaData.Any(e => e == null))
			{
				throw new InvalidOperationException("You cannot use a Null value in the collection of MultiFormatterInfo");
			}
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Func<object, object, object> formatTemplate,
			[CanBeNull] CanExecute canFormat, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, canFormat, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Func<object, object[], object> formatTemplate,
			[CanBeNull] CanExecute canFormat, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, canFormat, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Action<object, object[]> formatTemplate,
			[CanBeNull] CanExecute canFormat, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, canFormat, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		///     Ctor
		/// </summary>
		[PublicAPI]
		public FormatTemplateElement([NotNull] Action<object, object> formatTemplate,
			[CanBeNull] CanExecute canFormat, [NotNull] Type inputTypes,
			[CanBeNull] Type outputType, [NotNull] [ItemNotNull] MultiFormatterInfo[] argumentMeta)
			: this((Delegate)formatTemplate, canFormat, inputTypes, outputType, argumentMeta)
		{
		}

		/// <summary>
		/// Sets the can format delegate.
		/// </summary>
		/// <param name="canFormat">The can format.</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">This object is Frozen</exception>
		[NotNull]
		public FormatTemplateElement SetCanFormat([CanBeNull]CanExecute canFormat)
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException("This object is Frozen");
			}

			CanFormat = canFormat;
			return this;
		}

		/// <summary>
		///     Delegate for custom check if this formatter can handle the values
		/// </summary>
		[CanBeNull]
		public CanExecute CanFormat { get; private set; }

		/// <summary>
		///     delegate for formatting template pars
		/// </summary>
		[NotNull]
		public Delegate Format { get; private set; }

		/// <summary>
		///     Gets the Meta data for the Arguments
		/// </summary>
		[NotNull]
		[ItemNotNull]
		public MultiFormatterInfoCollection MetaData { get; }

		/// <summary>
		///     The type of input the Formatter is able to accept. Can be null.
		/// </summary>
		[NotNull]
		public Type InputTypes { get; }

		/// <summary>
		///     The type that the formatter will return. Can be null.
		/// </summary>
		[CanBeNull]
		public Type OutputType { get; }
		
		/// <inheritdoc />
		public bool IsFrozen { get; private set; }

		/// <inheritdoc />
		public void Freeze()
		{
			IsFrozen = true;
		}
	}

	/// <summary>
	///		Defines an object that can be Freeze. If an object is frozen it cannot be changed anymore.
	/// </summary>
	public interface IFreezable
	{
		/// <summary>
		/// Gets a value indicating whether this instance is frozen.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is frozen; otherwise, <c>false</c>.
		/// </value>
		bool IsFrozen { get; }
		/// <summary>
		/// Freezes this instance.
		/// </summary>
		void Freeze();
	}

	/// <inheritdoc />
	public class MultiFormatterInfoCollection : IReadOnlyList<MultiFormatterInfo>
	{
		private readonly IReadOnlyList<MultiFormatterInfo> _source;
		
		/// <inheritdoc />
		public MultiFormatterInfoCollection(IEnumerable<MultiFormatterInfo> source)
		{
			_source = source.ToArray();
		}

		/// <inheritdoc />
		public IEnumerator<MultiFormatterInfo> GetEnumerator()
		{
			return _source.GetEnumerator();
		}
		
		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _source.GetEnumerator();
		}
		
		/// <inheritdoc />
		public int Count => _source.Count;
		
		/// <inheritdoc />
		public MultiFormatterInfo this[int index] => _source[index];

		/// <summary>
		///		Sets the name of an Parameter.
		/// </summary>
		/// <returns></returns>
		public MultiFormatterInfoCollection SetName(string parameterName, string templateParameterName)
		{
			var multiFormatterInfo = this.FirstOrDefault(e => e.Name.Equals(parameterName));
			if (multiFormatterInfo == null)
			{
				return this;
			}

			multiFormatterInfo.Name = templateParameterName;
			return this;
		}

		/// <summary>
		///		When called and the last parameter is an object array, it will be used as an params parameter.
		///		This is quite helpfull as you cannot annotate Lambdas.
		/// </summary>
		/// <returns></returns>
		public MultiFormatterInfoCollection LastIsParams()
		{
			var multiFormatterInfo = this.LastOrDefault();
			if (multiFormatterInfo == null)
			{
				return this;
			}

			if (multiFormatterInfo.Type == typeof(object[]))
			{
				multiFormatterInfo.IsRestObject = true;
			}

			return this;
		}
	}
}