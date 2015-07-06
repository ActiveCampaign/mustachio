# mustachio
Lightweight, powerful, nutritious template engine.


#### How to use mustachio:

```csharp
//Parse the template:
var sourceTemplate = "Dear {{name}}, this is definitely a personalized note to you. Very truly yours, {{sender}}"
var template = Mustachio.Parser.Parse(sourceTemplate);

//Render the template:
dynamic model = new ExpandoObject();
model.name = "John";
model.sender = "Sally";

// Combine the model with the template:
var content = template(model);
```
