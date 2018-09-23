using System.Collections.Generic;
using System.Linq;
using Morestachio.Helper;
using NUnit.Framework;

namespace Morestachio.Tests
{
	public class TemplateFixture
	{
		[Test]
		[TestCase(200)]
		[TestCase(80000)]
		[TestCase(700000)]
		public void TemplateMaxSizeLimit(int maxSize)
		{
			var tempdata = new List<string>();
			var sizeOfOneChar = ParserFixture.DefaultEncoding.GetByteCount(" ");
			for (var i = 0; i < maxSize / sizeOfOneChar; i++)
			{
				tempdata.Add(" ");
			}

			var template = "{{#each Data}}{{.}}{{/each}}";
			var templateFunc =
				Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding, maxSize));
			var templateStream = templateFunc.Create(new Dictionary<string, object>
			{
				{"Data", tempdata}
			});
			Assert.AreEqual(templateStream.Length, maxSize);
		}

		[Test]
		[TestCase(200)]
		[TestCase(80000)]
		[TestCase(700000)]
		public void TemplateMaxSizeOverLimit(int maxSize)
		{
			var tempdata = new List<string>();
			var sizeOfOneChar = ParserFixture.DefaultEncoding.GetByteCount(" ");
			for (var i = 0; i < maxSize * sizeOfOneChar; i++)
			{
				tempdata.Add(" ");
			}

			var template = "{{#each Data}}{{.}}{{/each}}";
			var templateFunc =
				Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding, maxSize));
			var templateStream = templateFunc.Create(new Dictionary<string, object>
			{
				{"Data", tempdata}
			});
			Assert.True(templateStream.Length == maxSize);
		}

		[TestCase(new int[] { })]
		[TestCase(false)]
		[TestCase("")]
		[TestCase(0.0)]
		[TestCase(0)]
		[Test]
		public void TemplatesShoudlNotRenderFalseyComplexStructures(object falseyModelValue)
		{
			var model = new Dictionary<string, object>
			{
				{"outer_level", falseyModelValue}
			};

			var template = "{{#outer_level}}Shouldn't be rendered!{{inner_level}}{{/outer_level}}";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual(string.Empty, result);
		}

		[TestCase(new int[] { })]
		[TestCase(false)]
		[TestCase("")]
		[TestCase(0.0)]
		[TestCase(0)]
		[Test]
		public void TemplateShouldTreatFalseyValuesAsEmptyArray(object falseyModelValue)
		{
			var model = new Dictionary<string, object>
			{
				{"locations", falseyModelValue}
			};

			var template = "{{#each locations}}Shouldn't be rendered!{{/each}}";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual(string.Empty, result);
		}

		[TestCase(0)]
		[TestCase(0.0)]
		[Test]
		public void TemplateShouldRenderZeroValue(object value)
		{
			var model = new Dictionary<string, object>
			{
				{"times_won", value}
			};

			var template = "You've won {{times_won}} times!";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("You've won 0 times!", result);
		}

		[Test]
		public void CommentsAreExcludedFromOutput()
		{
			var model = new Dictionary<string, object>();

			var plainText = @"as{{!stu
            ff}}df";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("asdf", rendered);
		}

		[Test]
		public void HtmlIsEscapedByDefault()
		{
			var model = new Dictionary<string, object>();

			model["stuff"] = "<b>inner</b>";

			var plainText = @"{{stuff}}";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("&lt;b&gt;inner&lt;/b&gt;", rendered);
		}

		[Test]
		public void HtmlIsNotEscapedWhenUsingUnsafeSyntaxes()
		{
			var model = new Dictionary<string, object>();

			model["stuff"] = "<b>inner</b>";

			var plainText = @"{{{stuff}}}";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("<b>inner</b>", rendered);

			plainText = @"{{&stuff}}";
			rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);
			Assert.AreEqual("<b>inner</b>", rendered);
		}

		[Test]
		public void NegationGroupRendersContentWhenValueNotSet()
		{
			var model = new Dictionary<string, object>();

			var plainText = @"{{^stuff}}No Stuff Here.{{/stuff}}";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("No Stuff Here.", rendered);
		}

		[Test]
		public void TemplateRendersContentWithNoVariables()
		{
			var plainText = "ASDF";
			var template = Parser.ParseWithOptions(new ParserOptions("ASDF", null, ParserFixture.DefaultEncoding));
			Assert.AreEqual(plainText, template.Create(null).Stringify(true, ParserFixture.DefaultEncoding));
		}


		[Test]
		public void TemplateRendersWithComplextEachPath()
		{
			var template =
				@"{{#each Company.ceo.products}}<li>{{ name }} and {{version}} and has a CEO: {{../../last_name}}</li>{{/each}}";

			var parsedTemplate =
				Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding));

			var model = new Dictionary<string, object>();

			var company = new Dictionary<string, object>();
			model["Company"] = company;

			var ceo = new Dictionary<string, object>();
			company["ceo"] = ceo;
			ceo["last_name"] = "Smith";

			var products = Enumerable.Range(0, 3).Select(k =>
			{
				var r = new Dictionary<string, object>();
				r["name"] = "name " + k;
				r["version"] = "version " + k;
				return r;
			}).ToArray();

			ceo["products"] = products;

			var result = parsedTemplate.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("<li>name 0 and version 0 and has a CEO: Smith</li>" +
			             "<li>name 1 and version 1 and has a CEO: Smith</li>" +
			             "<li>name 2 and version 2 and has a CEO: Smith</li>", result);
		}

		[Test]
		public void TemplateShouldNotRenderNullValue()
		{
			var model = new Dictionary<string, object>
			{
				{"times_won", null}
			};

			var template = "You've won {{times_won}} times!";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("You've won  times!", result);
		}

		[Test]
		public void TemplateShouldProcessVariablesInInvertedGroup()
		{
			var model = new Dictionary<string, object>
			{
				{"not_here", false},
				{"placeholder", "a placeholder value"}
			};

			var template = "{{^not_here}}{{../placeholder}}{{/not_here}}";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("a placeholder value", result);
		}

		[Test]
		public void TemplateShouldRenderFalseValue()
		{
			var model = new Dictionary<string, object>
			{
				{"times_won", false}
			};

			var template = "You've won {{times_won}} times!";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding))
				.Create(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.AreEqual("You've won False times!", result);
		}
	}
}