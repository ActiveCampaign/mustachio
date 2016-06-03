namespace Mustachio
{
    internal class ParsingOptions
    {
        /// <summary>
        /// If this is true, all values will be rendered without being HTML-encoded. (regardless of using {{{ }}} or {{ }} syntax)
        /// </summary>
        internal bool DisableContentSafety { get; set; }
    }
}
