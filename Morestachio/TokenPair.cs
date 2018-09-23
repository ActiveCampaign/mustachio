using System;

namespace Morestachio
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

		public string FormatAs { get; set; }

		public string Value { get; set; }

		public override string ToString()
		{
			return string.Format("{0}, {1}", Type, Value);
		}
	}
}