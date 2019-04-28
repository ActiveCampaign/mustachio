namespace Mustachio
{
    public class ParsingOptions
    {
        /// <summary>
        /// If this is true, all values will be rendered without being HTML-encoded. (regardless of using {{{ }}} or {{ }} syntax)
        /// </summary>
        public bool DisableContentSafety { get; set; } = false;

        public string SourceName { get; set; } = "Base";

        public TokenExpander[] TokenExpanders { get; set; }
    }
}
