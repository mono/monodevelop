// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.Sensors;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class UnknownSensorTests
    {
        [Fact]
        public void ConstructAndConfirmThatGettersThrow()
        {
            UnknownSensor us = new UnknownSensor();

            Assert.Throws<SensorPlatformException>(() => { bool b = us.AutoUpdateDataReport; }); // BUG: Inconsistency with the rest of the API
            Assert.Throws<NullReferenceException>(() => { Guid? g = us.CategoryId; });
            Assert.Throws<NullReferenceException>(() => { SensorConnectionType? t = us.ConnectionType; });
            Assert.Equal<SensorReport>(null, us.DataReport);   // BUG: Inconsistency
            Assert.Throws<NullReferenceException>(() => { string s = us.Description; });
            Assert.Throws<NullReferenceException>(() => { string s = us.DevicePath; });
            Assert.Throws<NullReferenceException>(() => { string s = us.FriendlyName; });
            Assert.Throws<NullReferenceException>(() => { string s = us.Manufacturer; });
            Assert.Throws<NullReferenceException>(() => { uint u = us.MinimumReportInterval; });
            Assert.Throws<NullReferenceException>(() => { string s = us.Model; });
            Assert.Throws<NullReferenceException>(() => { uint u = us.ReportInterval; });
            Assert.Throws<NullReferenceException>(() => { Guid? g = us.SensorId; });
            Assert.Throws<NullReferenceException>(() => { string s = us.SerialNumber; });
            Assert.Throws<NullReferenceException>(() => { SensorState s = us.State; });
            Assert.Throws<NullReferenceException>(() => { Guid? g = us.TypeId; });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AutoUpdateDataReportPropertySetterThrows(bool autoUpdate)
        {
            UnknownSensor us = new UnknownSensor();
            Assert.Throws<SensorPlatformException>(() => { us.AutoUpdateDataReport = autoUpdate; }); // BUG: Inconsistency with the rest of the API
        }

        [Theory]
        [InlineData(UInt32.MinValue)]
        [InlineData(UInt32.MinValue + 1)]
        [InlineData(UInt32.MinValue + 2)]
        [InlineData(UInt32.MaxValue - 1)]
        [InlineData(UInt32.MaxValue)]
        public void ReportIntervalPropertySetterThrows(uint interval)
        {
            UnknownSensor us = new UnknownSensor();
            Assert.Throws<NullReferenceException>(() => { us.ReportInterval = interval; });
        }
    }
}
