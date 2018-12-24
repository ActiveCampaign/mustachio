using System;
using JetBrains.Annotations;

namespace Morestachio.Formatter
{
	/// <summary>
	///		Common used extensions to the Formatter Matcher
	/// </summary>
	[PublicAPI]
	public static class FormatterMatcherExtensions
	{
		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="matcher">The instance of FormatterMatcher</param>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public static FormatTemplateElement AddFormatter<T>(this IFormatterMatcher matcher, [NotNull] Func<T> formatterDelegate)
		{
			return matcher.AddFormatter(typeof(T), formatterDelegate);
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="matcher">The instance of FormatterMatcher</param>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public static FormatTemplateElement AddFormatter<T, TResult>(this IFormatterMatcher matcher, [NotNull] Func<T, TResult> formatterDelegate)
		{
			return matcher.AddFormatter(typeof(T), formatterDelegate);
		}

		/// <summary>
		///     Adds the formatter.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <typeparam name="TArgument"></typeparam>
		/// <param name="matcher">The instance of FormatterMatcher</param>
		/// <param name="formatterDelegate">The formatter delegate.</param>
		public static FormatTemplateElement AddFormatter<T, TArgument, TResult>(
			this IFormatterMatcher matcher, [NotNull] Func<T, TArgument, TResult> formatterDelegate)
		{
			return  matcher.AddFormatter(typeof(T), formatterDelegate);
		}
	}
}