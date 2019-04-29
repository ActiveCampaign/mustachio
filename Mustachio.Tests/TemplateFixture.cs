using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace Mustachio.Tests
{
    public class TemplateFixture
    {
        [Fact]
        public void TemplateRendersContentWithNoVariables()
        {
            var plainText = "ASDF";
            var template = Mustachio.Parser.Parse("ASDF");
            Assert.Equal(plainText, template(null));
        }

        [Fact]
        public void HtmlIsNotEscapedWhenUsingUnsafeSyntaxes()
        {
            var model = new Dictionary<string, object>();

            model["stuff"] = "<b>inner</b>";

            var plainText = @"{{{stuff}}}";
            var rendered = Mustachio.Parser.Parse(plainText)(model);

            Assert.Equal("<b>inner</b>", rendered);

            plainText = @"{{&stuff}}";
            rendered = Mustachio.Parser.Parse(plainText)(model);
            Assert.Equal("<b>inner</b>", rendered);
        }

        [Fact]
        public void HtmlIsEscapedByDefault()
        {
            var model = new Dictionary<string, object>();

            model["stuff"] = "<b>inner</b>";

            var plainText = @"{{stuff}}";
            var rendered = Mustachio.Parser.Parse(plainText)(model);

            Assert.Equal("&lt;b&gt;inner&lt;/b&gt;", rendered);
        }

        [Fact]
        public void CommentsAreExcludedFromOutput()
        {
            var model = new Dictionary<string, object>();

            var plainText = @"as{{!stu
            ff}}df";
            var rendered = Mustachio.Parser.Parse(plainText)(model);

            Assert.Equal("asdf", rendered);
        }

        [Fact]
        public void NegationGroupRendersContentWhenValueNotSet()
        {
            var model = new Dictionary<string, object>();

            var plainText = @"{{^stuff}}No Stuff Here.{{/stuff}}";
            var rendered = Mustachio.Parser.Parse(plainText)(model);

            Assert.Equal("No Stuff Here.", rendered);
        }


        [Fact]
        public void TemplateRendersWithComplextEachPath()
        {
            var template = @"{{#each Company.ceo.products}}<li>{{ name }} and {{version}} and has a CEO: {{../../last_name}}</li>{{/each}}";

            var parsedTemplate = Parser.Parse(template);

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

            var result = parsedTemplate(model);

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

            var result = Parser.Parse(template)(model);

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

            var result = Parser.Parse(template)(model);

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

            var result = Parser.Parse(template)(model);

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

            var result = Parser.Parse(template)(model);

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

            var result = Parser.Parse(template)(model);

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

            var result = Parser.Parse(template)(model);

            Assert.Equal("You've won  times!", result);
        }

        [Fact]
        public void ExpandedTokensShouldBeRenderedByDefault()
        {
            var baseTemplate = "Hello, {{{ @title }}}!!!";
            var titleData = "Mr. User";

            var tokenExpander = new TokenExpander
            {
                RegEx = "{{{ @title }}}",
                ExpandTokens = (s, baseOptions) => Tokenizer.Tokenize(titleData, new ParsingOptions()),
                Precedence = Precedence.Medium
            };
            var model = new Dictionary<string, object>();
            var parsingOptions = new ParsingOptions { TokenExpanders = new[] { tokenExpander } };

            var result = Parser.Parse(baseTemplate, parsingOptions)(model);

            // The custom token will not be rendered by default.
            Assert.Equal("Hello, Mr. User!!!", result);
        }

        [Fact]
        public void ExpandedTokensShouldProcessVariables()
        {
            var baseTemplate = "Hello, {{{ @title }}}!!!";
            var titleData = "Mr. {{ userId }}";

            var tokenExpander = new TokenExpander
            {
                RegEx = "{{{ @title }}}",
                ExpandTokens = (s, baseOptions) => Tokenizer.Tokenize(titleData, new ParsingOptions()),
                Precedence = Precedence.Medium
            };
            var expectedName = "Ralph";
            var model = new Dictionary<string, object> { ["userId"] = expectedName };
            var parsingOptions = new ParsingOptions { TokenExpanders = new[] { tokenExpander } };

            var result = Parser.Parse(baseTemplate, parsingOptions)(model);

            Assert.Equal($"Hello, Mr. {expectedName}!!!", result);
        }

        [Fact]
        public void ExpandedTokensShouldProcessComplexVariableStructures()
        {
            var baseTemplate = @"{{#each Company.ceo.products}}{{{ @content }}}{{/each}}";
            var contentData = "<li>{{ name }} and {{version}} and has a CEO: {{../../last_name}}</li>";

            var model = new Dictionary<string, object>();

            var company = new Dictionary<string, object>();
            model["Company"] = company;

            var ceo = new Dictionary<string, object>();
            company["ceo"] = ceo;
            ceo["last_name"] = "Smith";

            var products = Enumerable
                .Range(0, 3)
                .Select(k => new Dictionary<string, object> { ["name"] = "name " + k, ["version"] = "version " + k })
                .ToArray();

            ceo["products"] = products;

            var tokenExpander = new TokenExpander
            {
                RegEx = "{{{ @content }}}",
                ExpandTokens = (s, baseOptions) => Tokenizer.Tokenize(contentData, new ParsingOptions()),
                Precedence = Precedence.Medium
            };
            var parsingOptions = new ParsingOptions { TokenExpanders = new[] { tokenExpander } };

            var result = Parser.Parse(baseTemplate, parsingOptions)(model);

            Assert.Equal("<li>name 0 and version 0 and has a CEO: Smith</li>" +
                         "<li>name 1 and version 1 and has a CEO: Smith</li>" +
                         "<li>name 2 and version 2 and has a CEO: Smith</li>", result);
        }

        [Fact]
        public void ExpandedTokensCustomRenderingIsUsed()
        {
            var baseTemplate = "Hello, {{{ @title }}}!!!";
            var titleData = "Mr. User";

            var expectedCustomToken = "1234";
            var tokenExpander = new TokenExpander
            {
                RegEx = "{{{ @title }}}",
                RenderTokens = (tokenString, queue, options, inferredModel) =>
                {
                    return (builder, context) => { builder.Append(expectedCustomToken); };
                },
                ExpandTokens = (s, baseOptions) => Tokenizer.Tokenize(titleData, new ParsingOptions()),
                Precedence = Precedence.Medium
            };
            var model = new Dictionary<string, object>();
            var parsingOptions = new ParsingOptions { TokenExpanders = new[] { tokenExpander } };

            var result = Parser.Parse(baseTemplate, parsingOptions)(model);

            Assert.Equal($"Hello, {expectedCustomToken}Mr. User!!!", result);
        }

        [Fact]
        public void TokenExpanderPrecedenceIsRespected()
        {
            var baseTemplate = "Hello, {{{ title }}}!!!";
            var titleData = "Mr. User";

            var tokenExpander = new TokenExpander
            {
                RegEx = "{{{ title }}}",
                ExpandTokens = (s, baseOptions) => Tokenizer.Tokenize(titleData, new ParsingOptions()),
                Precedence = Precedence.Low
            };
            var model = new Dictionary<string, object> { ["title"] = "Mr. Bob" };
            var parsingOptions = new ParsingOptions { TokenExpanders = new[] { tokenExpander } };

            var result = Parser.Parse(baseTemplate, parsingOptions)(model);

            Assert.Equal($"Hello, Mr. Bob!!!", result);

            // Testing with Medium Precedence. It should use our token expander this time.
            tokenExpander.Precedence = Precedence.Medium;
            result = Parser.Parse(baseTemplate, parsingOptions)(model);

            Assert.Equal($"Hello, Mr. User!!!", result);
        }
    }
}
