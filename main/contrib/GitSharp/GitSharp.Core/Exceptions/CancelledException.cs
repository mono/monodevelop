using System;
using System.Runtime.Serialization;

namespace GitSharp.Core.Exceptions
{
	[Serializable]
	public class CancelledException : Exception
	{
        private const long serialVersionUID = 1L;

		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public CancelledException()
		{
		}

		public CancelledException(string message) : base(message)
		{
		}

		public CancelledException(string message, Exception inner) : base(message, inner)
		{
		}

		protected CancelledException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}
	}
}
