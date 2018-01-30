using Mustachio;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace Mustachio.Tests
{
	/// <summary>
	/// Allows for simpler comparison of template results that don't demand
	/// </summary>
	internal static class WhitespaceNormalizer
	{
		private static Regex WHITESPACE_NORMALIZER = new Regex("[\\s]+", RegexOptions.Compiled);
		/// <summary>
		/// Provides a mechanism to make comparing expected and actual results a little more sane to author.
		/// You may include whitespace in resources to make them easier to read.
		/// </summary>
		/// <param name="subject"></param>
		/// <returns></returns>
		internal static string EliminateWhitespace(this string subject)
		{
			return WHITESPACE_NORMALIZER.Replace(subject, "");
		}
	}

	public class ParserCancellationional
	{
		private readonly CancellationTokenSource _tokenSource;
		private string _valueCancel;

		public ParserCancellationional(CancellationTokenSource tokenSource)
		{
			_tokenSource = tokenSource;
			ValueA = "ValueA";
			ValueB = "ValueB";
			ValueCancel = "ValueCancel";
		}

		public string ValueA { get; set; }
		public string ValueB { get; set; }

		public string ValueCancel
		{
			get
			{
				_tokenSource.Cancel();
				return _valueCancel;
			}
			set { _valueCancel = value; }
		}
	}

	public class ParserFixture
	{
		private static Encoding _defaultEncoding = new UnicodeEncoding(true, false, false);

		public static Encoding DefaultEncoding
		{
			get { return _defaultEncoding; }
			set { _defaultEncoding = value; }
		}

		[Fact]
		public void ParserCanProcessCompoundConditionalGroup()
		{
			Parser.ParseWithOptions(new ParserOptions("{{#Collection}}Collection has elements{{^Collection}}Collection doesn't have elements{{/Collection}}"));
			Parser.ParseWithOptions(new ParserOptions("{{^Collection}}Collection doesn't have elements{{#Collection}}Collection has elements{{/Collection}}"));
		}

		[Fact]
		public void ParserCanProcessHandleMultilineTemplates()
		{
			Parser.ParseWithOptions(new ParserOptions(@"{{^Collection}}Collection doesn't have
                            elements{{#Collection}}Collection has
                        elements{{/Collection}}"));
		}

		[Fact]
		public void ParsingThrowsAnExceptionWhenConditionalGroupsAreMismatched()
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions("{{#Collection}}Collection has elements{{/AnotherCollection}}")));
		}

		[Fact]
		public void ParserCanProcessSimpleConditionalGroup()
		{
			Parser.ParseWithOptions(new ParserOptions("{{#Collection}}Collection has elements{{/Collection}}"));
		}

		[Fact]
		public void ParserCanProcessSimpleNegatedCondionalGroup()
		{
			Parser.ParseWithOptions(new ParserOptions("{{^Collection}}Collection has no elements{{/Collection}}"));
		}

		[Fact]
		public void ParserCanProcessSimpleValuePath()
		{
			Parser.ParseWithOptions(new ParserOptions("Hello {{Name}}!"));
		}

		[Fact]
		public void ParserCanProcessComplexValuePath()
		{
			Parser.ParseWithOptions(new ParserOptions("{{#content}}Hello {{../Person.Name}}!{{/content}}"));
		}

		[Fact]
		public void ParserCanProcessEachConstruct()
		{
			Parser.ParseWithOptions(new ParserOptions("{{#each ACollection}}{{.}}{{/each}}"));
		}

		[Fact]
		public async Task TestCancelation()
		{
			var token = new CancellationTokenSource();
			var model = new ParserCancellationional(token);
			var extendedParseInformation = Parser.ParseWithOptions(new ParserOptions("{{data.ValueA}}{{data.ValueCancel}}{{data.ValueB}}", null, DefaultEncoding));
			var template = extendedParseInformation.ParsedTemplateWithCancelation(new Dictionary<string, object>()
			{
				{"data", model}
			}, token.Token).Stringify(true, DefaultEncoding);
			Assert.Equal(model.ValueA + model.ValueCancel, template);
		}

		class CollectionContextInfo
		{
			public int IndexProp { get; set; }
			public bool FirstProp { get; set; }
			public bool MiddelProp { get; set; }
			public bool LastProp { get; set; }

			public bool OddProp { get; set; }
			public bool EvenProp { get; set; }

			public override string ToString()
			{
				return string.Format("{0},{1},{2},{3},{4},{5}.", IndexProp, FirstProp, MiddelProp, LastProp, OddProp, EvenProp);
			}
		}

		[Fact]
		public async Task TestCollectionContext()
		{
			var template = "{{#each data}}{{$index}},{{$first}},{{$middel}},{{$last}},{{$odd}},{{$even}}.{{/each}}";

			var elementdata = new List<CollectionContextInfo>()
			{
				new CollectionContextInfo()
				{
					IndexProp = 0,
					EvenProp = true,
					OddProp = false,
					LastProp = false,
					FirstProp = true,
					MiddelProp = false
				},
				new CollectionContextInfo()
				{
					IndexProp = 1,
					EvenProp = false,
					OddProp = true,
					LastProp = false,
					FirstProp = false,
					MiddelProp = true
				},
				new CollectionContextInfo()
				{
					IndexProp = 2,
					EvenProp = true,
					OddProp = false,
					LastProp = true,
					FirstProp = false,
					MiddelProp = false
				},
			};

			var parsedTemplate = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding));
			var genTemplate = parsedTemplate.ParsedTemplate(new Dictionary<string, object>() {{"data", elementdata}})
				.Stringify(true, DefaultEncoding);
			var realData = elementdata.Select(e => e.ToString()).Aggregate((e, f) => e + f);
			Assert.Equal(realData, genTemplate);
		}

		[Theory]
		[InlineData("d")]
		[InlineData("D")]
		[InlineData("f")]
		[InlineData("F")]
		public void ParserCanFormat(string dtFormat)
		{
			var data = DateTime.UtcNow;
			var resuts = Parser.ParseWithOptions(new ParserOptions("{{data(" + dtFormat + ")}}", null, DefaultEncoding));
			var result = resuts.ParsedTemplate(new Dictionary<string, object>() { { "data", data } })
							   .Stringify(true, DefaultEncoding);
			Assert.Equal(result, data.ToString(dtFormat));
		}

		[Fact]
		public void ParserCanFormatAndCombine()
		{
			var data = DateTime.UtcNow;
			var resuts = Parser.ParseWithOptions(new ParserOptions("{{data(d).Year}}", null, DefaultEncoding));
			//this should compile as its valid but not work as the Default
			//settings for DateTime are ToString(Arg) so it should return a string and not an object
			Assert.Equal(string.Empty, resuts.ParsedTemplate(new Dictionary<string, object>() { { "data", data } })
												   .Stringify(true, DefaultEncoding));
		}

		[Fact]
		public void ParserChangeDefaultFormatter()
		{
			var data = DateTime.UtcNow;
			var options = new ParserOptions("{{data(d).AnyInt}}", null, DefaultEncoding);
			options.Formatters.Add(typeof(DateTime), (dt, arg) => new
			{
				Dt = dt,
				AnyInt = 2
			});
			var resuts = Parser.ParseWithOptions(options);
			//this should not work as the Default settings for DateTime are ToString(Arg) so it should return a string and not an object
			Assert.Equal("2", resuts.ParsedTemplate(new Dictionary<string, object>() { { "data", data } })
												   .Stringify(true, DefaultEncoding));
		}

		[Theory]
		[InlineData("{{data(d))}}")]
		[InlineData("{{data(d)ddd}}")]

		[InlineData("{{data((d)}}")]
		[InlineData("{{data((d))}}")]
		[InlineData("{{data)}}")]
		[InlineData("{{data(}}")]

		public void ParserThrowsAnExceptionWhenFormatIsMismatched(string invalidTemplate)
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions(invalidTemplate)));
		}

		[Theory]
		[InlineData("{{#ACollection}}{{.}}{{/each}}")]
		[InlineData("{{#ACollection}}{{.}}{{/ACollection}}{{/each}}")]
		[InlineData("{{/each}}")]
		public void ParserThrowsAnExceptionWhenEachIsMismatched(string invalidTemplate)
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions(invalidTemplate)));
		}

		[Fact]
		public void ParserCanInferCollection()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("{{#Person}}{{Name}}{{#each ../Person.FavoriteColors}}{{.}}{{/each}}{{/Person}}", null, null, 0, false, true));

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

			var actual = results.InferredModel.ToString().EliminateWhitespace();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ParserCanInferScalar()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("{{Name}}", null, null, 0, false, true));
			var expected = @"{""Name"" : ""Name_Value""}".EliminateWhitespace();
			var actual = results.InferredModel.ToString().EliminateWhitespace();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ParserCanInferNestedProperties()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("{{#Person}}{{Name}}{{/Person}}", null, null, 0, false, true));

			var expected = @"{
                ""Person"" :{
                    ""Name"" : ""Name_Value""
                }
            }".EliminateWhitespace();

			var actual = results.InferredModel.ToString().EliminateWhitespace();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ParserProducesEmptyObjectWhenTemplateHasNoMustacheMarkup()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("This template has no mustache thingies.", null, null, 0, false, true));

			var expected = @"{}".EliminateWhitespace();

			var actual = results.InferredModel.ToString().EliminateWhitespace();

			Assert.Equal(expected, actual);
		}


		[Fact]
		public void ParserRendersCollectionObjectsWhenUsed()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("{{#each Employees}}{{name}}{{/each}}", null, null, 0, false, true));

			var expected = @"{""Employees"" : [{ ""name"" : ""name_Value""}]}".EliminateWhitespace();

			var actual = results.InferredModel.ToString().EliminateWhitespace();

			Assert.Equal(expected, actual);
		}


		[Fact]
		public void ParserRendersCollectionSubObjectsWhenUsed()
		{
			var results = Parser.ParseWithOptions(new ParserOptions("{{#each Employees}}{{person.name}}{{#each favoriteColors}}{{hue}}{{/each}}{{#each workplaces}}{{.}}{{/each}}{{/each}}", null, null, 0, false, true));

			var expected = @"{
                            ""Employees"" : [{
                                ""person"" : { ""name"" : ""name_Value""},
                                ""favoriteColors"" : [{""hue"" : ""hue_Value""}],
                                ""workplaces"" : [ ""workplaces_1"",""workplaces_2"",""workplaces_3"" ]
                                }]
                            }".EliminateWhitespace();

			var actual = results.InferredModel.ToString().EliminateWhitespace();

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void ParserThrowsParserExceptionForEmptyEach()
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions("{{#each}}")));
		}

		[Fact]
		public void ParserThrowsParserExceptionForEachWithoutPath()
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions("{{#eachs}}{{name}}{{/each}}")));
		}

		[Theory]
		[InlineData("{{Mike", "{{{{name}}")]
		[InlineData("{Mike", "{{{name}}")]
		[InlineData("Mike}", "{{name}}}")]
		[InlineData("Mike}}", "{{name}}}}")]
		public void ParserHandlesPartialOpenAndPartialClose(string expected, string template)
		{
			var model = new Dictionary<string, object>();
			model["name"] = "Mike";

			Assert.Equal(expected, Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding)).ParsedTemplate(model).Stringify(true, DefaultEncoding));
		}


		[Theory]
		[InlineData("{{#each element}}{{name}}")]
		[InlineData("{{#element}}{{name}}")]
		[InlineData("{{^element}}{{name}}")]
		public void ParserThrowsParserExceptionForUnclosedGroups(string invalidTemplate)
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions(invalidTemplate)));
		}

		[Fact]
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

			Parser.ParseWithOptions(new ParserOptions(emailACIDTest));
		}


		[Theory]
		[InlineData("{{.../asdf.content}}")]
		[InlineData("{{/}}")]
		[InlineData("{{./}}")]
		[InlineData("{{.. }}")]
		[InlineData("{{..}}")]
		[InlineData("{{//}}")]
		[InlineData("{{@}}")]
		[InlineData("{{[}}")]
		[InlineData("{{]}}")]
		[InlineData("{{)}}")]
		[InlineData("{{(}}")]
		[InlineData("{{()}}")]
		[InlineData("{{~}}")]
		[InlineData("{{%}}")]

		public void ParserShouldThrowForInvalidPaths(string template)
		{
			Assert.Throws(typeof(AggregateException), () => Parser.ParseWithOptions(new ParserOptions(template)));
		}

		[Theory]
		[InlineData("{{first_name}}")]
		[InlineData("{{company.name}}")]
		[InlineData("{{company.address_line_1}}")]
		[InlineData("{{name}}")]
		public void ParserShouldNotThrowForValidPath(string template)
		{
			Parser.ParseWithOptions(new ParserOptions(template));
		}


		[Theory]
		[InlineData("1{{first name}}", 1)]
		[InlineData("ss{{#each company.name}}\nasdf", 1)]
		[InlineData("xzyhj{{#company.address_line_1}}\nasdf{{dsadskl-sasa@}}\n{{/each}}", 3)]
		[InlineData("fff{{#each company.address_line_1}}\n{{dsadskl-sasa@}}\n{{/each}}", 1)]
		[InlineData("a{{name}}dd\ndd{{/each}}dd", 1)]
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
				Assert.Equal(expectedErrorCount, ex.InnerExceptions.Count);
			}
			Assert.True(didThrow);
		}

		[Theory]
		[InlineData("<wbr>", "{{content}}", "&lt;wbr&gt;")]
		[InlineData("<wbr>", "{{{content}}}", "<wbr>")]
		public void ValueEscapingIsActivatedBasedOnValueInterpolationMustacheSyntax(string content, string template, string expected)
		{
			var model = new Dictionary<string, object>(){
				{"content" , content}
			};
			var value = Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding)).ParsedTemplate(model)
							  .Stringify(true, DefaultEncoding);

			Assert.Equal(expected, value);
		}

		[Theory]
		[InlineData("<wbr>", "{{content}}", "<wbr>")]
		[InlineData("<wbr>", "{{{content}}}", "<wbr>")]
		public void ValueEscapingIsDisabledWhenRequested(string content, string template, string expected)
		{
			var model = new Dictionary<string, object>(){
				{"content" , content}
			};
			Assert.Equal(expected, Parser.ParseWithOptions(new ParserOptions(template, null, DefaultEncoding, 0, true)).ParsedTemplate(model).Stringify(true, DefaultEncoding));
		}
	}
}
