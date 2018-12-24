using System;
using System.Linq;
using JetBrains.Annotations;
using Morestachio.Formatter;

namespace Morestachio.Framework
{
	/// <summary>
	///     The token that has been lexed out of template content.
	/// </summary>
	[Serializable]
	internal class TokenPair
	{
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

		public override string ToString()
		{
			if (FormatString != null && FormatString.Any())
			{
				return $"{Type} \"{Value}\" AS ({FormatString.Select(e => e.ToString()).Aggregate((e, f) => e + "," + f)})";
			}
			return $"{Type} {Value}";
		}
	}
}