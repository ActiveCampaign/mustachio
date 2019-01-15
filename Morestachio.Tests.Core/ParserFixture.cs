using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Morestachio.Attributes;
using Morestachio.Helper;
using Morestachio.Formatter;
using Morestachio.Framework;
using Newtonsoft.Json;
using NUnit.Framework;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Morestachio.Tests
{
	public class ParserFixture
	{
		public static Encoding DefaultEncoding { get; set; } = new UnicodeEncoding(true, false, false);

		private class CollectionContextInfo
		{
			public int IndexProp { private get; set; }
			public bool FirstProp { private get; set; }
			public bool MiddelProp { private get; set; }
			public bool LastProp { private get; set; }

			public bool OddProp { private get; set; }
			public bool EvenProp { private get; set; }

			public override string ToString()
			{
				return $"{IndexProp},{FirstProp},{MiddelProp},{LastProp},{OddProp},{EvenProp}.";
			}
		}

		[Test]
		[TestCase("d")]
		[TestCase("D")]
		[TestCase("f")]
		[TestCase("F")]
		[TestCase("dd,,MM,,YYY")]
		public void ParserCanFormat(string dtFormat)
		{
			var data = DateTime.UtcNow;
			var results =
				Parser.ParseWithOptions(new ParserOptions("{{data(\"" + dtFormat + "\")}},{{data}}", null,
					DefaultEncoding));
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo(data.ToString(dtFormat) + "," + data));
		}

		[Test]
		[TestCase("d")]
		[TestCase("D")]
		[TestCase("f")]
		[TestCase("F")]
		[TestCase("dd,,MM,,YYY")]
		public void ParserCanSelfFormat(string dtFormat)
		{
			var data = DateTime.UtcNow;
			var results = Parser.ParseWithOptions(new ParserOptions("{{#data}}{{.(\"" + dtFormat + "\")}}{{/data}},{{data}}",
				null, DefaultEncoding));
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo(data.ToString(dtFormat) + "," + data));
		}

		[Test]
		[TestCase("{{data(d))}}")]
		[TestCase("{{data((d)}}")]
		[TestCase("{{data)}}")]
		[TestCase("{{data(}}")]
		public void ParserThrowsAnExceptionWhenFormatIsMismatched(string invalidTemplate)
		{
			Assert.Throws(typeof(AggregateException),
				() => Parser.ParseWithOptions(new ParserOptions(invalidTemplate)));
		}

		[Test]
		[TestCase("{{#ACollection}}{{.}}{{/each}}")]
		[TestCase("{{#ACollection}}{{.}}{{/ACollection}}{{/each}}")]
		[TestCase("{{/each}}")]
		public void ParserThrowsAnExceptionWhenEachIsMismatched(string invalidTemplate)
		{
			Assert.Throws(typeof(AggregateException),
				() => Parser.ParseWithOptions(new ParserOptions(invalidTemplate)));
		}

		[Test]
		[TestCase("{{Mike", "{{{{name}}")]
		[TestCase("{Mike", "{{{name}}")]
		[TestCase("Mike}", "{{name}}}")]
		[TestCase("Mike}}", "{{name}}}}")]
		public void ParserHandlesPartialOpenAndPartialClose(string expected, string template)
		{
			var model = new Dictionary<string, object>();
			model["name"] = "Mike";

			Assert.That(Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding)).CreateAndStringify(model), Is.EqualTo(expected));
		}


		[Test]
		[TestCase("{{#each element}}{{name}}")]
		[TestCase("{{#element}}{{name}}")]
		[TestCase("{{^element}}{{name}}")]
		public void ParserThrowsParserExceptionForUnclosedGroups(string invalidTemplate)
		{
			Assert.Throws(typeof(AggregateException),
				() => Parser.ParseWithOptions(new ParserOptions(invalidTemplate)));
		}


		[Test]
		[TestCase("{{..../asdf.content}}")]
		[TestCase("{{/}}")]
		[TestCase("{{./}}")]
		[TestCase("{{.. }}")]
		[TestCase("{{..}}")]
		[TestCase("{{...}}")]
		[TestCase("{{//}}")]
		[TestCase("{{@}}")]
		[TestCase("{{[}}")]
		[TestCase("{{]}}")]
		[TestCase("{{)}}")]
		[TestCase("{{(}}")]
		[TestCase("{{~}}")]
		[TestCase("{{%}}")]
		public void ParserShouldThrowForInvalidPaths(string template)
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions(template)));
		}

		[Test]
		[TestCase("{{first_name}}")]
		[TestCase("{{company.name}}")]
		[TestCase("{{company.address_line_1}}")]
		[TestCase("{{name}}")]
		public void ParserShouldNotThrowForValidPath(string template)
		{
			Parser.ParseWithOptions(new ParserOptions(template));
		}


		[Test]
		[TestCase("1{{first name}}", 1)]
		[TestCase("ss{{#each company.name}}\nasdf", 1)]
		[TestCase("xzyhj{{#company.address_line_1}}\nasdf{{dsadskl-sasa@}}\n{{/each}}", 3)]
		[TestCase("fff{{#each company.address_line_1}}\n{{dsadskl-sasa@}}\n{{/each}}", 1)]
		[TestCase("a{{name}}dd\ndd{{/each}}dd", 1)]
		public void ParserShouldThrowWithCharacterLocationInformation(string template, int expectedErrorCount)
		{
			var didThrow = false;
			try
			{
				Parser.ParseWithOptions(new ParserOptions(template));
			}
			catch (AggregateException ex)
			{
				didThrow = true;
				Assert.That(ex.InnerExceptions.Count, Is.EqualTo(expectedErrorCount));
			}

			Assert.True(didThrow);
		}

		[Test]
		[TestCase("<wbr>", "{{content}}", "&lt;wbr&gt;")]
		[TestCase("<wbr>", "{{{content}}}", "<wbr>")]
		public void ValueEscapingIsActivatedBasedOnValueInterpolationMustacheSyntax(string content, string template,
			string expected)
		{
			var model = new Dictionary<string, object>
			{
				{"content", content}
			};
			var value = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding))
				.CreateAndStringify(model);

			Assert.That(value, Is.EqualTo(expected));
		}

		[Test]
		[TestCase("<wbr>", "{{content}}", "<wbr>")]
		[TestCase("<wbr>", "{{{content}}}", "<wbr>")]
		public void ValueEscapingIsDisabledWhenRequested(string content, string template, string expected)
		{
			var model = new Dictionary<string, object>
			{
				{"content", content}
			};
			Assert.That(Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding, 0, true))
					.CreateAndStringify(model), Is.EqualTo(expected));
		}

		[Test]
		public void ParserCanChainFormat()
		{
			var data = DateTime.UtcNow;
			var parsingOptions = new ParserOptions("{{#data}}{{.(d).()}}{{/data}}", null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<string>(new Func<string, string>((s) => "TEST"));
			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo("TEST"));
		}

		[Test]
		public void ParserCanTransferChains()
		{
			var data = "d";
			var parsingOptions = new ParserOptions("{{#data}}{{.((d(a)))}}{{/data}}", null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<string>(new Func<string, string, string>((s, s1) => s1));

			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo("(d(a))"));
		}

		[Test]
		public void ParserCanFormatMultipleUnnamedWithoutResult()
		{
			var data = 123123123;
			var formatterResult = "";
			var parsingOptions = new ParserOptions("{{#data}}{{.(test, arg, 'arg, arg', ' spaced ', ' spaced with quote \\\" ' , $.$ )}}{{/data}}", null, DefaultEncoding);

			parsingOptions.Formatters.AddFormatter<int>(new Action<int, string[]>(
				(self, test) =>
				{
					Assert.Fail("Should not be invoked");
				}));

			parsingOptions.Formatters.AddFormatter<int>(new Action<int, string, string, string, string, string, int>(
				(self, test, arg, argarg, spacedArg, spacedWithQuote, refSelf) =>
				{
					formatterResult = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}", self, test, arg, argarg, spacedArg, spacedWithQuote,
						refSelf);
				}));

			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.Empty);
			Assert.That(formatterResult, Is.EqualTo("123123123|test|arg|arg, arg| spaced | spaced with quote \\\" |123123123"));
		}

		[Test]
		public void ParserCanFormatMultipleUnnamed()
		{
			var data = 123123123;
			var parsingOptions = new ParserOptions("{{#data}}{{.(test, arg, 'arg, arg', ' spaced ', ' spaced with quote \\\" ' , $.$ )}}{{/data}}", null, DefaultEncoding);


			parsingOptions.Formatters.AddFormatter<int>(new Func<int, string, string, string, string, string, int, string>(
				(self, test, arg, argarg, spacedArg, spacedWithQuote, refSelf) =>
			{
				return string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}", self, test, arg, argarg, spacedArg, spacedWithQuote,
					refSelf);
			}));

			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo("123123123|test|arg|arg, arg| spaced | spaced with quote \\\" |123123123"));
		}

		[Test]
		public void ParserCanFormatMultipleUnnamedParams()
		{
			var data = 123123123;
			var parsingOptions = new ParserOptions("{{#data}}{{.( arg, 'arg, arg', ' spaced ', [testArgument]test, ' spaced with quote \\\" ' , $.$)}}{{/data}}", null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<int>(new Func<int, string, object[], string>(UnnamedParamsFormatter));

			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo("123123123|test|arg|arg, arg| spaced | spaced with quote \\\" |123123123"));
		}

		private string UnnamedParamsFormatter(int self, string testArgument, params object[] other)
		{
			return string.Format("{0}|{1}|{2}", self, testArgument, other.Aggregate((e, f) => e + "|" + f));
		}

		[Test]
		public void ParserCanFormatMultipleNamed()
		{
			var data = 123123123;
			var parsingOptions = new ParserOptions("{{#data}}{{.([refSelf] $.$, arg,[Fob]test, [twoArgs]'arg, arg', [anySpaceArg]' spaced ')}}{{/data}}", null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<int>(new Func<int, string, string, string, string, int, string>(NamedFormatter));

			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo("123123123|test|arg|arg, arg| spaced |123123123"));
		}

		private string NamedFormatter(int self, [FormatterArgumentName("Fob")]string testArgument, string arg, string twoArgs, string anySpaceArg, int refSelf)
		{
			return string.Format("{0}|{1}|{2}|{3}|{4}|{5}", self, testArgument, arg, twoArgs, anySpaceArg, refSelf);
		}

		[Test]
		public void ParserCanCheckCanFormat()
		{
			var data = "d";
			var parsingOptions = new ParserOptions("{{#data}}{{.((d(a)))}}{{/data}}", null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<string>(new Func<string, string, string, string>((s, inv, inva) => throw new Exception("A")));

			parsingOptions.Formatters.AddFormatter<string>(new Func<string, string>((s) => throw new Exception("Wrong Ordering")));
			parsingOptions.Formatters.AddFormatter<string>(new Action<string>((s) => throw new Exception("Wrong Return Ordering")));
			parsingOptions.Formatters.AddFormatter<string>(new Func<string, string, string>((s, inv) => inv));

			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo("(d(a))"));
		}

		[Test]
		public void ParserCanChainWithAndWithoutFormat()
		{
			var data = DateTime.UtcNow;
			var parsingOptions = new ParserOptions("{{data().TimeOfDay.Ticks().()}}", null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<string, string>((s) => s);
			parsingOptions.Formatters.AddFormatter<DateTime, DateTime>((s) => s);
			parsingOptions.Formatters.AddFormatter<long, TimeSpan>((s) => new TimeSpan(s));
			var results = Parser.ParseWithOptions(parsingOptions);
			var result = results.CreateAndStringify(new Dictionary<string, object> { { "data", data } });
			Assert.That(result, Is.EqualTo(new TimeSpan(data.TimeOfDay.Ticks).ToString()));
		}

		[Test]
		public void ParserCanFormatAndCombine()
		{
			var data = DateTime.UtcNow;
			var results = Parser.ParseWithOptions(new ParserOptions("{{data(d).Year}},{{data}}", null, DefaultEncoding));
			//this should compile as its valid but not work as the Default
			//settings for DateTime are ToString(Arg) so it should return a string and not an object
			Assert.That(results
				.CreateAndStringify(new Dictionary<string, object> { { "data", data } }), Is.EqualTo(string.Empty + "," + data));
		}

		[Test]
		public void ParserCanFormatArgumentWithExpression()
		{
			var dt = DateTime.Now;
			var extendedParseInformation = Parser.ParseWithOptions(new ParserOptions("{{data($testFormat$)}}", null, DefaultEncoding));

			var format = "yyyy.mm";
			var andStringify = extendedParseInformation.CreateAndStringify(new Dictionary<string, object>
			{
				{"data", dt},
				{"testFormat", format}
			});

			Assert.That(andStringify, Is.EqualTo(dt.ToString(format)));
		}

		[Test]
		public async Task ParserCanPartials()
		{
			var data = new Dictionary<string, object>();
			data["Data"] = new List<object>()
			{
				new Dictionary<string, object>()
				{
					{
						"self", new Dictionary<string, object>()
						{
							{"Test", 1}
						}
					}
				},
				new Dictionary<string, object>()
				{
					{
						"self", new Dictionary<string, object>()
						{
							{"Test", 2}
						}
					}
				},
				new Dictionary<string, object>()
				{
					{
						"self", new Dictionary<string, object>()
						{
							{"Test", 3}
						}
					}
				}
			};

			var template = @"{{#declare TestPartial}}{{self.Test}}{{/declare}}{{#each Data}}{{#include TestPartial}}{{/each}}";

			var parsed = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding));
			var andStringify = await parsed.CreateAndStringifyAsync(data);
			Assert.That(andStringify, Is.EqualTo("123"));
		}

		[Test]
		public async Task ParserCanPartialsOneUp()
		{
			var data = new Dictionary<string, object>();
			data["Data"] = new List<object>()
			{
				new Dictionary<string, object>()
				{
					{
						"self", new Dictionary<string, object>()
						{
							{"Test", 1}
						}
					}
				},
				new Dictionary<string, object>()
				{
					{
						"self", new Dictionary<string, object>()
						{
							{"Test", 2}
						}
					}
				}
			};

			data["DataOneUp"] =
				new Dictionary<string, object>()
				{
					{
						"self", new Dictionary<string, object>()
						{
							{"Test", "Is:"}
						}
					}
				};

			var template = @"{{#declare TestPartial}}{{../../DataOneUp.self.Test}}{{self.Test}}{{/declare}}{{#each Data}}{{#include TestPartial}}{{/each}}";

			var parsed = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding));
			var andStringify = await parsed.CreateAndStringifyAsync(data);
			Assert.That(andStringify, Is.EqualTo("Is:1Is:2"));
		}

		[Test]
		public void ParserThrowsOnInfiniteNestedCalls()
		{
			var data = new Dictionary<string, object>();
			var template = @"{{#declare TestPartial}}{{#include TestPartial}}{{/declare}}{{#include TestPartial}}";

			var parsed = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding));
			Assert.That(async () => await parsed.CreateAndStringifyAsync(data), Throws.Exception.TypeOf<MustachioStackOverflowException>());
		}

		[Test]
		public async Task ParserCanCreateNestedPartials()
		{
			var data = new Dictionary<string, object>();
			var template = @"{{#declare TestPartial}}{{#declare InnerPartial}}1{{/declare}}2{{/declare}}{{#include TestPartial}}{{#include InnerPartial}}";

			var parsed = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding));
			var result = await parsed.CreateAndStringifyAsync(data);
			Assert.That(result, Is.EqualTo("21"));
		}

		[Test]
		public async Task ParserCanPrintNested()
		{
			var data = new Dictionary<string, object>();
			//declare TestPartial -> Print Recursion -> If Recursion is smaller then 10 -> Print TestPartial
			//Print TestPartial
			var template = @"{{#declare TestPartial}}{{$recursion}}{{#$recursion()}}{{#include TestPartial}}{{/$recursion()}}{{/declare}}{{#include TestPartial}}";

			var parsingOptions = new ParserOptions(template, null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<int, bool>(e => { return e < 9; });
			var parsed = Parser.ParseWithOptions(parsingOptions);
			var result = await parsed.CreateAndStringifyAsync(data);
			Assert.That(result, Is.EqualTo("123456789"));
		}

		[Test]
		public void ParserCanInferCollection()
		{
			var results = Parser.ParseWithOptions(new ParserOptions(
				"{{#Person}}{{Name}}{{#each ../Person.FavoriteColors}}{{.}}{{/each}}{{/Person}}", null, null, 0, false,
				true));

			var expected = @"{
				""Person"" :{
					""Name"" : ""Name_Value"",
					""FavoriteColors"" : [
						""FavoriteColors_1"",
						""FavoriteColors_2"",
						""FavoriteColors_3""
					 ]
				}
			}".EliminateWhitespace();

			var actual = JsonConvert.SerializeObject(results.InferredModel?.RepresentedContext()).EliminateWhitespace();

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void ParserCanInferNestedProperties()
		{
			var results =
				Parser.ParseWithOptions(new ParserOptions("{{#Person}}{{Name}}{{/Person}}", null, null, 0, false,
					true));

			var expected = @"{
				""Person"" :{
					""Name"" : ""Name_Value""
				}
			}".EliminateWhitespace();

			var actual = JsonConvert.SerializeObject(results.InferredModel?.RepresentedContext()).EliminateWhitespace();

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void ParserCanInferScalar()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("{{Name}}", null, null, 0, false, true));
			var expected = @"{""Name"" : ""Name_Value""}".EliminateWhitespace();
			var actual = JsonConvert.SerializeObject(results.InferredModel?.RepresentedContext()).EliminateWhitespace();

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void ParserCanParseEmailAcidTest()
		{
			#region Email ACID Test Body:

			var emailACIDTest = @"
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<title></title>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<meta http-equiv=""Content-Language"" content=""en-us"" />
<style type=""text/css"" media=""screen"">

	/* common
	--------------------------------------------------*/

	body {
		margin: 0px;
		padding: 0px;
		color: #fff;
		background: #930;
	}
	#BodyImposter {
		color: #fff;
		background: #930 url(""img/bgBody.gif"") repeat-x;
		background-color: #930;
		font-family: Arial, Helvetica, sans-serif;
		width: 100%;
		margin: 0px;
		padding: 0px;
		text-align: center;
	}
	#BodyImposter dt {
		font-size: 14px;
		line-height: 1.5em;
		font-weight: bold;
	}
	#BodyImposter dd,
	#BodyImposter li,
	#BodyImposter p,
	#WidthHeight span {
		font-size: 12px;
		line-height: 1.5em;
	}
	#BodyImposter dd,
	#BodyImposter dt {
		margin: 0px;
		padding: 0px;
	}
	#BodyImposter dl,
	#BodyImposter ol,
	#BodyImposter p,
	#BodyImposter ul {
		margin: 0px 0px 4px 0px;
		padding: 10px;
		color: #fff;
		background: #ad5c33;
	}
	#BodyImposter small {
		font-size: 11px;
		font-style: italic;
	}
	#BodyImposter ol li {
		margin: 0px 0px 0px 20px;
		padding: 0px;
	}
	#BodyImposter ul#BulletBg li {
		background: url(""img/bullet.gif"") no-repeat 0em 0.2em;
		padding: 0px 0px 0px 20px;
		margin: 0px;
		list-style: none;
	}
	#BodyImposter ul#BulletListStyle li {
		margin: 0px 0px 0px 22px;
		padding: 0px;
		list-style: url(""img/bullet.gif"");
	}

	/* links
	--------------------------------------------------*/

	#BodyImposter a {
		text-decoration: underline;
	}
	#BodyImposter a:link,
	#BodyImposter a:visited {
		color: #dfb8a4;
		background: #ad5c33;
	}
	#ButtonBorders a:link,
	#ButtonBorders a:visited {
		color: #fff;
		background: #892e00;
	}
	#BodyImposter a:hover {
		text-decoration: none;
	}
	#BodyImposter a:active {
		color: #000;
		background: #ad5c33;
		text-decoration: none;
	}

	/* heads
	--------------------------------------------------*/

	#BodyImposter h1,
	#BodyImposter h2,
	#BodyImposter h3 {
		color: #fff;
		background: #ad5c33;
		font-weight: bold;
		line-height: 1em;
		margin: 0px 0px 4px 0px;
		padding: 10px;
	}
	#BodyImposter h1 {
		font-size: 34px;
	}
	#BodyImposter h2 {
		font-size: 22px;
	}
	#BodyImposter h3 {
		font-size: 16px;
	}
	#BodyImposter h1:hover,
	#BodyImposter h2:hover,
	#BodyImposter h3:hover,
	#BodyImposter dl:hover,
	#BodyImposter ol:hover,
	#BodyImposter p:hover,
	#BodyImposter ul:hover {
		color: #fff;
		background: #892e00;
	}

	/* boxes
	--------------------------------------------------*/

	#Box {
		width: 470px;
		margin: 0px auto;
		padding: 40px 20px;
		text-align: left;
	}
	p#ButtonBorders {
		clear: both;
		color: #fff;
		background: #892e00;
		border-top: 10px solid #ad5c33;
		border-right: 1px dotted #ad5c33;
		border-bottom: 1px dashed #ad5c33;
		border-left: 1px dotted #ad5c33;
	}
	p#ButtonBorders a#Arrow {
		padding-right: 20px;
		background: url(""img/arrow.gif"") no-repeat right 2px;
	}
	p#ButtonBorders a {
		color: #fff;
		background-color: #892e00;
	}
	p#ButtonBorders a#Arrow:hover {
		background-position: right -38px;
	}
	#Floater {
		width: 470px;
	}
	#Floater #Left {
		float: left;
		width: 279px;
		height: 280px;
		color: #fff;
		background: #892e00;
		margin-bottom: 4px;
	}
	#Floater #Right {
		float: right;
		width: 187px;
		height: 280px;
		color: #fff;
		background: #892e00 url(""img/ornament.gif"") no-repeat right bottom;
		margin-bottom: 4px;
	}
	#Floater #Right p {
		color: #fff;
		background: transparent;
	}
	#FontInheritance {
		font-family: Georgia, Times, serif;
	}
	#MarginPaddingOut {
		padding: 20px;
	}
	#MarginPaddingOut #MarginPaddingIn {
		padding: 15px;
		color: #fff;
		background: #ad5c33;
	}
	#MarginPaddingOut #MarginPaddingIn img {
		background: url(""img/bgPhoto.gif"") no-repeat;
		padding: 15px;
	}
	span#SerifFont {
		font-family: Georgia, Times, serif;
	}
	p#QuotedFontFamily {
		font-family: ""Trebuchet MS"", serif;
	}
	#WidthHeight {
		width: 470px;
		height: 200px;
		color: #fff;
		background: #892e00;
	}
	#WidthHeight span {
		display: block;
		padding: 10px;
	}

