using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using JetBrains.Annotations;
using Morestachio.Formatter.Framework;

namespace Morestachio.Formatter.Linq
{
	[PublicAPI]
	public static class ListFormatter
	{
		[MorestachioFormatter("first or default", "Selects the First item in the list")]
		public static T FirstOrDefault<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.FirstOrDefault();
		}

		[MorestachioFormatter("order desc", "Orders the list descending")]
		public static IEnumerable<T> OrderDesc<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.OrderByDescending(e => e);
		}

		[MorestachioFormatter("order asc", "Orders the list ascending")]
		public static IEnumerable<T> Order<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.OrderBy(e => e);
		}

		[MorestachioFormatter("reverse", "Reverses the order of the list")]
		public static IEnumerable<T> Reverse<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Reverse();
		}

		[MorestachioFormatter("max", "Called on a list of numbers it returns the biggest")]
		public static T Max<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Max();
		}

		[MorestachioFormatter("min", "Called on a list of numbers it returns the smallest")]
		public static T Min<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Min();
		}

		[MorestachioFormatter("contains", "Searches in the list for that the argument")]
		[MorestachioFormatterInput("Must be ether a fixed value or an reference $other$")]
		public static bool Contains<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Any(e => e.Equals(arguments));
		}

		[MorestachioFormatter("element at", "Gets the item in the list on the postion")]
		[MorestachioFormatterInput("Must be a number")]
		public static T ElementAt<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.ElementAtOrDefault(int.Parse(arguments));
		}

		[MorestachioFormatter("order by asc", "Orders the list by the argument")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static IEnumerable<T> OrderBy<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.OrderBy(arguments);
		}

		[MorestachioFormatter("order by desc", "Orders the list by the argument")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static IEnumerable<T> OrderByDecending<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.OrderBy(arguments).Reverse();
		}

		[MorestachioFormatter("order group by", "Orders the list by the argument")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static IEnumerable<IGrouping<TKey, T>> GroupOrderBy<T, TKey>(IEnumerable<IGrouping<TKey, T>> sourceCollection, string arguments)
		{
			return sourceCollection.OrderBy(arguments);
		}

		[MorestachioFormatter("count", "Gets the count of the list")]
		public static decimal Count<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Count();
		}

		[MorestachioFormatter("distinct", "Gets a new list that contains not duplicates")]
		public static IEnumerable<T> Distinct<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Distinct();
		}

		[MorestachioFormatter("group by", "Groups the list be the argument.",
			ReturnHint = "List with Key. Can be listed with #each")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static IEnumerable GroupBy<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.GroupBy(arguments, "it");
		}

		[MorestachioFormatter("flat group", "Flattens the Group returned by group by",
			ReturnHint = "Can be listed with #each")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static IEnumerable<T> GroupByList<TKey, T>(IGrouping<TKey, T> sourceCollection, string arguments)
		{
			return sourceCollection.ToList();
		}

		[MorestachioFormatter("select", "Selects a Property from each item in the list and creates a new list", ReturnHint = "List contains the property. Can be listed with #each")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static IEnumerable Select<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Select(arguments);
		}

		[MorestachioFormatter("where", "Filters the list", ReturnHint = "List contains the property. Can be listed with #each")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static IEnumerable<T> Where<T>(IEnumerable<T> sourceCollection, string arguments)
		{
			return sourceCollection.Where(arguments);
		}

		[MorestachioFormatter("any", "Returns ether true or false if the expression in the argument is furfilled by any item")]
		[MorestachioFormatterInput("Must be Expression to property")]
		public static bool Any(IEnumerable sourceCollection, string arguments)
		{
			return sourceCollection.Any();
		}

		[MorestachioFormatter("take", "Takes the ammount of items in argument")]
		[MorestachioFormatterInput("number")]
		public static object Take(IEnumerable sourceCollection, string arguments)
		{
			return sourceCollection.Take(int.Parse(arguments));
		}

		[MorestachioFormatter("aggregate", "Aggreates the elements and returns it")]
		public static object Aggregate(IEnumerable sourceCollection, string arguments)
		{
			var colQuery = sourceCollection.AsQueryable();

			if (typeof(int).IsAssignableFrom(colQuery.ElementType))
			{
				return colQuery.Cast<int>().Sum();
			}
			if (typeof(decimal).IsAssignableFrom(colQuery.ElementType))
			{
				return colQuery.Cast<decimal>().Sum();
			}

			return sourceCollection;
		}

		[MorestachioFormatter("sum", "Aggreates the property in the argument and returns it")]
		public static decimal Sum(IEnumerable sourceCollection, string arguments)
		{
			var colQuery = sourceCollection.Cast<decimal>();
			return colQuery.Sum();
		}

		[MorestachioFormatter("sum", "Aggreates the property in the argument and returns it")]
		public static int Sum(IEnumerable<int> sourceCollection, string arguments)
		{
			return sourceCollection.Sum();
		}

		[MorestachioFormatter("sum", "Aggreates the property in the argument and returns it")]
		public static decimal Sum(IEnumerable<decimal> sourceCollection, string arguments)
		{
			return sourceCollection.Sum();
		}

		[MorestachioFormatter("sum", "Aggreates the property in the argument and returns it")]
		public static double Sum(IEnumerable<double> sourceCollection, string arguments)
		{
			return sourceCollection.Sum();
		}
	}
}
