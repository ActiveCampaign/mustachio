using Mustachio;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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
    }
}
