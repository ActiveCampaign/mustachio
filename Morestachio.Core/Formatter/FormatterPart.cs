using System.Diagnostics;

namespace Morestachio.Formatter
{
	[DebuggerDisplay("[{Name ?? 'Unnamed'}] {Argument}")]
	public class FormatterPart
	{
		public FormatterPart(string name, string argument)
		{
			Name = name;
			Argument = argument;
		}

		public string Name { get; set; }
		public string Argument { get; set; }
	}
}