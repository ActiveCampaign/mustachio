using System.Collections;
using System.Collections.Generic;

namespace Mustachio
{
    public class TokenizeResult
    {
        public IEnumerable<TokenTuple> Tokens { get; set; }
        public IEnumerable<IndexedParseException> Errors { get; set; }
    }
}
