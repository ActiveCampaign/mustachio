using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	}

	[TestFixture]
	public class FormatterTests
	{
		[Test]
		public void TestSingleNamed()
		{
			var formatterService = new MorestachioFormatterService();
			formatterService.AddFromType(typeof(StringFormatter));

			var options = new ParserOptions("{{data([Name]reverse)}}");
			formatterService.AddFormatterToMorestachio(options);
			var template = Parser.ParseWithOptions(options);

			var andStringify = template.CreateAndStringify(new Dictionary<string, object>() {{"data", "Test"}});
			Assert.That(andStringify, Is.EqualTo("tseT"));
		}
	}
}
