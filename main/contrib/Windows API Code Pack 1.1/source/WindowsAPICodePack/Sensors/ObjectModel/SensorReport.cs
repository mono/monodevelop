// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Represents all the data from a single sensor data report.
    /// </summary>
    public class SensorReport
    {
        /// <summary>
        /// Gets the time when the data report was generated.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TimeStamp")]
        public DateTime TimeStamp
        {
            get
            {
                return timeStamp;
            }
        }

        /// <summary>
        /// Gets the data values in the report.
        /// </summary>
        public SensorData Values
        {
            get
            {
                return sensorData;
            }
        }

        /// <summary>
        /// Gets the sensor that is the source of this data report.
        /// </summary>
        public Sensor Source
        {
            get
            {
                return originator;
            }
        }

        #region implementation
        private SensorData sensorData;
        private Sensor originator;
        private DateTime timeStamp = new DateTime();

        internal static SensorReport FromNativeReport(Sensor originator, ISensorDataReport iReport)
        {

            SystemTime systemTimeStamp = new SystemTime();
            iReport.GetTimestamp(out systemTimeStamp);
            FILETIME ftTimeStamp = new FILETIME();
            SensorNativeMethods.SystemTimeToFileTime(ref systemTimeStamp, out ftTimeStamp);
            long lTimeStamp = (((long)ftTimeStamp.dwHighDateTime) << 32) + (long)ftTimeStamp.dwLowDateTime;
            DateTime timeStamp = DateTime.FromFileTime(lTimeStamp);

            SensorReport sensorReport = new SensorReport();
            sensorReport.originator = originator;
            sensorReport.timeStamp = timeStamp;
            sensorReport.sensorData = SensorData.FromNativeReport(originator.internalObject, iReport);

            return sensorReport;
        }
        #endregion

    }
}
