// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// The SystemTime structure represents a date and time using individual members for 
    /// the month, day, year, weekday, hour, minute, second, and millisecond.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemTime
    {
        internal ushort Year;
        internal ushort Month;
        internal ushort DayOfWeek;
        internal ushort Day;
        internal ushort Hour;
        internal ushort Minute;
        internal ushort Second;
        internal ushort Millisecond;

        /// <summary>
        /// Gets the <see cref="DateTime"/> representation of this object.
        /// </summary>
        public DateTime DateTime
        {
            get { return new DateTime(Year, Month, Day, Hour, Minute, Second, Millisecond); }
        }

        public static implicit operator DateTime(SystemTime systemTime)
        {
            return systemTime.DateTime;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture.NumberFormat, 
                "{0:D2}/{1:D2}/{2:D4}, {3:D2}:{4:D2}:{5:D2}.{6}", 
                Month, Day, Year, Hour, Minute, Second, Millisecond);
        }
    }

    /// <summary>
    /// COM interop wrapper for the ISensorDataReport interface.
    /// </summary>
    [ComImport, Guid("0AB9DF9B-C4B5-4796-8898-0470706A2E1D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISensorDataReport
    {
        /// <summary>
        /// Get the timestamp for the data report
        /// </summary>
        /// <param name="timeStamp">The timestamp returned for the data report</param>
        void GetTimestamp(out SystemTime timeStamp);

        /// <summary>
        /// Get a single value reported by the sensor
        /// </summary>
        /// <param name="propKey">The data field ID of interest</param>
        /// <param name="propValue">The data returned</param>
        void GetSensorValue(
            [In] ref PropertyKey propKey,
            [Out] PropVariant propValue);

        /// <summary>
        /// Get multiple values reported by a sensor
        /// </summary>
        /// <param name="keys">The collection of keys for values to obtain data for</param>
        /// <param name="values">The values returned by the query</param>
        void GetSensorValues(
            [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceKeyCollection keys,
            [Out, MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValues values);
    }
}
