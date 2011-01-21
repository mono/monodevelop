// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// A COM interop events interface for the ISensorManager object
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9B3B0B86-266A-4AAD-B21F-FDE5501001B7")]
    internal interface ISensorManagerEvents
    {
        void OnSensorEnter(
            [In, MarshalAs(UnmanagedType.Interface)] ISensor pSensor,
            [In, MarshalAs(UnmanagedType.U4)] NativeSensorState state);
    }

    /// <summary>
    /// A COM interop events interface for the ISensor object
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("5D8DCC91-4641-47E7-B7C3-B74F48A6C391")]
    internal interface ISensorEvents
    {
        void OnStateChanged(
            [In, MarshalAs(UnmanagedType.Interface)] ISensor sensor,
            [In, MarshalAs(UnmanagedType.U4)] NativeSensorState state);

        void OnDataUpdated(
            [In, MarshalAs(UnmanagedType.Interface)] ISensor sensor,
            [In, MarshalAs(UnmanagedType.Interface)] ISensorDataReport newData);

        void OnEvent(
            [In, MarshalAs(UnmanagedType.Interface)] ISensor sensor,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid eventID,
            [In, MarshalAs(UnmanagedType.Interface)] ISensorDataReport newData);

        void OnLeave([In, MarshalAs(UnmanagedType.LPStruct)] Guid sensorID);
    }

}
