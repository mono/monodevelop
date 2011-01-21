// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.Serialization;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Defines an exception specific to the sensors.
    /// </summary>
    [Serializable]
    public class SensorPlatformException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the Microsoft.WindowsAPICodePack.Sensors.SensorPlatformException class
        ///  with the specified context and the serialization information.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo associated with this exception.</param>
        /// <param name="context">A System.Runtime.Serialization.StreamingContext that represents the context of this exception.</param>
        protected SensorPlatformException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Microsoft.WindowsAPICodePack.Sensors.SensorPlatformException 
        /// class with the specified detailed description.
        /// </summary>
        /// <param name="message">A detailed description of the error.</param>
        public SensorPlatformException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Microsoft.WindowsAPICodePack.Sensors.SensorPlatformException class
        /// with the last Win32 error that occurred.
        /// </summary>
        public SensorPlatformException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Microsoft.WindowsAPICodePack.Sensors.SensorPlatformException class
        /// with the specified detailed description and the specified exception.
        /// </summary>
        /// <param name="message">A detailed description of the error.</param>
        /// <param name="innerException">A reference to the inner exception that is the cause of this exception.</param>
        public SensorPlatformException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}