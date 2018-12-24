namespace Morestachio.Formatter
{
	internal class FormatterPart
	{
		public FormatterPart(string name, string argument)
		{
			Name = name;
			Argument = argument;
		}

		public string Name { get; set; }
		public string Argument { get; set; }

		public override string ToString()
		{
			return (string.IsNullOrWhiteSpace(Name) ? "" : $"[{Name}]") + Argument.ToString();
		}
	}

	internal class FormatterCollectionPart : FormatterPart
	{
		public FormatterCollectionPart(string name, string argument, FormatterPart[] collectionItems) : base(name, argument)
		{
			CollectionItems = collectionItems;
		}

		public FormatterPart[] CollectionItems { get; private set; }
		
		public override string ToString()
		{
			return (string.IsNullOrWhiteSpace(Name) ? "" : $"[{Name}]") + $"[{Argument}]";
		}
	}
}