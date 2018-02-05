using System.Collections;
using Mustachio;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace Mustachio.Tests
{
	public class TemplateFixture
	{
		[Theory]
		[InlineData(200)]
		[InlineData(80000)]
		[InlineData(700000)]
		public void TemplateMaxSizeLimit(int maxSize)
		{
			var tempdata = new List<string>();
			var sizeOfOneChar = ParserFixture.DefaultEncoding.GetByteCount(" ");
			for (int i = 0; i < (maxSize / sizeOfOneChar); i++)
			{
				tempdata.Add(" ");
			}

			var template = "{{#each Data}}{{.}}{{/each}}";
			var templateFunc = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding, maxSize));
			var templateStream = templateFunc.ParsedTemplate(new Dictionary<string, object>()
			{
				{"Data", tempdata}
			});
			Assert.Equal(templateStream.Length, maxSize);
		}

		[Theory]
		[InlineData(200)]
		[InlineData(80000)]
		[InlineData(700000)]
		public void TemplateMaxSizeOverLimit(int maxSize)
		{
			var tempdata = new List<string>();
			var sizeOfOneChar = ParserFixture.DefaultEncoding.GetByteCount(" ");
			for (int i = 0; i < (maxSize * sizeOfOneChar); i++)
			{
				tempdata.Add(" ");
			}

			var template = "{{#each Data}}{{.}}{{/each}}";
			var templateFunc = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding, maxSize));
			var templateStream = templateFunc.ParsedTemplate(new Dictionary<string, object>()
			{
				{"Data", tempdata}
			});
			Assert.True(templateStream.Length == maxSize);
		}

		[Fact]
		public void TemplateRendersContentWithNoVariables()
		{
			var plainText = "ASDF";
			var template = Parser.ParseWithOptions(new ParserOptions("ASDF", null, ParserFixture.DefaultEncoding));
			Assert.Equal(plainText, template.ParsedTemplate(null).Stringify(true, ParserFixture.DefaultEncoding));
		}

		[Fact]
		public void HtmlIsNotEscapedWhenUsingUnsafeSyntaxes()
		{
			var model = new Dictionary<string, object>();

			model["stuff"] = "<b>inner</b>";

			var plainText = @"{{{stuff}}}";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("<b>inner</b>", rendered);

			plainText = @"{{&stuff}}";
			rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);
			Assert.Equal("<b>inner</b>", rendered);
		}

		[Fact]
		public void HtmlIsEscapedByDefault()
		{
			var model = new Dictionary<string, object>();

			model["stuff"] = "<b>inner</b>";

			var plainText = @"{{stuff}}";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("&lt;b&gt;inner&lt;/b&gt;", rendered);
		}

		[Fact]
		public void CommentsAreExcludedFromOutput()
		{
			var model = new Dictionary<string, object>();

			var plainText = @"as{{!stu
            ff}}df";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("asdf", rendered);
		}

		[Fact]
		public void NegationGroupRendersContentWhenValueNotSet()
		{
			var model = new Dictionary<string, object>();

			var plainText = @"{{^stuff}}No Stuff Here.{{/stuff}}";
			var rendered = Parser.ParseWithOptions(new ParserOptions(plainText, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("No Stuff Here.", rendered);
		}


		[Fact]
		public void TemplateRendersWithComplextEachPath()
		{
			var template = @"{{#each Company.ceo.products}}<li>{{ name }} and {{version}} and has a CEO: {{../../last_name}}</li>{{/each}}";

			var parsedTemplate = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding));

			var model = new Dictionary<string, object>();

			var company = new Dictionary<string, object>();
			model["Company"] = company;

			var ceo = new Dictionary<string, object>();
			company["ceo"] = ceo;
			ceo["last_name"] = "Smith";

			var products = Enumerable.Range(0, 3).Select(k =>
			{
				var r = new Dictionary<String, object>();
				r["name"] = "name " + k;
				r["version"] = "version " + k;
				return r;
			}).ToArray();

			ceo["products"] = products;

			var result = parsedTemplate.ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("<li>name 0 and version 0 and has a CEO: Smith</li>" +
				"<li>name 1 and version 1 and has a CEO: Smith</li>" +
				"<li>name 2 and version 2 and has a CEO: Smith</li>", result);
		}

		[Fact]
		public void TemplateShouldProcessVariablesInInvertedGroup()
		{
			var model = new Dictionary<String, object>
			{
				{ "not_here" , false },
				{ "placeholder" , "a placeholder value" }
			};

			var template = "{{^not_here}}{{../placeholder}}{{/not_here}}";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("a placeholder value", result);
		}

		[InlineData(new int[] { })]
		[InlineData(false)]
		[InlineData("")]
		[InlineData(0.0)]
		[InlineData(0)]
		[Theory]
		public void TemplatesShoudlNotRenderFalseyComplexStructures(object falseyModelValue)
		{
			var model = new Dictionary<String, object>
			{
				{ "outer_level", falseyModelValue}
			};

			var template = "{{#outer_level}}Shouldn't be rendered!{{inner_level}}{{/outer_level}}";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal(String.Empty, result);
		}

		[InlineData(new int[] { })]
		[InlineData(false)]
		[InlineData("")]
		[InlineData(0.0)]
		[InlineData(0)]
		[Theory]
		public void TemplateShouldTreatFalseyValuesAsEmptyArray(object falseyModelValue)
		{
			var model = new Dictionary<String, object>
			{
				{ "locations", falseyModelValue}
			};

			var template = "{{#each locations}}Shouldn't be rendered!{{/each}}";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal(String.Empty, result);
		}

		[InlineData(0)]
		[InlineData(0.0)]
		[Theory]
		public void TemplateShouldRenderZeroValue(object value)
		{
			var model = new Dictionary<String, object>
			{
				{ "times_won", value}
			};

			var template = "You've won {{times_won}} times!";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("You've won 0 times!", result);
		}

		[Fact]
		public void TemplateShouldRenderFalseValue()
		{
			var model = new Dictionary<String, object>
			{
				{ "times_won", false}
			};

			var template = "You've won {{times_won}} times!";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("You've won False times!", result);
		}

		[Fact]
		public void TemplateShouldNotRenderNullValue()
		{
			var model = new Dictionary<String, object>
			{
				{ "times_won", null}
			};

			var template = "You've won {{times_won}} times!";

			var result = Parser.ParseWithOptions(new ParserOptions(template, null, ParserFixture.DefaultEncoding)).ParsedTemplate(model).Stringify(true, ParserFixture.DefaultEncoding);

			Assert.Equal("You've won  times!", result);
		}
	}
}
