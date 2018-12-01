using System.Collections.Generic;
using JetBrains.Annotations;

namespace Morestachio
{
	/// <summary>
	///     A context object for collections that is generated for each item inside a collection
	/// </summary>
	public class ContextCollection : ContextObject
	{
		/// <summary>
		///     ctor
		/// </summary>
		/// <param name="index">the current index of the item inside the collection</param>
		/// <param name="last">true if its the last item</param>
		/// <param name="options"></param>
		/// <param name="key"></param>
		public ContextCollection(long index, bool last, [NotNull] ParserOptions options, string key) : base(options, key)
		{
			Index = index;
			Last = last;
		}

		/// <summary>
		///     The current index inside the collection
		/// </summary>
		public long Index { get; }

		/// <summary>
		///     True if its the last item in the current collection
		/// </summary>
		public bool Last { get; }

		/// <inheritdoc />
		protected override ContextObject HandlePathContext(Queue<string> elements, string path)
		{
			var innerContext = new ContextObject(Options, path);
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