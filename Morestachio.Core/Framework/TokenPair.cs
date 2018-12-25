using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Morestachio.Formatter;

namespace Morestachio.Framework
{
	/// <summary>
	///     The token that has been lexed out of template content.
	/// </summary>
	[DebuggerTypeProxy(typeof(TokenPairDebuggerProxy))]
	internal class TokenPair
	{
		[PublicAPI]
		private class TokenPairDebuggerProxy
		{
			private readonly TokenPair _pair;

			public TokenPairDebuggerProxy(TokenPair pair)
			{
				_pair = pair;
			}

			public TokenType Type
			{
				get { return _pair.Type; }
			}

			public FormatterPart[] FormatString
			{
				get { return _pair.FormatString; }
			}

			public string Value
			{
				get { return _pair.Value; }
			}

			public override string ToString()
			{
				if (FormatString != null && FormatString.Any())
				{
					return $"{Type} \"{Value}\" AS ({FormatString.Select(e => e.ToString()).Aggregate((e, f) => e + "," + f)})";
				}
				return $"{Type} {Value}";
			}
		}

		public TokenPair(TokenType type, string value)
		{
			Type = type;
			Value = value;
		}

		public TokenType Type { get; set; }

		[CanBeNull]
		public FormatterPart[] FormatString { get; set; }
		
		[CanBeNull]
		public string Value { get; set; }
	}
}