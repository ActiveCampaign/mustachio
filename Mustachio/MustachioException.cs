using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mustachio
{
	/// <summary>
	/// The General Exception type for Framework Exceptions
	/// </summary>
    public class MustachioException : Exception
    {
		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="message"></param>
		/// <param name="replacements"></param>
        public MustachioException(string message, params object[] replacements) : base(String.Format(message, replacements)) { }
    }
}
