// Copyright (c) Microsoft Corporation.  All rights reserved.


namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Represents the state of the sensor device.
    /// </summary>
    public enum NativeSensorState
    {
        /// <summary>
        /// The device is ready.
        /// </summary>
        Ready = 0,
        /// <summary>
        /// The device is not available.
        /// </summary>
        NotAvailable = 1,
        /// <summary>
        /// No data is available.
        /// </summary>
        NoData,
        /// <summary>
        /// The device is initializing.
        /// </summary>
        Initializing,
        /// <summary>
        /// No permissions exist to access the device.
        /// </summary>
        AccessDenied,
        /// <summary>
        /// The device has encountered an error.
        /// </summary>
        Error
    }

}
