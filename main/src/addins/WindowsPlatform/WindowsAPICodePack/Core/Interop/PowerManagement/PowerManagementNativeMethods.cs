//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    internal static class PowerManagementNativeMethods
    {
        #region Power Management

        internal const uint PowerBroadcastMessage = 536;
        internal const uint PowerSettingChangeMessage = 32787;
        internal const uint ScreenSaverSetActive = 0x0011;
        internal const uint UpdateInFile = 0x0001;
        internal const uint SendChange = 0x0002;

        // This structure is sent when the PBT_POWERSETTINGSCHANGE message is sent.
        // It describes the power setting that has changed and 
        // contains data about the change.
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PowerBroadcastSetting
        {
            public Guid PowerSetting;
            public Int32 DataLength;
        }

        // This structure is used when calling CallNtPowerInformation 
        // to retrieve SystemPowerCapabilities
        [StructLayout(LayoutKind.Sequential)]
        public struct SystemPowerCapabilities
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool PowerButtonPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool SleepButtonPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool LidPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool SystemS1;
            [MarshalAs(UnmanagedType.I1)]
            public bool SystemS2;
            [MarshalAs(UnmanagedType.I1)]
            public bool SystemS3;
            [MarshalAs(UnmanagedType.I1)]
            public bool SystemS4;
            [MarshalAs(UnmanagedType.I1)]
            public bool SystemS5;
            [MarshalAs(UnmanagedType.I1)]
            public bool HiberFilePresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool FullWake;
            [MarshalAs(UnmanagedType.I1)]
            public bool VideoDimPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool ApmPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool UpsPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool ThermalControl;
            [MarshalAs(UnmanagedType.I1)]
            public bool ProcessorThrottle;
            public byte ProcessorMinimumThrottle;
            public byte ProcessorMaximumThrottle;
            [MarshalAs(UnmanagedType.I1)]
            public bool FastSystemS4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] spare2;
            [MarshalAs(UnmanagedType.I1)]
            public bool DiskSpinDown;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] spare3;
            [MarshalAs(UnmanagedType.I1)]
            public bool SystemBatteriesPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool BatteriesAreShortTerm;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public BatteryReportingScale[] BatteryScale;
            public SystemPowerState AcOnlineWake;
            public SystemPowerState SoftLidWake;
            public SystemPowerState RtcWake;
            public SystemPowerState MinimumDeviceWakeState;
            public SystemPowerState DefaultLowLatencyWake;
        }

        public enum PowerInformationLevel
        {
            SystemPowerPolicyAc,
            SystemPowerPolicyDc,
            VerifySystemPolicyAc,
            VerifySystemPolicyDc,
            SystemPowerCapabilities,
            SystemBatteryState,
            SystemPowerStateHandler,
            ProcessorStateHandler,
            SystemPowerPolicyCurrent,
            AdministratorPowerPolicy,
            SystemReserveHiberFile,
            ProcessorInformation,
            SystemPowerInformation,
            ProcessorStateHandler2,
            LastWakeTime,
            LastSleepTime,
            SystemExecutionState,
            SystemPowerStateNotifyHandler,
            ProcessorPowerPolicyAc,
            ProcessorPowerPolicyDc,
            VerifyProcessorPowerPolicyAc,
            VerifyProcessorPowerPolicyDc,
            ProcessorPowerPolicyCurrent,
            SystemPowerStateLogging,
            SystemPowerLoggingEntry,
            SetPowerSettingValue,
            NotifyUserPowerSetting,
            PowerInformationLevelUnused0,
            PowerInformationLevelUnused1,
            SystemVideoState,
            TraceApplicationPowerMessage,
            TraceApplicationPowerMessageEnd,
            ProcessorPerfStates,
            ProcessorIdleStates,
            ProcessorCap,
            SystemWakeSource,
            SystemHiberFileInformation,
            TraceServicePowerMessage,
            ProcessorLoad,
            PowerShutdownNotification,
            MonitorCapabilities,
            SessionPowerInit,
            SessionDisplayState,
            PowerRequestCreate,
            PowerRequestAction,
            GetPowerRequestList,
            ProcessorInformationEx,
            NotifyUserModeLegacyPowerEvent,
            GroupPark,
            ProcessorIdleDomains,
            WakeTimerList,
            SystemHiberFileSize,
            PowerInformationLevelMaximum
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BatteryReportingScale
        {
            public UInt32 Granularity;
            public UInt32 Capacity;
        }

        public enum SystemPowerState
        {
            Unspecified = 0,
            Working = 1,
            Sleeping1 = 2,
            Sleeping2 = 3,
            Sleeping3 = 4,
            Hibernate = 5,
            Shutdown = 6,
            Maximum = 7
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemBatteryState
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool AcOnLine;
            [MarshalAs(UnmanagedType.I1)]
            public bool BatteryPresent;
            [MarshalAs(UnmanagedType.I1)]
            public bool Charging;
            [MarshalAs(UnmanagedType.I1)]
            public bool Discharging;
            public byte Spare1;
            public byte Spare2;
            public byte Spare3;
            public byte Spare4;
            public uint MaxCapacity;
            public uint RemainingCapacity;
            public uint Rate;
            public uint EstimatedTime;
            public uint DefaultAlert1;
            public uint DefaultAlert2;
        }

        [DllImport("powrprof.dll")]
        internal static extern UInt32 CallNtPowerInformation(
             PowerInformationLevel informationLevel,
             IntPtr inputBuffer,
             UInt32 inputBufferSize,
             out SystemPowerCapabilities outputBuffer,
             UInt32 outputBufferSize
        );

        [DllImport("powrprof.dll")]
        internal static extern UInt32 CallNtPowerInformation(
             PowerInformationLevel informationLevel,
             IntPtr inputBuffer,
             UInt32 inputBufferSize,
             out SystemBatteryState outputBuffer,
             UInt32 outputBufferSize
        );

        /// <summary>
        /// Gets the Guid relating to the currently active power scheme.
        /// </summary>
        /// <param name="rootPowerKey">Reserved for future use, this must be set to IntPtr.Zero</param>
        /// <param name="activePolicy">Returns a Guid referring to the currently active power scheme.</param>
        [DllImport("powrprof.dll")]
        internal static extern void PowerGetActiveScheme(
            IntPtr rootPowerKey,
            [MarshalAs(UnmanagedType.LPStruct)]
            out Guid activePolicy);
        
        [DllImport("User32", SetLastError = true,
            EntryPoint = "RegisterPowerSettingNotification",
            CallingConvention = CallingConvention.StdCall)]
        internal static extern int RegisterPowerSettingNotification(
                IntPtr hRecipient,
                ref Guid PowerSettingGuid,
                Int32 Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern ExecutionStates SetThreadExecutionState(ExecutionStates esFlags);
        
        #endregion
    }
}
