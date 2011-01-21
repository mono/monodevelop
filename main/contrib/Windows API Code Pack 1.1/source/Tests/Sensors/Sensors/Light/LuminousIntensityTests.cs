// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Sensors;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class LuminousIntensityTests
    {
        [Fact]
        public void Construction()
        {
            SensorReport sr = new SensorReport();
            LuminousIntensity li = new LuminousIntensity(sr);
            Assert.Equal<float>(0, li.Intensity);
        }
    }
}
