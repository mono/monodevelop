//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Threading;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// This class keeps track of the current state of each type of event.  
    /// The MessageManager class tracks event handlers.  
    /// This class only deals with each event type (i.e.
    /// BatteryLifePercentChanged) as a whole.
    /// </summary>
    internal static class EventManager
    {
        // Prevents reading from PowerManager members while they are still null.
        // MessageManager notifies the PowerManager that the member 
        // has been set and can be used.        
        internal static AutoResetEvent monitorOnReset = new AutoResetEvent(false);

        #region Hardcoded GUIDS for each event

        internal static readonly Guid PowerPersonalityChange = new Guid(0x245d8541, 0x3943, 0x4422, 0xb0, 0x25, 0x13, 0xA7, 0x84, 0xF6, 0x79, 0xB7);
        internal static readonly Guid PowerSourceChange = new Guid(0x5d3e9a59, 0xe9D5, 0x4b00, 0xa6, 0xbd, 0xff, 0x34, 0xff, 0x51, 0x65, 0x48);
        internal static readonly Guid BatteryCapacityChange = new Guid(0xa7ad8041, 0xb45a, 0x4cae, 0x87, 0xa3, 0xee, 0xcb, 0xb4, 0x68, 0xa9, 0xe1);
        internal static readonly Guid BackgroundTaskNotification = new Guid(0x515c31d8, 0xf734, 0x163d, 0xa0, 0xfd, 0x11, 0xa0, 0x8c, 0x91, 0xe8, 0xf1);
        internal static readonly Guid MonitorPowerStatus = new Guid(0x02731015, 0x4510, 0x4526, 0x99, 0xe6, 0xe5, 0xa1, 0x7e, 0xbd, 0x1a, 0xea);

        #endregion

        #region private static members

        // Used to catch the initial message Windows sends when 
        // you first register for a power notification.
        // We do not want to fire any event handlers when this happens.
        private static bool personalityCaught;
        private static bool powerSrcCaught;
        private static bool batteryLifeCaught;
        private static bool monitorOnCaught;

        #endregion

        /// <summary>
        /// Determines if a message should be caught, preventing
        /// the event handler from executing. 
        /// This is needed when an event is initially registered.
        /// </summary>
        /// <param name="eventGuid">The event to check.</param>
        /// <returns>A boolean value. Returns true if the 
        /// message should be caught.</returns>
        internal static bool IsMessageCaught(Guid eventGuid)
        {
            bool isMessageCaught = false;

            if (eventGuid == EventManager.BatteryCapacityChange)
            {
                if (!batteryLifeCaught)
                {
                    batteryLifeCaught = true;
                    isMessageCaught = true;
                }
            }
            else if (eventGuid == EventManager.MonitorPowerStatus)
            {
                if (!monitorOnCaught)
                {
                    monitorOnCaught = true;
                    isMessageCaught = true;
                }
            }
            else if (eventGuid == EventManager.PowerPersonalityChange)
            {
                if (!personalityCaught)
                {
                    personalityCaught = true;
                    isMessageCaught = true;
                }
            }
            else if (eventGuid == EventManager.PowerSourceChange)
            {
                if (!powerSrcCaught)
                {
                    powerSrcCaught = true;
                    isMessageCaught = true;
                }
            }

            return isMessageCaught;
        }
    }
}
