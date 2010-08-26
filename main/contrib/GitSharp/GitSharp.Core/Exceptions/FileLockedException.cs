using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GitSharp.Core.Exceptions
{
    [global::System.Serializable]
    public class FileLockedException : IOException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public FileLockedException() { }
        public FileLockedException(string message) : base(message) { }
        public FileLockedException(string message, Exception inner) : base(message, inner) { }
        internal FileLockedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
