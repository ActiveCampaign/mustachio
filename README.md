<img src="http://assets.wildbit.com/postmark/misc/mustachio-logo@2x.png" alt="Mustachio Logo" title="Pistachio + Mustache =&gt; Mustachio" width="148" height="149">

# Mustachio
A Lightweight, powerful, flavorful, templating engine for C# and other .net-based languages.

#### What's this for?

*Mustachio* allows you to create simple text-based templates that are fast and safe to render. It's the heart of [Postmark Templates](http://blog.postmarkapp.com/post/125849089273/special-delivery-postmark-templates), and we're ecstatic to provide it as Open Source to the .net community.

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

#### Installing Mustachio:

Mustachio can be installed via [NuGet](https://www.nuget.org/packages/Mustachio/):

```bash
Install-Package Mustachio
```

##### Key differences between Mustachio and [Mustache](https://mustache.github.io/)

Mustachio contains a few modifications to the core Mustache language that are important.

1. `each` blocks are recommended for handling arrays of values. (We have a good reason!)
2. Complex paths are supported, for example `{{ this.is.a.valid.path }}` and `{{ ../this.goes.up.one.level }}`
3. Template partials (`{{> secondary_template }}`) are not supported. (We have a good reason!)
 
###### A little more about the differences:

One awesome feature of Mustachio is that with a minor alteration in the mustache syntax, we can infer what model will be required to completely fill out a template. By using the `each` keyword when interating over an array, our parser can infer whether an array or object (or scalar) should be expected when the template is used. Normal mustache syntax would prevent us from determining this.

We think the model inference feature is compelling, because it allows for error detection, and faster debugging iterations when developing templates, which justifies this minor change to 'vanilla' mustache syntax.

**Template partials** are a great feature for large scale template development. However, they introduce the risk of _infinite recursion_ if used improperly (especially since Mustachio allows for one to navigate 'up' a model with `../`).

In our use case (email templating), including partials would complicate the general process of creating the templates, and allow unknown users to create potentially unbound processing requirements on our servers. It is possible to detect these cycles while parsing templates, so, if this is important to our customers, or the broader OSS community, partial template support may be added to Mustachio in the future.


###### Infos about new features
 
Its now possible to add plain objects to the Dictionary.They will be called by reflection. Also you can now spezify the excact Size of the template to limit it (this could be come handy if you are in a hostet env). Also there is a new Operator {{?}}. Use it the access the current value direct. This will invoke ToString on the object in the current scope. Its good for cases where you are looping through a collection of primitives like:
 
{{#each Data.ArrayOfInts}}
Current int: {{?}}
{{/each}}
 
###### Formatter
Use the ContextObject.PrintableTypes collection to create own formatter for your types or add one to the new ParserOptions object for just one call. To invoke them in your template use the new Function syntax:
{{Just.One.Formattable(AnyStringFormat).Thing}}

The formatter CAN return a new object on wich you can call new Propertys or it can return a string.
There are formatter prepaired for all Primitve types. That means per default you can call on an object hat contains a DateTime:

{{MyObject.DateTime(D)}}


