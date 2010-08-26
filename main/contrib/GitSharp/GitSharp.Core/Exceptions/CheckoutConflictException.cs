using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace GitSharp.Core.Exceptions
{
    [Serializable]
    public class CheckoutConflictException : Exception
    {
        /// <summary>
        /// Construct a <see cref="CheckoutConflictException"/> for the specified file
        /// </summary>
        public CheckoutConflictException(string file)
            : base("Checkout conflict with file: " + file)
        {
        }
        /// <summary>
        /// Construct a <see cref="CheckoutConflictException"/> for the specified file
        /// </summary>
        public CheckoutConflictException(string file, Exception inner)
            : base("Checkout conflict with file: " + file, inner)
        {
        }

        /// <summary>
        /// Construct a <see cref="CheckoutConflictException"/> for the specified file
        /// </summary>
        /// <param name="files"></param>
        public CheckoutConflictException(IEnumerable<string> files)
            : base("Checkout conflict with files: " + BuildList(files))
        {
        }

        private static string BuildList(IEnumerable<string> files)
        {
            var builder = new StringBuilder();
            foreach (string file in files)
            {
                builder.Append(Environment.NewLine);
                builder.Append(file);
            }

            return builder.ToString();
        }
		
		protected CheckoutConflictException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}
    }
}