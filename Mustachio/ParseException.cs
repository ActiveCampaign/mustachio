using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mustachio
{
    public class ParseException : Exception
    {
        public ParseException(string message, params object[] replacements) : base(String.Format(message, replacements)) { }
    }
}
