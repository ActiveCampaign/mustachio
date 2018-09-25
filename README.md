# Morestachio
A Lightweight, powerful, flavorful, templating engine for C# and other .net-based languages. Its a fork of Mustachio.

#### What's this for?

*Morestachio* allows you to create simple text-based templates that are fast and safe to render. It is optimized for WebServers and high on customersation with its Formatter syntax.

#### How to use Morestachio:

{% if page.heading == "x" %}
```csharp
// Parse the template:
var sourceTemplate = "Dear {{name}}, this is definitely a personalized note to you. Very truly yours, {{sender}}"
var template = Morestachio.Parser.ParseWithOptions(new Morstachio.ParserOptions(sourceTemplate));

// Create the values for the template model:
dynamic model = new ExpandoObject();
model.name = "John";
model.sender = "Sally";
// or with dictionarys
IDictionary model = new Dictionary<string, object>();
model["name"] = "John";
model["sender"] = "Sally";
//or with any other object
var model = new {name= "John", sender= "Sally"}

// Combine the model with the template to get content:
Stream content = template.Create(model);
content.Stringify(Encoding.Default); // Dear John, this is definitely a personalized note to you. Very truly yours, Sally
```

{% endif %}

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

Including partials would complicate the general process of creating the templates, and allow unknown users to create potentially unbound processing requirements on our servers. It is possible to detect these cycles while parsing templates, so, if this is important to the broader OSS community, partial template support may be added to Morestachio in the future.


###### Infos about new features
 
Its possible to use plain C# objects they will be called by reflection. 
Also you can now spezify the excact Size of the template to limit it (this could be come handy if you are in a hostet env) use the `ParserOptions.MaxSize` option to define a max size. It will be enforced on exact that amount of bytes in the stream.

##### Streams
One mayor component is the usage of Streams in morestachio. You can declare a Factory for the streams generated in the `ParserOptions.SourceFactory`. This is very important if you are rendering templates that will be very huge and you want to stream them directly to the harddrive or elsewhere. This has also a very positive effect on the performance as we will not use string concatination for compiling the template. If you do not set the `ParserOptions.SourceFactory` and the `ParserOptions.Encoding`, a memory stream will be created and the `Encoding.Default` will be used.
 
###### Formatter
Use the `ContextObject.PrintableTypes` collection to create own formatter for all your types or add one to the new `ParserOptions.Formatters` object for just one call. To invoke them in your template use the new Function syntax:
```csharp
{{Just.One.Formattable(AnyString).Thing}}
```

The formatter CAN return a new object on wich you can call new Propertys or it can return a string.
There are formatter prepaired for all Primitve types. That means per default you can call on an object hat contains a DateTime:
```csharp
{{MyObject.DateTime(D)}}
```
that will call the `IFormattable` interface on the DateTime. 

**Formatter References** can be used to reference another property/key in the template and then use it in a Formatter.
```csharp
{{MyObject.Value($Key$)}}
```
This will call a formatter that is resposible for the type that `Value` has and will give it whats in `Key`. Example:
```csharp
//create the template
var template = "{{Value($Key$)}}";
//create the model
var model = new Dictionary<string, object>();
model["Value"] = DateTime.Now; 
model["Key"] = "D";
//now add a formatter for our DateTime and add it to the ParserOptions

var parserOptions = new ParserOptions(template);
//                         Value   | Argument| Return
parserOptions.AddFormatter<DateTime, string,   string>((value, argument) => {
  //value will be the DateTime object and argument will be the value from Key
  return value.ToString(argument);
});

Parser.CreateAndStringify(parserOptions); // Friday, September 21, 2018 ish

```


