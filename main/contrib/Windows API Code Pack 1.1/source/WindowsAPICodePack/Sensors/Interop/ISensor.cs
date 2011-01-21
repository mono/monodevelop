// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Sensors
{

    /// <summary>
    /// A COM interop wrapper for the ISensor interface.
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("5FA08F80-2657-458E-AF75-46F73FA6AC5C")]
    internal interface ISensor
    {
        /// <summary>
        /// Unique ID of sensor within the sensors platform
        /// </summary>
        /// <param name="id">The unique ID to be returned</param>
        [PreserveSig]
        HResult GetID(out Guid id);

        /// <summary>
        /// Category of sensor Ex: Location
        /// </summary>
        /// <param name="sensorCategory">The sensor category to be returned</param>
        [PreserveSig]
        HResult GetCategory(out Guid sensorCategory);

        /// <summary>
        /// Specific type of sensor: Ex: IPLocationSensor
        /// </summary>
        /// <param name="sensorType">The sensor Type to be returned</param>
        [PreserveSig]
        HResult GetType(out Guid sensorType);

        /// <summary>
        /// Human readable name for sensor
        /// </summary>
        /// <param name="friendlyName">The friendly name for the sensor</param>
        [PreserveSig]
        HResult GetFriendlyName([Out, MarshalAs(UnmanagedType.BStr)] out string friendlyName);

        /// <summary>
        /// Sensor metadata: make, model, serial number, etc
        /// </summary>
        /// <param name="key">The PROPERTYKEY for the property to be retrieved</param>
        /// <param name="property">The property returned</param>
        [PreserveSig]
        HResult GetProperty([In] ref PropertyKey key, [Out] PropVariant property);

        /// <summary>
        /// Bulk Sensor metadata query: make, model, serial number, etc
        /// </summary>
        /// <param name="keys">The PROPERTYKEY collection for the properties to be retrieved</param>
        /// <param name="properties">The properties returned</param>
        [PreserveSig]
        HResult GetProperties(
            [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceKeyCollection keys,
            [Out, MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValues properties);

        /// <summary>
        /// Get the array of SensorDataField objects that describe the individual values that can be reported by the sensor
        /// </summary>
        /// <param name="dataFields">A collection of PROPERTYKEY structures representing the data values reported by the sensor</param>
        [PreserveSig]
        HResult GetSupportedDataFields(
            [Out, MarshalAs(UnmanagedType.Interface)] out IPortableDeviceKeyCollection dataFields);

        /// <summary>
        /// Bulk Sensor metadata set for settable properties
        /// </summary>
        /// <param name="properties">The properties to be set</param>
        /// <param name="results">The PROPERTYKEY/HRESULT pairs indicating success/failure for each property set</param>
        [PreserveSig]
        HResult SetProperties(
            [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceValues properties,
            [Out, MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValues results);

        /// <summary>
        /// Reports whether or not a sensor can deliver the requested data type
        /// </summary>
        /// <param name="key">The GUID to find matching PROPERTYKEY structures for</param>
        /// <param name="isSupported">A collection of PROPERTYKEY structures representing the data values</param>
        void SupportsDataField(
            [In] PropertyKey key,
            [Out, MarshalAs(UnmanagedType.VariantBool)] out bool isSupported);

        /// <summary>
        /// Get the sensor state
        /// </summary>
        /// <param name="state">The SensorState returned</param>
        void GetState([Out, MarshalAs(UnmanagedType.U4)] out NativeSensorState state);

        /// <summary>
        /// Get the most recent ISensorDataReport for the sensor
        /// </summary>
        /// <param name="dataReport">The data report returned</param>
        [PreserveSig]
        HResult GetData([Out, MarshalAs(UnmanagedType.Interface)] out ISensorDataReport dataReport);

        /// <summary>
        /// Reports whether or not a sensor supports the specified event.
        /// </summary>
        /// <param name="eventGuid">The event identifier</param>
        /// <param name="isSupported">true if the event is supported, otherwise false</param>
        void SupportsEvent(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid eventGuid,
            [Out, MarshalAs(UnmanagedType.VariantBool)] out bool isSupported);

        /// <summary>
        /// Reports the set of event interests.
        /// </summary>
        /// <param name="pValues"></param>
        /// <param name="count"></param>
        void GetEventInterest(
            out IntPtr pValues, [Out] out uint count);

        /// <summary>
        /// Sets the set of event interests
        /// </summary>
        /// <param name="pValues">The array of GUIDs representing sensor events of interest</param>
        /// <param name="count">The number of guids included</param>
        [PreserveSig]
        HResult SetEventInterest([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Guid[] pValues, [In] uint count);

        /// <summary>
        /// Subscribe to ISensor events
        /// </summary>
        /// <param name="pEvents">An interface pointer to the callback object created for events</param>
        void SetEventSink([In, MarshalAs(UnmanagedType.Interface)] ISensorEvents pEvents);
    }
}
