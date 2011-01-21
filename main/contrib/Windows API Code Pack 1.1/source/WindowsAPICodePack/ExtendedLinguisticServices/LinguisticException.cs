// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Runtime.ConstrainedExecution;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// This exception is thrown by the managed wrappers of ELS when the underlying
    /// unmanaged implementation returns an HResult which is not S_OK (0).
    /// This exception is also thrown when the managed wrapper detects an exceptional
    /// condition which causes it to fail. Please note that other .NET exceptions are also
    /// possible to be thrown from the ELS managed wrappers.
    /// </summary>
    [Serializable]
    public class LinguisticException : Win32Exception
    {
        // Common HResult values.
        internal const uint InvalidArgs = 0x80070057;
        internal const uint Fail = 0x80004005;
        internal const uint InvalidData = 0x8007000D;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinguisticException">LinguisticException</see> class
        /// with the last Win32 error that occurred.
        /// </summary>
        public LinguisticException() { }

        internal LinguisticException(UInt32 hResult)
            : base((int)hResult)
        {
            HResult = (int)hResult;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinguisticException">LinguisticException</see> class
        ///  with the specified context and the serialization information.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo">SerializationInfo</see> associated with this exception.</param>
        /// <param name="context">A <see cref="System.Runtime.Serialization.StreamingContext">StreamingContext</see> that represents the context of this exception.</param>
        protected LinguisticException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinguisticException">LinguisticException</see> 
        /// class with the specified detailed description.
        /// </summary>
        /// <param name="message">A detailed description of the error.</param>
        public LinguisticException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinguisticException">LinguisticException</see> class
        /// with the specified detailed description and the specified exception.
        /// </summary>
        /// <param name="message">A detailed description of the error.</param>
        /// <param name="innerException">A reference to the inner exception that is the cause of this exception.</param>
        public LinguisticException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets the MappingResultState describing the error condition for this exception.
        /// </summary>
        public MappingResultState ResultState
        {
            get
            {
                return new MappingResultState(HResult, Message);
            }
        }
    }

}
