// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Sensors;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class SensorDescriptionAttributeTests
    {
        // BUG: Need to make the sensor type strongly-typed (convert to GUID) to prevent malformed SDA objects
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        [InlineData("11111111-2222-3333-4444-555555555555")]
        public void Construction(string sensorTypeGuid)
        {
            SensorDescriptionAttribute a = new SensorDescriptionAttribute(sensorTypeGuid);
            Assert.Equal<string>(sensorTypeGuid, a.SensorType);
        }
    }
}
