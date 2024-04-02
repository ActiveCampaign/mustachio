<img src="https://newsletter.postmarkapp.com/assets/images/open-source/mustachio-logo@2x.png" alt="Mustachio Logo" title="Pistachio + Mustache =&gt; Mustachio" width="148" height="149">

# Mustachio

[![Nuget](https://img.shields.io/nuget/v/Mustachio)](https://www.nuget.org/packages/Mustachio/)

A Lightweight, powerful, flavorful, templating engine for C# and other .net-based languages.

#### What's this for?

*Mustachio* allows you to create simple text-based templates that are fast and safe to render. It's the heart of [Postmark Templates](https://postmarkapp.com/blog/special-delivery-postmark-templates), and we're ecstatic to provide it as Open Source to the .net community.

#### How to use Mustachio:

```csharp
// Parse the template:
var sourceTemplate = "Dear {{name}}, this is definitely a personalized note to you. Very truly yours, {{sender}}"
var template = Mustachio.Parser.Parse(sourceTemplate);

// Create the values for the template model:
dynamic model = new ExpandoObject();
model.name = "John";
model.sender = "Sally";

// Combine the model with the template to get content:
var content = template(model);
```
#### Extending Mustachio with Token Expanders:

```csharp
// You can add support for Partials via Token Expanders.
// Token Expanders can be used to extend Mustachio for many other use cases, such as: Date/Time formatters, Localization, etc., allowing also custom Token Render functions.

var sourceTemplate = "Welcome to our website! {{{ @content }}} Yours Truly, John Smith.";
var stringData = "This is a partial. You can also add variables here {{ testVar }} or use other expanders. Watch out for infinite loops!";
var tokenExpander = new TokenExpander
    {
        RegEx = new Regex("{{{ @content }}}"), // you can also use Mustache syntax: {{> content }}
        ExpandTokens = (s, baseOptions) => Tokenizer.Tokenize(stringData, baseOptions) // Instead of baseOptions, you can pass a new ParsingOptions object, which has no TokenExpanders to avoid infinite loops.
    };
var parsingOptions = new ParsingOptions { TokenExpanders = new[] { tokenExpander } };
var template = Mustachio.Parser.Parse(sourceTemplate, parsingOptions);

// Create the values for the template model:
dynamic model = new ExpandoObject();
model.testVar = "Test";

// Combine the model with the template to get content:
var content = template(model);
```
#### Installing Mustachio:

Mustachio can be installed via [NuGet](https://www.nuget.org/packages/Mustachio/):

```bash
Install-Package Mustachio
```

##### Key differences between Mustachio and [Mustache](https://mustache.github.io/)

Mustachio contains a few modifications to the core Mustache language that are important.

1. `each` blocks are recommended for handling arrays of values. (We have a good reason!)
2. Complex paths are supported, for example `{{ this.is.a.valid.path }}` and `{{ ../this.goes.up.one.level }}`
3. Template partials are supported via Token Expanders.
 
###### A little more about the differences:

One awesome feature of Mustachio is that with a minor alteration in the mustache syntax, we can infer what model will be required to completely fill out a template. By using the `each` keyword when interating over an array, our parser can infer whether an array or object (or scalar) should be expected when the template is used. Normal mustache syntax would prevent us from determining this.

We think the model inference feature is compelling, because it allows for error detection, and faster debugging iterations when developing templates, which justifies this minor change to 'vanilla' mustache syntax.