</style>

</head>

<body>
<div id=""BodyImposter"">
	<div id=""Box"">
		<div id=""FontInheritance"">
			<h1>H1 headline (34px/1em)</h1>
			<h2>H2 headline (22px/1em)</h2>
			<h3>H3 headline (16px/1em)</h3>
		</div>
		<p>Paragraph (12px/1.5em) Lorem ipsum dolor sit amet, <a href=""http://www.email-standards.org/"">consectetuer adipiscing</a> elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. <span id=""SerifFont"">(This is a serif font inside of a paragraph styled with a sans-serif font.)</span> <small>(This is <code>small</code> text.)</small></p>
		<p id=""QuotedFontFamily"">This is a font (Trebuchet MS) which needs quotes because its label comprises two words.</p>
		<ul id=""BulletBg"">
			<li>background bullet eum iriure dolor in hendrerit in</li>
			<li>vulputate velit esse molestie consequat, vel illum dolore eu</li>
			<li>feugiat nulla facilisis at vero eros et accumsan et iusto odio</li>
		</ul>
		<ul id=""BulletListStyle"">
			<li>list-style bullet eum iriure dolor in hendrerit in</li>
			<li>vulputate velit esse molestie consequat, vel illum dolore eu</li>
			<li>feugiat nulla facilisis at vero eros et accumsan et iusto odio</li>
		</ul>
		<ol>
			<li>ordered list sit amet, consectetuer adipiscing elit</li>
			<li>sed diam nonummy nibh euismod tincidunt ut laoreet</li>
			<li>dolore magna aliquam erat volutpat. Ut wisi enim ad minim</li>
		</ol>
		<dl>
			<dt>Definition list</dt>
			<dd>lorem ipsum dolor sit amet, consectetuer adipiscing elit</dd>
			<dd>sed diam nonummy nibh euismod tincidunt ut laoreet</dd>
			<dd>dolore magna aliquam erat volutpat. Ut wisi enim ad minim</dd>
		</dl>
		<div id=""Floater"">
			<div id=""Left"">
				<div id=""MarginPaddingOut"">
					<div id=""MarginPaddingIn"">
						<img src=""img/photo.jpg"" width=""180"" height=""180"" alt=""[photo: root canal]"" />
					</div>
				</div>
			</div>
			<div id=""Right"">
				<p>Right float with a positioned background</p>
			</div>
		</div>
		<p id=""ButtonBorders""><a href=""http://www.email-standards.org/"" id=""Arrow"">Borders and an a:hover background image</a></p>
		<div id=""WidthHeight"">
			<span>Width = 470, height = 200</span>
		</div>
	</div>
