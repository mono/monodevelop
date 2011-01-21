//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.WindowsAPICodePack.Resources;
using MS.WindowsAPICodePack.Internal;
using System.ComponentModel;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// Enables registration for 
    /// power-related event notifications and provides access to power settings.
    /// </summary>
    public static class PowerManager
    {
        private static bool? isMonitorOn;
        private static bool monitorRequired;
        private static bool requestBlockSleep;

        private static readonly object monitoronlock = new object();


        #region Notifications

        /// <summary>
        /// Raised each time the active power scheme changes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The event handler specified for removal was not registered.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        public static event EventHandler PowerPersonalityChanged
        {
            add
            {


                MessageManager.RegisterPowerEvent(
                    EventManager.PowerPersonalityChange, value);
            }

            remove
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.UnregisterPowerEvent(
                    EventManager.PowerPersonalityChange, value);
            }
        }

        /// <summary>
        /// Raised when the power source changes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The event handler specified for removal was not registered.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        public static event EventHandler PowerSourceChanged
        {
            add
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.RegisterPowerEvent(
                    EventManager.PowerSourceChange, value);
            }

            remove
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.UnregisterPowerEvent(
                    EventManager.PowerSourceChange, value);
            }
        }

        /// <summary>
        /// Raised when the remaining battery life changes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The event handler specified for removal was not registered.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        public static event EventHandler BatteryLifePercentChanged
        {
            add
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.RegisterPowerEvent(
                    EventManager.BatteryCapacityChange, value);
            }
            remove
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.UnregisterPowerEvent(
                    EventManager.BatteryCapacityChange, value);
            }
        }

        /// <summary>
        /// Raised when the monitor status changes.
        /// </summary>
        /// <exception cref="InvalidOperationException">The event handler specified for removal was not registered.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        public static event EventHandler IsMonitorOnChanged
        {
            add
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.RegisterPowerEvent(
                    EventManager.MonitorPowerStatus, value);
            }
            remove
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.UnregisterPowerEvent(
                    EventManager.MonitorPowerStatus, value);
            }
        }

        /// <summary>
        /// Raised when the system will not be moving into an idle 
        /// state in the near future so applications should
        /// perform any tasks that 
        /// would otherwise prevent the computer from entering an idle state. 
        /// </summary>
        /// <exception cref="InvalidOperationException">The event handler specified for removal was not registered.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        public static event EventHandler SystemBusyChanged
        {
            add
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.RegisterPowerEvent(
                    EventManager.BackgroundTaskNotification, value);
            }
            remove
            {
                CoreHelpers.ThrowIfNotVista();

                MessageManager.UnregisterPowerEvent(
                    EventManager.BackgroundTaskNotification, value);
            }
        }
        #endregion

        /// <summary>
        /// Gets a snapshot of the current battery state.
        /// </summary>
        /// <returns>A <see cref="BatteryState"/> instance that represents 
        /// the state of the battery at the time this method was called.</returns>
        /// <exception cref="System.InvalidOperationException">The system does not have a battery.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Requires XP/Windows Server 2003 or higher.</exception>        
        public static BatteryState GetCurrentBatteryState()
        {
            CoreHelpers.ThrowIfNotXP();
            return new BatteryState();
        }

        #region Power System Properties

        /// <summary>
        /// Gets or sets a value that indicates whether the monitor is 
        /// set to remain active.  
        /// </summary>
        /// <exception cref="T:System.PlatformNotSupportedException">Requires XP/Windows Server 2003 or higher.</exception>
        /// <exception cref="T:System.Security.SecurityException">The caller does not have sufficient privileges to set this property.
        /// </exception>
        /// <remarks>This information is typically used by applications
        /// that display information but do not require 
        /// user interaction. For example, video playback applications.</remarks>
        /// <permission cref="T:System.Security.Permissions.SecurityPermission"> to set this property. Demand value: <see cref="F:System.Security.Permissions.SecurityAction.Demand"/>; Named Permission Sets: <b>FullTrust</b>.</permission>
        /// <value>A <see cref="System.Boolean"/> value. <b>True</b> if the monitor
        /// is required to remain on.</value>
        public static bool MonitorRequired
        {
            get
            {
                CoreHelpers.ThrowIfNotXP();
                return monitorRequired;
            }
            [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
            set
            {
                CoreHelpers.ThrowIfNotXP();

                if (value)
                {
                    PowerManager.SetThreadExecutionState(ExecutionStates.Continuous | ExecutionStates.DisplayRequired);
                }
                else
                {
                    PowerManager.SetThreadExecutionState(ExecutionStates.Continuous);
                }

                monitorRequired = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the system 
        /// is required to be in the working state.
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Requires XP/Windows Server 2003 or higher.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have sufficient privileges to set this property.
        /// </exception>
        /// <permission cref="System.Security.Permissions.SecurityPermission"> to set this property. Demand value: <see cref="F:System.Security.Permissions.SecurityAction.Demand"/>; Named Permission Sets: <b>FullTrust</b>.</permission>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public static bool RequestBlockSleep
        {
            get
            {
                CoreHelpers.ThrowIfNotXP();

                return requestBlockSleep;
            }
            [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
            set
            {
                CoreHelpers.ThrowIfNotXP();

                if (value)
                    PowerManager.SetThreadExecutionState(ExecutionStates.Continuous | ExecutionStates.SystemRequired);
                else
                    PowerManager.SetThreadExecutionState(ExecutionStates.Continuous);

                requestBlockSleep = value;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether a battery is present.  
        /// The battery can be a short term battery.
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Requires XP/Windows Server 2003 or higher.</exception>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public static bool IsBatteryPresent
        {
            get
            {
                CoreHelpers.ThrowIfNotXP();

                return Power.GetSystemBatteryState().BatteryPresent;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the battery is a short term battery. 
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Requires XP/Windows Server 2003 or higher.</exception>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public static bool IsBatteryShortTerm
        {
            get
            {
                CoreHelpers.ThrowIfNotXP();
                return Power.GetSystemPowerCapabilities().BatteriesAreShortTerm;
            }
        }

        /// <summary>
        /// Gets a value that indicates a UPS is present to prevent 
        /// sudden loss of power.
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Requires XP/Windows Server 2003 or higher.</exception>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public static bool IsUpsPresent
        {
            get
            {
                CoreHelpers.ThrowIfNotXP();

                // Because the native method doesn't return the correct value for .UpsPresent,
                // use .BatteriesAreShortTerm and .SystemBatteriesPresent to check for UPS
                PowerManagementNativeMethods.SystemPowerCapabilities batt = Power.GetSystemPowerCapabilities();

                return (batt.BatteriesAreShortTerm && batt.SystemBatteriesPresent);
            }
        }

        /// <summary>
        /// Gets a value that indicates the current power scheme.  
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        /// <value>A <see cref="PowerPersonality"/> value.</value>
        public static PowerPersonality PowerPersonality
        {
            get
            {
                Guid guid;
                PowerManagementNativeMethods.PowerGetActiveScheme(IntPtr.Zero, out guid);

                try
                {
                    return PowerPersonalityGuids.GuidToEnum(guid);
                }
                finally
                {
                    CoreNativeMethods.LocalFree(ref guid);
                }
            }
        }



        /// <summary>
        /// Gets a value that indicates the remaining battery life 
        /// (as a percentage of the full battery charge). 
        /// This value is in the range 0-100, 
        /// where 0 is not charged and 100 is fully charged.  
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The system does not have a battery.</exception>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        /// <value>An <see cref="System.Int32"/> value.</value>
        public static int BatteryLifePercent
        {
            get
            {
                // Because of the way this value is being calculated, it should not be limited to granularity
                // as the data from the event (old way) was.
                CoreHelpers.ThrowIfNotVista();
                if (!Power.GetSystemBatteryState().BatteryPresent)
                    throw new InvalidOperationException(LocalizedMessages.PowerManagerBatteryNotPresent);

                var state = Power.GetSystemBatteryState();

                int percent = (int)Math.Round(((double)state.RemainingCapacity / state.MaxCapacity * 100), 0);
                return percent;
            }
        }

        /// <summary>
        /// Gets a value that indictates whether the monitor is on. 
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public static bool IsMonitorOn
        {
            get
            {
                CoreHelpers.ThrowIfNotVista();

                lock (monitoronlock)
                {
                    if (isMonitorOn == null)
                    {
                        EventHandler dummy = delegate(object sender, EventArgs args) { };
                        IsMonitorOnChanged += dummy;
                        // Wait until Windows updates the power source 
                        // (through RegisterPowerSettingNotification)
                        EventManager.monitorOnReset.WaitOne();
                    }
                }

                return (bool)isMonitorOn;
            }
            internal set { isMonitorOn = value; }
        }

        /// <summary>
        /// Gets the current power source.  
        /// </summary>
        /// <exception cref="System.PlatformNotSupportedException">Requires Vista/Windows Server 2008.</exception>
        /// <value>A <see cref="PowerSource"/> value.</value>
        public static PowerSource PowerSource
        {
            get
            {
                CoreHelpers.ThrowIfNotVista();

                if (IsUpsPresent)
                {
                    return PowerSource.Ups;
                }

                if (!IsBatteryPresent || GetCurrentBatteryState().ACOnline)
                {
                    return PowerSource.AC;
                }

                return PowerSource.Battery;
            }
        }
        #endregion

        /// <summary>
        /// Allows an application to inform the system that it 
        /// is in use, thereby preventing the system from entering 
        /// the sleeping power state or turning off the display 
        /// while the application is running.
        /// </summary>
        /// <param name="executionStateOptions">The thread's execution requirements.</param>
        /// <exception cref="Win32Exception">Thrown if the SetThreadExecutionState call fails.</exception>
        public static void SetThreadExecutionState(ExecutionStates executionStateOptions)
        {
            ExecutionStates ret = PowerManagementNativeMethods.SetThreadExecutionState(executionStateOptions);
            if (ret == ExecutionStates.None)
            {
                throw new Win32Exception(LocalizedMessages.PowerExecutionStateFailed);
            }
        }

    }
}
