using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mustachio
{
    /// <summary>
    /// Records elements used in a model, and allowing a 
    /// simple JSON model to be produced for testing.
    /// </summary>
    public class InferredTemplateModel
    {
        private static readonly Regex _pathFinder = new Regex("(\\.\\.[\\\\/]{1})|([^.]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public InferredTemplateModel()
        {
            this.Children = new Dictionary<string, InferredTemplateModel>();
            this.Usages = new HashSet<UsedAs>();
        }

        public InferredTemplateModel GetInferredModelForPath(string path, UsedAs accessType)
        {
            var retval = this.GetContextForPath(path);
            retval.Usages.Add(accessType);
            return retval;
        }

        public InferredTemplateModel Parent { get; private set; }

        public HashSet<UsedAs> Usages { get; private set; }

        private IDictionary<string, InferredTemplateModel> Children { get; set; }

        public string Key { get; private set; }

        private InferredTemplateModel GetContextForPath(Queue<String> elements)
        {
            var retval = this;
            if (elements.Any())
            {
                var element = elements.Dequeue();
                if (element.StartsWith(".."))
                {
                    if (Parent != null)
                    {
                        retval = Parent.GetContextForPath(elements);
                    }
                    else
                    {
                        //Calling "../" too much may be "ok" in that if we're at root,
                        //we may just stop recursion and traverse down the path.
                        retval = GetContextForPath(elements);
                    }
                }
                //TODO: handle array accessors and maybe "special" keys.
                else
                {
                    //ALWAYS return the context, even if the value is null.

                    InferredTemplateModel innerContext = null;
                    if (!this.Children.TryGetValue(element, out innerContext))
                    {
                        innerContext = new InferredTemplateModel();
                        innerContext.Key = element;
                        innerContext.Parent = this;
                        this.Children[element] = innerContext;
                    }
                    retval = innerContext.GetContextForPath(elements);
                }
            }
            return retval;
        }

        private InferredTemplateModel GetContextForPath(string path)
        {
            var elements = new Queue<string>();
            foreach (Match m in _pathFinder.Matches(path))
            {
                elements.Enqueue(m.Value);
            }
            return GetContextForPath(elements);
        }

        /// <summary>
        /// Allows us to capture how each path is used in a template.
        /// </summary>
        public enum UsedAs
        {
            Scalar,
            ConditionalValue,
            Collection
        }

        private object RepresentedContext()
        {
            object retval = null;
            if (!Usages.Any())
            {
                retval = Children.ToDictionary(k => k.Key, v => v.Value.RepresentedContext());
            }
            else if (Usages.Contains(UsedAs.Scalar) && this.Usages.Count == 1)
            {
                retval = this.Key + "_Value";
            }
            else
            {
                if (this.Usages.Contains(UsedAs.Collection))
                {
                    if (Children.Any())
                    {
                        retval = new[] { Children.ToDictionary(k => k.Key, v => v.Value.RepresentedContext()) };
                    }
                    else
                    {
                        retval = Enumerable.Range(1, 3).Select(k => Key + "_" + k).ToArray();
                    }
                }
                else
                {
                    retval = Children.ToDictionary(k => k.Key, v => v.Value.RepresentedContext());
                }
            }
            return retval;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(RepresentedContext());
        }
    }
}
