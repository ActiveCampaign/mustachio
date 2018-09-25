using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morestachio.Formatter
{
	internal class MultiFormatterInfo
	{
		public Type Type { get; set; }
		public string Name { get; set; }
		public bool IsOptional { get; set; }
		public bool IsSourceObject { get; set; }
		public int Index { get; set; }
	}

	internal class FormatterPart
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
