using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Morestachio.Formatter.Linq;
using NUnit.Framework;

namespace Morestachio.Formatter.Framework.Tests
{
	public static class StringFormatter
	{
		[MorestachioFormatter("reverse", "XXX")]
		public static string Reverse(string originalObject)
		{
			return originalObject.Reverse().Select(e => e.ToString()).Aggregate((e, f) => e + f);
		}

		[MorestachioFormatter("reverse-arg", "XXX")]
		public static string ReverseWithArgSuccess(string originalObject, string argument)
		{
			return argument;
		}

		[MorestachioFormatter("fod", "XXX")]
		public static T GenericTest<T>(IEnumerable<T> originalObject)
		{
			return originalObject.FirstOrDefault();
		}
	}

	[TestFixture]
	public class FormatterTests
	{
		public static Encoding DefaultEncoding { get; set; } = new UnicodeEncoding(true, false, false);

		[Test]
		public void TestSingleNamed()
		{
			var formatterService = new MorestachioFormatterService();
			formatterService.AddFromType(typeof(StringFormatter));

			var options = new ParserOptions("{{data([Name]reverse)}}", null, DefaultEncoding);
			formatterService.AddFormatterToMorestachio(options);
			var template = Parser.ParseWithOptions(options);

			var andStringify = template.CreateAndStringify(new Dictionary<string, object>() { { "data", "Test" } });
			Assert.That(andStringify, Is.EqualTo("tseT"));
		}

		[Test]
		public void TestNamed()
		{
			var formatterService = new MorestachioFormatterService();
			formatterService.AddFromType(typeof(StringFormatter));

			var options = new ParserOptions("{{data([Name]reverse-arg, TEST)}}", null, DefaultEncoding);
			formatterService.AddFormatterToMorestachio(options);
			var template = Parser.ParseWithOptions(options);

			var andStringify = template.CreateAndStringify(new Dictionary<string, object>() { { "data", "Test" } });
			Assert.That(andStringify, Is.EqualTo("TEST"));
		}

		[Test]
		public void GenericsTest()
		{
			var formatterService = new MorestachioFormatterService();
			formatterService.AddFromType(typeof(StringFormatter));
			formatterService.AddFromType(typeof(ListFormatter));

			var options = new ParserOptions("{{data([Name]fod)}}", null, DefaultEncoding);
			formatterService.AddFormatterToMorestachio(options);
			var template = Parser.ParseWithOptions(options);

			var andStringify = template.CreateAndStringify(new Dictionary<string, object>() { { "data", new[] { "TEST", "test" } } });
			Assert.That(andStringify, Is.EqualTo("TEST"));
		}
	}
}