</div>
<!-- <unsubscribe>Hidden for testing</unsubscribe> -->
</body>
</html>";

			#endregion

			Assert.That(() => Parser.ParseWithOptions(new ParserOptions(emailACIDTest)), Throws.Nothing);
		}

		[Test]
		public void ParserCanProcessComplexValuePath()
		{
			Assert.That(() =>
				Parser.ParseWithOptions(new ParserOptions("{{#content}}Hello {{../Person.Name}}!{{/content}}")), Throws.Nothing);
		}

		//[Test]
		//public void ParserCanProcessRootValuePath()
		//{
		//	Parser.ParseWithOptions(new ParserOptions("{{#content}}Hello {{.../Person.Name}}!{{/content}}"));
		//}

		[Test]
		public void ParserCanProcessCompoundConditionalGroup()
		{
			Assert.That(() =>
			{
				Parser.ParseWithOptions(new ParserOptions(
					"{{#Collection}}Collection has elements{{^Collection}}Collection doesn't have elements{{/Collection}}"));
				Parser.ParseWithOptions(new ParserOptions(
					"{{^Collection}}Collection doesn't have elements{{#Collection}}Collection has elements{{/Collection}}"));
			}, Throws.Nothing);
		}

		[Test]
		public void ParserCanProcessEachConstruct()
		{
			Assert.That(() => { Parser.ParseWithOptions(new ParserOptions("{{#each ACollection}}{{.}}{{/each}}")); }, Throws.Nothing);
		}

		[Test]
		public void ParserCanProcessHandleMultilineTemplates()
		{
			Assert.That(() => Parser.ParseWithOptions(new ParserOptions(@"{{^Collection}}Collection doesn't have
							elements{{#Collection}}Collection has
						elements{{/Collection}}")), Throws.Nothing);
		}

		[Test]
		public void ParserCanProcessSimpleConditionalGroup()
		{
			Assert.That(() =>
				Parser.ParseWithOptions(new ParserOptions("{{#Collection}}Collection has elements{{/Collection}}")), Throws.Nothing);
		}

		[Test]
		public void ParserCanProcessSimpleNegatedCondionalGroup()
		{
			Assert.That(() =>
				Parser.ParseWithOptions(new ParserOptions("{{^Collection}}Collection has no elements{{/Collection}}")), Throws.Nothing);
		}

		[Test]
		public void ParserCanProcessSimpleValuePath()
		{
			Assert.That(() =>
				Parser.ParseWithOptions(new ParserOptions("Hello {{Name}}!")), Throws.Nothing);
		}

		[Test]
		public void ParserChangeDefaultFormatter()
		{
			var dateTime = DateTime.UtcNow;
			var parsingOptions = new ParserOptions("{{data(d).AnyInt}},{{data}}", null, DefaultEncoding);
			parsingOptions.Formatters.AddFormatter<DateTime, object>((dt) => new
			{
				Dt = dt,
				AnyInt = 2
			});
			var results = Parser.ParseWithOptions(parsingOptions);
			//this should not work as the Default settings for DateTime are ToString(Arg) so it should return a string and not an object
			Assert.That(results.CreateAndStringify(new Dictionary<string, object>
				{
					{
						"data", dateTime
					}
				}), Is.EqualTo("2," + dateTime));
		}

		[Test]
		public void ParserProducesEmptyObjectWhenTemplateHasNoMustacheMarkup()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("This template has no mustache thingies.", null,
				null, 0, false, true));

			var expected = @"{}".EliminateWhitespace();

			var actual = JsonConvert.SerializeObject(results.InferredModel?.RepresentedContext()).EliminateWhitespace();

			Assert.That(actual, Is.EqualTo(expected));
		}


		[Test]
		public void ParserRendersCollectionObjectsWhenUsed()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("{{#each Employees}}{{name}}{{/each}}", null, null,
				0, false, true));

			var expected = @"{""Employees"" : [{ ""name"" : ""name_Value""}]}".EliminateWhitespace();

			var actual = JsonConvert.SerializeObject(results.InferredModel?.RepresentedContext()).EliminateWhitespace();

			Assert.That(actual, Is.EqualTo(expected));
		}


		[Test]
		public void ParserRendersCollectionSubObjectsWhenUsed()
		{
			var results = Parser.ParseWithOptions(new ParserOptions(
				"{{#each Employees}}{{person.name}}{{#each favoriteColors}}{{hue}}{{/each}}{{#each workplaces}}{{.}}{{/each}}{{/each}}",
				null, null, 0, false, true));

			var expected = @"{
							""Employees"" : [{
								""person"" : { ""name"" : ""name_Value""},
								""favoriteColors"" : [{""hue"" : ""hue_Value""}],
								""workplaces"" : [ ""workplaces_1"",""workplaces_2"",""workplaces_3"" ]
								}]
							}".EliminateWhitespace();

			var actual = JsonConvert.SerializeObject(results.InferredModel?.RepresentedContext()).EliminateWhitespace();

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void ParserThrowsParserExceptionForEachWithoutPath()
		{
			Assert.Throws(typeof(AggregateException),
				() => Parser.ParseWithOptions(new ParserOptions("{{#eachs}}{{name}}{{/each}}")));
		}

		[Test]
		public void ParserThrowsParserExceptionForEmptyEach()
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions("{{#each}}")));
		}

		[Test]
		public void ParsingThrowsAnExceptionWhenConditionalGroupsAreMismatched()
		{
			Assert.Throws(typeof(AggregateException),
				() => Parser.ParseWithOptions(
					new ParserOptions("{{#Collection}}Collection has elements{{/AnotherCollection}}")));
		}

		[Test]
		public void TestCancelation()
		{
			var token = new CancellationTokenSource();
			var model = new ParserCancellationional(token);
			var extendedParseInformation = Parser.ParseWithOptions(
				new ParserOptions("{{data.ValueA}}{{data.ValueCancel}}{{data.ValueB}}", null, DefaultEncoding));
			var template = extendedParseInformation.CreateAndStringify(new Dictionary<string, object>
			{
				{"data", model}
			}, token.Token);
			Assert.That(template, Is.EqualTo(model.ValueA + model.ValueCancel));
		}

		[Test]
		public void TestCollectionContext()
		{
			var template = "{{#each data}}{{$index}},{{$first}},{{$middel}},{{$last}},{{$odd}},{{$even}}.{{/each}}";

			var elementdata = new List<CollectionContextInfo>
			{
				new CollectionContextInfo
				{
					IndexProp = 0,
					EvenProp = true,
					OddProp = false,
					LastProp = false,
					FirstProp = true,
					MiddelProp = false
				},
				new CollectionContextInfo
				{
					IndexProp = 1,
					EvenProp = false,
					OddProp = true,
					LastProp = false,
					FirstProp = false,
					MiddelProp = true
				},
				new CollectionContextInfo
				{
					IndexProp = 2,
					EvenProp = true,
					OddProp = false,
					LastProp = true,
					FirstProp = false,
					MiddelProp = false
				}
			};

			var parsedTemplate = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding));
			var genTemplate = parsedTemplate.CreateAndStringify(new Dictionary<string, object> { { "data", elementdata } });
			var realData = elementdata.Select(e => e.ToString()).Aggregate((e, f) => e + f);
			Assert.That(genTemplate, Is.EqualTo(realData));
		}
	}
}