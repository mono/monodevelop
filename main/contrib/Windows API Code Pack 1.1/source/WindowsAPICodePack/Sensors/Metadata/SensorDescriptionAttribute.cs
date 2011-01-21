// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// An attribute which is applied on <see cref="Sensor"/>-derived types. Provides essential metadata
    /// such as the GUID of the sensor type for which this wrapper was written.
    /// </summary>    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SensorDescriptionAttribute : Attribute
    {
        private Guid _sensorType;

        /// <summary>
        /// Constructs the attribue with a string represening the sensor type GUID and the type of the data report class.
        /// </summary>
        /// <param name="sensorType">String representing the sensor type GUID.</param>
        public SensorDescriptionAttribute(string sensorType)
        {
            // will throw if invalid format
            _sensorType = new Guid(sensorType);
        }

        /// <summary>
        /// Gets a string representing the sensor type GUID.
        /// </summary>
        public string SensorType { get { return _sensorType.ToString(); } }

        /// <summary>
        /// Gets the GUID of the sensor type.
        /// </summary>
        public Guid SensorTypeGuid
        {
            get { return _sensorType; }
        }
    }
}