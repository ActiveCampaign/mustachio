using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mustachio.Tests
{
	public class ParserCollectionFixture
	{
		private void AddCollectionTypeFormatter<T>(ParserOptions options)
		{
			options.AddFormatter<IEnumerable<T>>((value, arg) =>
			{
				if (arg.StartsWith("order"))
				{
					if (arg.Equals("order asc"))
					{
						return value.OrderBy(e => e);
					}
					if (arg.Equals("order desc"))
					{
						return value.OrderBy(e => e);
					}
					var propName = arg.Replace("order by ", "");
					var lamdbaX = "(item) => item." + propName;

					var parameterExpression = Expression.Parameter(typeof(T));
					var propCall = Expression.Property(parameterExpression, propName);
					var expression = Expression.Lambda<Func<T, object>>(propCall, parameterExpression).Compile();

					return value.OrderBy(expression);
				}
				return value;
			});
		}

		[Fact]
		public void TestCollectionFormatting()
		{
			var options = new ParserOptions("{{#each data(order asc)}}{{.}},{{/each}}", null, ParserFixture.DefaultEncoding);
			var collection = new int[] {0, 1, 2, 3, 5, 4, 6, 7};
			AddCollectionTypeFormatter<int>(options);
			var report = Parser.ParseWithOptions(options).ParsedTemplate(new Dictionary<string, object>()
			{
				{
					"data", collection
				}
			}).Stringify(true, ParserFixture.DefaultEncoding);
			Assert.Equal(report, collection.OrderBy(e => e).Select(e => e.ToString()).Aggregate((e,f) => e + "," + f) + ",");
			Console.WriteLine(report);
		}
	}
}
