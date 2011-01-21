using System;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// This exception is thrown when there are problems with registering, unregistering or updating
    /// applications using Application Restart Recovery.
    /// </summary>
    [Serializable]
    public class ApplicationRecoveryException : ExternalException
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ApplicationRecoveryException() { }

        /// <summary>
        /// Initializes an exception with a custom message.
        /// </summary>
        /// <param name="message">A custom message for the exception.</param>
        public ApplicationRecoveryException(string message) : base(message) { }

        /// <summary>
        /// Initializes an exception with custom message and inner exception.
        /// </summary>
        /// <param name="message">A custom message for the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public ApplicationRecoveryException(string message, Exception innerException)
            : base(message, innerException)
        {
            // Empty
        }

        /// <summary>
        /// Initializes an exception with custom message and error code.
        /// </summary>
        /// <param name="message">A custom message for the exception.</param>
        /// <param name="errorCode">An error code (hresult) from which to generate the exception.</param>
        public ApplicationRecoveryException(string message, int errorCode) : base(message, errorCode) { }

        /// <summary>
        /// Initializes an exception from serialization info and a context.
        /// </summary>
        /// <param name="info">Serialization info from which to create exception.</param>
        /// <param name="context">Streaming context from which to create exception.</param>
        protected ApplicationRecoveryException(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            // Empty
        }
            
    }
}
