// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// The state of the sensor
    /// </summary>
    public enum SensorState
    {
        /// <summary>
        /// The device has been removed.
        /// </summary>
        Removed = -1,
        
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
        NoData = 2,
        
        /// <summary>
        /// The device is initializing.
        /// </summary>
        Initializing = 3,
       
        /// <summary>
        /// No permissions exist to access the device.
        /// </summary>
        AccessDenied = 4,
        
        /// <summary>
        /// The device has encountered an error.
        /// </summary>
        Error = 5
    }


}
