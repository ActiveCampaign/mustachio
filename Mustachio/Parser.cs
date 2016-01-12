using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Mustachio
{
    /// <summary>
    /// The main entry point for this library. Use the static "Parse" methods to create template functions.
    /// Functions are safe for reuse, so you may parse and cache the resulting function.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Parse the template content, producing a function that can be used to apply variables to the template. 
        /// The provided function can be reused (i.e. no state will "leak" from one application of the function to the next).
        /// </summary>
        /// <param name="template">The content of the template to be parsed.</param>
        /// <returns></returns>
        public static Func<IDictionary<String, object>, String> Parse(string template)
        {
            var tokens = new Queue<TokenPair>(Tokenizer.Tokenize(template));
            var internalTemplate = Parse(tokens);
            return (model) =>
            {
                var retval = new StringBuilder();
                var context = new ContextObject()
                {
                    Value = model,
                    Key = ""
                };
                internalTemplate(retval, context);
                return retval.ToString();
            };
        }

        /// <summary>
        /// Parse the template, and capture paths used in the template to determine a suitable structure for the required model.
        /// </summary>
        /// <param name="templateSource"></param>
        /// <returns></returns>
        public static ExtendedParseInformation ParseWithModelInference(string templateSource)
        {
            var tokens = new Queue<TokenPair>(Tokenizer.Tokenize(templateSource));
            var inferredModel = new InferredTemplateModel();

            var internalTemplate = Parse(tokens, inferredModel);
            Func<IDictionary<String, object>, String> template = (model) =>
            {
                var retval = new StringBuilder();
                var context = new ContextObject()
                {
                    Value = model,
                    Key = ""
                };
                internalTemplate(retval, context);
                return retval.ToString();
            };

            var result = new ExtendedParseInformation()
            {
                InferredModel = inferredModel,
                ParsedTemplate = template
            };

            return result;
        }

        private static Action<StringBuilder, ContextObject> Parse(Queue<TokenPair> tokens, InferredTemplateModel currentScope = null)
        {
            var buildArray = new List<Action<StringBuilder, ContextObject>>();

            while (tokens.Any())
            {
                var currentToken = tokens.Dequeue();
                switch (currentToken.Type)
                {
                    case TokenType.Comment:
                        break;
                    case TokenType.Content:
                        buildArray.Add(HandleContent(currentToken.Value));
                        break;
                    case TokenType.CollectionOpen:
                        buildArray.Add(HandleCollectionOpen(currentToken, tokens, currentScope));
                        break;
                    case TokenType.ElementOpen:
                        buildArray.Add(HandleElementOpen(currentToken, tokens, currentScope));
                        break;
                    case TokenType.InvertedElementOpen:
                        buildArray.Add(HandleInvertedElementOpen(currentToken, tokens, currentScope));
                        break;
                    case TokenType.CollectionClose:
                    case TokenType.ElementClose:
                        // This should immediately return if we're in the element scope, 
                        // and if we're not, this should have been detected by the tokenizer!
                        return (builder, context) =>
                        {
                            foreach (var a in buildArray)
                            {
                                a(builder, context);
                            }
                        };
                    case TokenType.EscapedSingleValue:
                    case TokenType.UnescapedSingleValue:
                        buildArray.Add(HandleSingleValue(currentToken, currentScope));
                        break;
                }
            }

            return (builder, context) =>
            {
                foreach (var a in buildArray)
                {
                    a(builder, context);
                }
            };
        }

        private static string HtmlEncodeString(string context)
        {
            return HttpUtility.HtmlEncode(context);
        }

        private static Action<StringBuilder, ContextObject> HandleSingleValue(TokenPair token, InferredTemplateModel scope)
        {

            if (scope != null)
            {
                scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Scalar);
            }

            return (builder, context) =>
            {
                if (context != null)
                {
                    //try to locate the value in the context, if it exists, append it.
                    var c = context.GetContextForPath(token.Value);
                    if (c.Value != null)
                    {
                        if (token.Type == TokenType.EscapedSingleValue)
                        {
                            builder.Append(HtmlEncodeString(c.ToString()));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                    }
                }
            };
        }

        private static Action<StringBuilder, ContextObject> HandleContent(string token)
        {
            return (builder, context) => builder.Append(token);
        }

        private static Action<StringBuilder, ContextObject> HandleInvertedElementOpen(TokenPair token, Queue<TokenPair> remainder, InferredTemplateModel scope)
        {
            if (scope != null)
            {
                scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);
            }

            var innerTemplate = Parse(remainder, scope);

            return (builder, context) =>
            {
                var c = context.GetContextForPath(token.Value);
                //"falsey" values by Javascript standards...
                if (!c.Exists())
                {
                    innerTemplate(builder, c);
                }
            };
        }

        private static Action<StringBuilder, ContextObject> HandleCollectionOpen(TokenPair token, Queue<TokenPair> remainder, InferredTemplateModel scope)
        {
            if (scope != null)
            {
                scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.Collection);
            }

            var innerTemplate = Parse(remainder, scope);

            return (builder, context) =>
            {
                //if we're in the same scope, just negating, then we want to use the same object
                var c = context.GetContextForPath(token.Value);

                //"falsey" values by Javascript standards...
                if (!c.Exists()) return;

                if (c.Value is IEnumerable && !(c.Value is String) && !(c.Value is IDictionary<string, object>))
                {
                    var index = 0;
                    foreach (object i in c.Value as IEnumerable)
                    {
                        var innerContext = new ContextObject()
                        {
                            Value = i,
                            Key = String.Format("[{0}]", index),
                            Parent = c
                        };
                        innerTemplate(builder, innerContext);
                        index++;
                    }
                }
                else
                {
                    throw new IndexedParseException("'{0}' is used like an array by the template, but is a scalar value or object in your model.", token.Value);
                }
            };
        }

        private static Action<StringBuilder, ContextObject> HandleElementOpen(TokenPair token, Queue<TokenPair> remainder, InferredTemplateModel scope)
        {
            if (scope != null)
            {
                scope = scope.GetInferredModelForPath(token.Value, InferredTemplateModel.UsedAs.ConditionalValue);
            }

            var innerTemplate = Parse(remainder, scope);

            return (builder, context) =>
            {
                var c = context.GetContextForPath(token.Value);
                //"falsey" values by Javascript standards...
                if (c.Exists())
                {
                    innerTemplate(builder, c);
                }
            };
        }
    }

}

