using System;

namespace Morestachio.Framework
{
	/// <summary>
	///     The General Exception type for Framework Exceptions
	/// </summary>
	public class MustachioException : Exception
	{
		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="message"></param>
		/// <param name="replacements"></param>
		public MustachioException(string message, params object[] replacements) : base(string.Format(message,
			replacements))
		{
		}
	}
}