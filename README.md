# Mustachio
A Lightweight, powerful, flavorful, templating engine.


#### How to use mustachio:

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





