<img src="http://assets.wildbit.com/postmark/misc/Morestachio-logo@2x.png" alt="Morestachio Logo" title="Pistachio + Mustache =&gt; Morestachio" width="148" height="149">

# Morestachio
A Lightweight, powerful, flavorful, templating engine for C# and other .net-based languages.

#### What's this for?

*Morestachio* allows you to create simple text-based templates that are fast and safe to render. It's the heart of [Postmark Templates](http://blog.postmarkapp.com/post/125849089273/special-delivery-postmark-templates), and we're ecstatic to provide it as Open Source to the .net community.

#### How to use Morestachio:

```csharp
// Parse the template:
var sourceTemplate = "Dear {{name}}, this is definitely a personalized note to you. Very truly yours, {{sender}}"
var template = Morestachio.Parser.Parse(sourceTemplate);

// Create the values for the template model:
dynamic model = new ExpandoObject();
model.name = "John";
model.sender = "Sally";

// Combine the model with the template to get content:
var content = template(model);
```

#### Installing Morestachio:

Morestachio can be installed via [NuGet](https://www.nuget.org/packages/Morestachio/):

```bash
Install-Package Morestachio
```

##### Key differences between Morestachio and [mustachio](https://github.com/wildbit/mustachio)

Morestachio is build upon Mustachio and extends the mustachio syntax in a few ways.

1. each value can be formatted by adding formatter the the morestachio
2. Templates will be parsed as streams and will create a new stream. This is better when creating larger templates and best for web as you can also limit the length of the "to be" created template to a certain size
3. Morestachio accepts any object besides the Dictionary<string,object> from mustachio.

##### Key differences between Morestachio and [Mustache](https://mustache.github.io/)

Morestachio contains a few modifications to the core Mustache language that are important.

1. `each` blocks are recommended for handling arrays of values. (We have a good reason!)
2. Complex paths are supported, for example `{{ this.is.a.valid.path }}` and `{{ ../this.goes.up.one.level }}`
3. Template partials (`{{> secondary_template }}`) are not supported. (We have a good reason!)
 
###### A little more about the differences:

One awesome feature of Morestachio is that with a minor alteration in the mustache syntax, we can infer what model will be required to completely fill out a template. By using the `each` keyword when interating over an array, our parser can infer whether an array or object (or scalar) should be expected when the template is used. Normal mustache syntax would prevent us from determining this.

We think the model inference feature is compelling, because it allows for error detection, and faster debugging iterations when developing templates, which justifies this minor change to 'vanilla' mustache syntax.

**Template partials** are a great feature for large scale template development. However, they introduce the risk of _infinite recursion_ if used improperly (especially since Morestachio allows for one to navigate 'up' a model with `../`).

In our use case (email templating), including partials would complicate the general process of creating the templates, and allow unknown users to create potentially unbound processing requirements on our servers. It is possible to detect these cycles while parsing templates, so, if this is important to our customers, or the broader OSS community, partial template support may be added to Morestachio in the future.


###### Infos about new features
 
Its now possible to add plain objects to the Dictionary.
They will be called by reflection. 
Also you can now spezify the excact Size of the template to limit it (this could be come handy if you are in a hostet env). Also there is a new Operator {{?}}. Use it the access the current value direct. This will invoke ToString on the object in the current scope. Its good for cases where you are looping through a collection of primitives like:
 
{{#each Data.ArrayOfInts}}
Current int: {{?}}
{{/each}}
 
###### Formatter
Use the ContextObject.PrintableTypes collection to create own formatter for your types or add one to the new ParserOptions object for just one call. To invoke them in your template use the new Function syntax:
{{Just.One.Formattable(AnyStringFormat).Thing}}

The formatter CAN return a new object on wich you can call new Propertys or it can return a string.
There are formatter prepaired for all Primitve types. That means per default you can call on an object hat contains a DateTime:

{{MyObject.DateTime(D)}}


