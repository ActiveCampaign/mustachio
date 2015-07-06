using Mustachio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    public class ParserFixture
    {
        [Fact]
        public void ParserCanProcessCompoundConditionalGroup()
        {
            Parser.Parse("{{#Collection}}Collection has elements{{^Collection}}Collection doesn't have elements{{/Collection}}");
            Parser.Parse("{{^Collection}}Collection doesn't have elements{{#Collection}}Collection has elements{{/Collection}}");
        }

        [Fact]
        public void ParserCanProcessHandleMultilineTemplates()
        {
            Parser.Parse(@"{{^Collection}}Collection doesn't have
                            elements{{#Collection}}Collection has
                        elements{{/Collection}}");
        }

        [Fact]
        public void ParsingThrowsAnExceptionWhenConditionalGroupsAreMismatched()
        {
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse("{{#Collection}}Collection has elements{{/AnotherCollection}}"));
        }

        [Fact]
        public void ParserCanProcessSimpleConditionalGroup()
        {
            Parser.Parse("{{#Collection}}Collection has elements{{/Collection}}");
        }

        [Fact]
        public void ParserCanProcessSimpleNegatedCondionalGroup()
        {
            Parser.Parse("{{^Collection}}Collection has no elements{{/Collection}}");
        }

        [Fact]
        public void ParserCanProcessSimpleValuePath()
        {
            Parser.Parse("Hello {{Name}}!");
        }

        [Fact]
        public void ParserCanProcessComplexValuePath()
        {
            Parser.Parse("{{#content}}Hello {{../Person.Name}}!{{/content}}");
        }

        [Fact]
        public void ParserCanProcessEachConstruct()
        {
            Parser.Parse("{{#each ACollection}}{{.}}{{/each}}");
        }

        [Fact]
        public void ParserThrowsAnExceptionWhenEachIsMismatched()
        {
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse("{{#ACollection}}{{.}}{{/each}}"));
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse("{{#ACollection}}{{.}}{{/ACollection}}{{/each}}"));
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse("{{/each}}"));
        }

        [Fact]
        public void ParserCanInferCollection()
        {
            var results = Parser.ParseWithModelInference("{{#Person}}{{Name}}{{#each ../Person.FavoriteColors}}{{.}}{{/each}}{{/Person}}");

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
            var results = Parser.ParseWithModelInference("{{Name}}");
            var expected = @"{""Name"" : ""Name_Value""}".EliminateWhitespace();
            var actual = results.InferredModel.ToString().EliminateWhitespace();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParserCanInferNestedProperties()
        {
            var results = Parser.ParseWithModelInference("{{#Person}}{{Name}}{{/Person}}");

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
            var results = Parser.ParseWithModelInference("This template has no mustache thingies.");

            var expected = @"{}".EliminateWhitespace();

            var actual = results.InferredModel.ToString().EliminateWhitespace();

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void ParserRendersCollectionObjectsWhenUsed()
        {
            var results = Parser.ParseWithModelInference("{{#each Employees}}{{name}}{{/each}}");

            var expected = @"{""Employees"" : [{ ""name"" : ""name_Value""}]}".EliminateWhitespace();

            var actual = results.InferredModel.ToString().EliminateWhitespace();

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void ParserRendersCollectionSubObjectsWhenUsed()
        {
            var results = Parser.ParseWithModelInference("{{#each Employees}}{{person.name}}{{#each favoriteColors}}{{hue}}{{/each}}{{#each workplaces}}{{.}}{{/each}}{{/each}}");

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
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse("{{#each}}"));
        }

        [Fact]
        public void ParserThrowsParserExceptionForEachWithoutPath()
        {
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse("{{#eachs}}{{name}}{{/each}}"));
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

            Assert.Equal(expected, Parser.Parse(template)(model));
        }


        [Theory]
        [InlineData("{{#each element}}{{name}}")]
        [InlineData("{{#element}}{{name}}")]
        [InlineData("{{^element}}{{name}}")]
        public void ParserThrowsParserExceptionForUnclosedGroups(string invalidTemplate)
        {
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse(invalidTemplate));
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

            Parser.Parse(emailACIDTest);
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
        [InlineData("{{~}}")]
        [InlineData("{{$}}")]
        [InlineData("{{%}}")]
        public void ParserShouldThrowForInvalidPaths(string template)
        {
            Assert.Throws(typeof(TemplateParseException), () => Parser.Parse(template));
        }

        [Theory]
        [InlineData("{{first_name}}")]
        [InlineData("{{company.name}}")]
        [InlineData("{{company.address_line_1}}")]
        [InlineData("{{name}}")]
        public void ParserShouldNotThrowForValidPath(string template)
        {
            Parser.Parse(template);
        }

    }
}
