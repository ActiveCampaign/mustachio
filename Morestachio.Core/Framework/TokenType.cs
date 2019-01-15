namespace Morestachio.Framework
{
	/// <summary>
	///     The type of token produced in the lexing stage of template compilation.
	/// </summary>
	internal enum TokenType
	{
		EscapedSingleValue,
		UnescapedSingleValue,
		InvertedElementOpen,
		ElementOpen,
		ElementClose,
		Comment,
		Content,
		CollectionOpen,
		CollectionClose,

		/// <summary>
		///     Contains information about the formatting of the values. Must be followed by PrintFormatted or CollectionOpen
		/// </summary>
		Format,

		/// <summary>
		///     Is used to "print" the current formatted value to the output
		/// </summary>
		PrintFormatted,

		/// <summary>
		///		A Partial that is inserted into the one or multiple places in the Template
		/// </summary>
		PartialOpen,

		/// <summary>
		///		End of a Partial
		/// </summary>
		PartialClose,

		/// <summary>
		///		Defines the place for rendering a single partial
		/// </summary>
		RenderPartial
	}
}