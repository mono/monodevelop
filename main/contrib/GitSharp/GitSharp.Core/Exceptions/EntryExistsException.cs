using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace GitSharp.Core.Exceptions
{
	[Serializable]
    public class EntryExistsException : Exception
    {
        public EntryExistsException(string name)
            : base(string.Format("Tree entry \"{0}\" already exists.", name))
        {
        }
		
        public EntryExistsException(string name, Exception inner)
            : base(string.Format("Tree entry \"{0}\" already exists.", name),inner)
        {
        }
		
		protected EntryExistsException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}

    }
}
