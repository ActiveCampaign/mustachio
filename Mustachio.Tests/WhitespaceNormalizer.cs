using System.Text.RegularExpressions;

namespace Mustachio.Tests
{
	/// <summary>
	/// Allows for simpler comparison of template results that don't demand
	/// </summary>
	internal static class WhitespaceNormalizer
	{
		private static Regex WHITESPACE_NORMALIZER = new Regex("[\\s]+", RegexOptions.Compiled);
		/// <summary>
		/// Provides a mechanism to make comparing expected and actual results a little more sane to author.
		/// You may include whitespace in resources to make them easier to read.
		/// </summary>
		/// <param name="subject"></param>
		/// <returns></returns>
		internal static string EliminateWhitespace(this string subject)
		{
			return WHITESPACE_NORMALIZER.Replace(subject, "");
		}
	}
}