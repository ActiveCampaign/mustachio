using System;
using System.Linq;
using JetBrains.Annotations;
using Morestachio.Formatter;

namespace Morestachio
{
	/// <summary>
	///     Encapsulates a Format function
	/// </summary>
	public class FormatTemplateElement
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
		[PublicAPI]
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
		public Delegate Format { get; }

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
}