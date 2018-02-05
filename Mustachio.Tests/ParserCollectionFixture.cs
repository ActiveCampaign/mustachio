using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mustachio.Tests
{
	public class EnumerableFormatter
	{
		static EnumerableFormatter()
		{
			Formatter = new Dictionary<string, Func<IEnumerable<object>, string, object>>();

			Formatter.Add("order by desc ", (collection, arg) => collection.OrderByDescending(PropExpression(arg)));
			Formatter.Add("order by ", (collection, arg) => collection.OrderBy(PropExpression(arg)));

			Formatter.Add("order desc", (collection, arg) => collection.OrderByDescending(e => e));
			Formatter.Add("order", (collection, arg) => collection.OrderBy(e => e));

			Formatter.Add("contains ", (collection, arg) => collection.Any(e => e.Equals(arg)));
			Formatter.Add("count", (collection, arg) => collection.Count());
			Formatter.Add("element at ", (collection, arg) => collection.ElementAt(int.Parse(arg)));
			Formatter.Add("distinct", (collection, arg) => collection.Distinct());
			Formatter.Add("first or default", (collection, arg) => collection.FirstOrDefault());
			Formatter.Add("group by ", (collection, arg) => collection.GroupBy(PropExpression(arg)));
			Formatter.Add("max ", (collection, arg) => collection.Max(PropExpression(arg)));
			Formatter.Add("max", (collection, arg) => collection.Max());
			Formatter.Add("min ", (collection, arg) => collection.Min(PropExpression(arg)));
			Formatter.Add("min", (collection, arg) => collection.Min());

			Formatter.Add("reverse", (collection, arg) => collection.Reverse());
			Formatter.Add("select ", (collection, arg) => collection.Select(PropExpression(arg)));
			Formatter.Add("take ", (collection, arg) => collection.Take(int.Parse(arg)));
		}

		public static Func<object, object> PropExpression(string propName)
		{
			var parameterExpression = Expression.Parameter(typeof(object));
			var propCall = Expression.Property(parameterExpression, propName);
			return Expression.Lambda<Func<object, object>>(propCall, parameterExpression).Compile();
		}

		public static IDictionary<string, Func<IEnumerable<object>, string, object>> Formatter { get; set; }

		public object FormatArgument(IEnumerable sourceCollection, string arguments)
		{
			var formatter = Formatter.FirstOrDefault(e => arguments.StartsWith(e.Key));

			if (formatter.Value != null)
			{
				return formatter.Value(sourceCollection.Cast<object>(), arguments.Remove(0, formatter.Key.Length));
			}
			return sourceCollection;
		}
	}

	public class ParserCollectionFixture
	{
		private void AddCollectionTypeFormatter(ParserOptions options)
		{
			options.AddFormatter<IEnumerable>((value, arg) =>
			{
				return arg.Split('|').Aggregate(value, (current, format) => (IEnumerable)new EnumerableFormatter().FormatArgument(current, format.Trim()));
			});
		}

		[Fact]
		public void TestCollectionFormatting()
		{
			var options = new ParserOptions("{{#each data(order)}}{{.}},{{/each}}", null, ParserFixture.DefaultEncoding);
			var collection = new int[] { 0, 1, 2, 3, 5, 4, 6, 7 };
			AddCollectionTypeFormatter(options);
			var report = Parser.ParseWithOptions(options).ParsedTemplate(new Dictionary<string, object>()
			{
				{
					"data", collection
				}
			}).Stringify(true, ParserFixture.DefaultEncoding);
			Assert.Equal(report, collection.OrderBy(e => e).Select(e => e.ToString()).Aggregate((e, f) => e + "," + f) + ",");
			Console.WriteLine(report);
		}

		[Fact]
		public void TestCollectionFormattingScope()
		{
			var options = new ParserOptions("{{#each data(order)}}{{.}},{{/each}}|{{#each data}}{{.}},{{/each}}", null, ParserFixture.DefaultEncoding);
			var collection = new int[] { 0, 1, 2, 3, 5, 4, 6, 7 };
			AddCollectionTypeFormatter(options);
			var report = Parser.ParseWithOptions(options).ParsedTemplate(new Dictionary<string, object>()
			{
				{
					"data", collection
				}
			}).Stringify(true, ParserFixture.DefaultEncoding);

			var resultLeftExpressionOrdered =
					collection.OrderBy(e => e).Select(e => e.ToString()).Aggregate((e, f) => e + "," + f) + ",";
			var resultRightExpression = collection.Select(e => e.ToString()).Aggregate((e, f) => e + "," + f) + ",";

			Assert.Equal(report, resultLeftExpressionOrdered + "|" + resultRightExpression);
			Console.WriteLine(report);
		}
	}
}
