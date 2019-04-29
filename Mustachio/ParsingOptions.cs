namespace Mustachio
{
    public class ParsingOptions
    {
        /// <summary>
        /// If this is true, all values will be rendered without being HTML-encoded. (regardless of using {{{ }}} or {{ }} syntax)
        /// In some cases, content should not be escaped (such as when rendering text bodies and subjects in emails). 
        /// By default, we use content escaping, but this parameter allows it to be disabled.
        /// </summary>
        public bool DisableContentSafety { get; set; } = false;

        /// <summary>
        /// The source name for this template. Will be used in error reporting to identify the location of parsing errors.
        /// </summary>
        public string SourceName { get; set; } = "Base";

        /// <summary>
        /// Allows to extend Mustachio to support unknown tokens. You can use this to include partials,
        /// or any custom behaviour such as date/time formatters, localization, etc.
        /// </summary>
        public TokenExpander[] TokenExpanders { get; set; }
    }
}
