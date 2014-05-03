using ICSharpCode.NRefactory.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoDevelop.JavaScript.Factories
{
    public static class ErrorFactory
    {
        public static Error CreateError(string message)
        {
            return new Error(ErrorType.Unknown, message);
        }

        public static Error CreateError(string message, int line)
        {
            return new Error(ErrorType.Error, message, line, 0);
        }
    }
}
