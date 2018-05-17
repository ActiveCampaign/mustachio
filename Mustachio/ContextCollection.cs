using System.Collections.Generic;

namespace Mustachio
{
	/// <summary>
	/// A context object for collections that is generated for each item inside a collection
	/// </summary>
	public class ContextCollection : ContextObject
	{
		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="index">the current index of the item inside the collection</param>
		/// <param name="last">true if its the last item</param>
		public ContextCollection(long index, bool last)
		{
			Index = index;
			Last = last;
		}

		/// <summary>
		/// The current index inside the collection
		/// </summary>
		public long Index { get; private set; }

		/// <summary>
		/// True if its the last item in the current collection
		/// </summary>
		public bool Last { get; private set; }

		/// <inheritdoc />
		protected override ContextObject HandlePathContext(Queue<string> elements, string path)
		{
			var innerContext = new ContextObject();
			innerContext.Options = Options;
			innerContext.Key = path;
			innerContext.Parent = this;

			object value = null;

			if (path.Equals("$first"))
			{
				value = Index == 0;
			}
			else if (path.Equals("$index"))
			{
				value = Index;
			}
			else if (path.Equals("$middel"))
			{
				value = Index != 0 && !Last;
			}
			else if (path.Equals("$last"))
			{
				value = Last;
			}
			else if (path.Equals("$odd"))
			{
				value = Index % 2 != 0;
			}
			else if (path.Equals("$even"))
			{
				value = Index % 2 == 0;
			}
			innerContext.Value = value;
			return value == null ? null : innerContext;
		}
	}
}