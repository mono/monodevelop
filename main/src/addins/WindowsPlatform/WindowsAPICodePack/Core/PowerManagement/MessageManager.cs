//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// This class generates .NET events based on Windows messages.  
    /// The PowerRegWindow class processes the messages from Windows.
    /// </summary>
    internal static class MessageManager
    {
        private static object lockObject = new object();
        private static PowerRegWindow window;

        #region Internal static methods

        /// <summary>
        /// Registers a callback for a power event.
        /// </summary>
        /// <param name="eventId">Guid for the event.</param>
        /// <param name="eventToRegister">Event handler for the specified event.</param>
        internal static void RegisterPowerEvent(Guid eventId, EventHandler eventToRegister)
        {
            EnsureInitialized();
            window.RegisterPowerEvent(eventId, eventToRegister);
        }

        /// <summary>
        /// Unregisters an event handler for a power event.
        /// </summary>
        /// <param name="eventId">Guid for the event.</param>
        /// <param name="eventToUnregister">Event handler to unregister.</param>
        internal static void UnregisterPowerEvent(Guid eventId, EventHandler eventToUnregister)
        {
            EnsureInitialized();
            window.UnregisterPowerEvent(eventId, eventToUnregister);
        }

        #endregion

        /// <summary>
        /// Ensures that the hidden window is initialized and 
        /// listening for messages.
        /// </summary>
        private static void EnsureInitialized()
        {
            lock (lockObject)
            {
                if (window == null)
                {
                    // Create a new hidden window to listen
                    // for power management related window messages.
                    window = new PowerRegWindow();
                }
            }
        }

        /// <summary>
        /// Catch Windows messages and generates events for power specific
        /// messages.
        /// </summary>
        internal class PowerRegWindow : Form
        {
            private Hashtable eventList = new Hashtable();
            private ReaderWriterLock readerWriterLock = new ReaderWriterLock();

            internal PowerRegWindow()
                : base()
            {

            }

            #region Internal Methods

            /// <summary>
            /// Adds an event handler to call when Windows sends 
            /// a message for an event.
            /// </summary>
            /// <param name="eventId">Guid for the event.</param>
            /// <param name="eventToRegister">Event handler for the event.</param>
            internal void RegisterPowerEvent(Guid eventId, EventHandler eventToRegister)
            {
                readerWriterLock.AcquireWriterLock(Timeout.Infinite);
                if (!eventList.Contains(eventId))
                {
                    Power.RegisterPowerSettingNotification(this.Handle, eventId);
                    ArrayList newList = new ArrayList();
                    newList.Add(eventToRegister);
                    eventList.Add(eventId, newList);
                }
                else
                {
                    ArrayList currList = (ArrayList)eventList[eventId];
                    currList.Add(eventToRegister);
                }
                readerWriterLock.ReleaseWriterLock();
            }

            /// <summary>
            /// Removes an event handler.
            /// </summary>
            /// <param name="eventId">Guid for the event.</param>
            /// <param name="eventToUnregister">Event handler to remove.</param>
            /// <exception cref="InvalidOperationException">Cannot unregister 
            /// a function that is not registered.</exception>
            internal void UnregisterPowerEvent(Guid eventId, EventHandler eventToUnregister)
            {
                readerWriterLock.AcquireWriterLock(Timeout.Infinite);
                if (eventList.Contains(eventId))
                {
                    ArrayList currList = (ArrayList)eventList[eventId];
                    currList.Remove(eventToUnregister);
                }
                else
                {
                    throw new InvalidOperationException(LocalizedMessages.MessageManagerHandlerNotRegistered);
                }
                readerWriterLock.ReleaseWriterLock();
            }

            #endregion

            /// <summary>
            /// Executes any registered event handlers.
            /// </summary>
            /// <param name="eventHandlerList">ArrayList of event handlers.</param>            
            private static void ExecuteEvents(ArrayList eventHandlerList)
            {
                foreach (EventHandler handler in eventHandlerList)
                {
                    handler.Invoke(null, new EventArgs());
                }
            }

            /// <summary>
            /// This method is called when a Windows message 
            /// is sent to this window.
            /// The method calls the registered event handlers.
            /// </summary>
            protected override void WndProc(ref Message m)
            {
                // Make sure it is a Power Management message.
                if (m.Msg == PowerManagementNativeMethods.PowerBroadcastMessage && 
                    (int)m.WParam == PowerManagementNativeMethods.PowerSettingChangeMessage)
                {
                    PowerManagementNativeMethods.PowerBroadcastSetting ps =
                         (PowerManagementNativeMethods.PowerBroadcastSetting)Marshal.PtrToStructure(
                             m.LParam, typeof(PowerManagementNativeMethods.PowerBroadcastSetting));

                    IntPtr pData = new IntPtr(m.LParam.ToInt64() + Marshal.SizeOf(ps));
                    Guid currentEvent = ps.PowerSetting;
                                        
                    // IsMonitorOn
                    if (ps.PowerSetting == EventManager.MonitorPowerStatus &&
                        ps.DataLength == Marshal.SizeOf(typeof(Int32)))
                    {
                        Int32 monitorStatus = (Int32)Marshal.PtrToStructure(pData, typeof(Int32));
                        PowerManager.IsMonitorOn = monitorStatus != 0;
                        EventManager.monitorOnReset.Set();
                    }

                    if (!EventManager.IsMessageCaught(currentEvent))
                    {
                        ExecuteEvents((ArrayList)eventList[currentEvent]);
                    }
                }
                else
                    base.WndProc(ref m);

            }

        }
    }
}
