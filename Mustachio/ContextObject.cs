using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mustachio
{
    public class ContextObject
    {
        private static readonly Regex _pathFinder = new Regex("(\\.\\.[\\\\/]{1})|([^.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public ContextObject Parent { get; set; }

        public object Value { get; set; }

        public string Key { get; set; }

        private ContextObject GetContextForPath(Queue<String> elements)
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
                        //calling "../" too much may be "ok" in that if we're at root,
                        //we may just stop recursion and traverse down the path.
                        retval = GetContextForPath(elements);
                    }
                }
                //TODO: handle array accessors and maybe "special" keys.
                else
                {
                    //ALWAYS return the context, even if the value is null.
                    var innerContext = new ContextObject();
                    innerContext.Key = element;
                    innerContext.Parent = this;
                    var ctx = this.Value as IDictionary<string, object>;
                    if (ctx != null)
                    {
                        object o;
                        ctx.TryGetValue(element, out o);
                        innerContext.Value = o;
                    }
                    retval = innerContext.GetContextForPath(elements);
                }
            }
            return retval;
        }

        public ContextObject GetContextForPath(string path)
        {
            var elements = new Queue<string>();
            foreach (var m in _pathFinder.Matches(path).OfType<Match>())
            {
                elements.Enqueue(m.Value);
            }
            return GetContextForPath(elements);
        }

        /// <summary>
        /// Determines if the value of this context exists.
        /// </summary>
        /// <returns></returns>
        public bool Exists()
        {
            return Value != null &&
                Value as bool? != false &&
                Value as double? != 0 &&
                Value as int? != 0 &&
                Value as string != String.Empty &&
                // We've gotten this far, if it is an object that does NOT cast as enumberable, it exists
                // OR if it IS an enumerable and .Any() returns true, then it exists as well
                (Value as IEnumerable == null || (Value as IEnumerable).Cast<object>().Any()
                );
        }

        /// <summary>
        /// The set of allowed types that may be printed. Complex types (such as arrays and dictionaries) 
        /// should not be printed, or their printing should be specialized.
        /// </summary>
        private static HashSet<Type> _printableTypes = new HashSet<Type>
        {
            typeof(String),
            typeof(char),
            typeof(int),
            typeof(bool),
            typeof(double),
            typeof(short),
            typeof(float),
            typeof(short),
            typeof(long),
            typeof(byte),
            typeof(sbyte),
            typeof(decimal),
            typeof(int?),
            typeof(bool?),
            typeof(double?),
            typeof(short?),
            typeof(float?),
            typeof(short?),
            typeof(long?),
            typeof(byte?),
            typeof(sbyte?),
            typeof(decimal?)
        };

        public override string ToString()
        {
            var retval = "";
            if (Value != null && _printableTypes.Contains(Value.GetType()))
            {
                retval = Value.ToString();
            }
            return retval;
        }
    }
}
