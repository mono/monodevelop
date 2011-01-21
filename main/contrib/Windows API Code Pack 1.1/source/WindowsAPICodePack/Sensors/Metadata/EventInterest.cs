// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Contains a list of well known event interest types. This class will be removed once wrappers are developed.
    /// </summary>
    public static class EventInterestTypes
    {
        /// <summary>
        /// Register for asynchronous sensor data updates. This has power management implications.
        /// </summary>
        public static readonly Guid DataUpdated = new Guid(0x2ED0F2A4, 0x0087, 0x41D3, 0x87, 0xDB, 0x67, 0x73, 0x37, 0x0B, 0x3C, 0x88);
        
        /// <summary>
        /// Register for sensor state change notifications.
        /// </summary>
        public static readonly Guid StateChanged = new Guid(0xBFD96016, 0x6BD7, 0x4560, 0xAD, 0x34, 0xF2, 0xF6, 0x60, 0x7E, 0x8F, 0x81);
    }
}
