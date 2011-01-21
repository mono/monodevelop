// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using Xunit;

namespace Tests
{
    public class BatteryStateTests
    {
        [Fact]
        public void ConfirmBatteryState()
        {
            BatteryState s = PowerManager.GetCurrentBatteryState();

            Assert.InRange<int>(s.CurrentCharge, 0, Int32.MaxValue);
            // TODO: add more tests with heuristics here (i.e. when not plugged in, est time remaining < a reasonable number, etc.)
            Assert.InRange<TimeSpan>(s.EstimatedTimeRemaining, TimeSpan.MinValue, TimeSpan.MaxValue);
            Assert.InRange<int>(s.MaxCharge, 0, Int32.MaxValue);

            // The max values below are just numbers we picked.
            Assert.InRange<int>(s.SuggestedBatteryWarningCharge, 0, 10000); 
            Assert.InRange<int>(s.SuggestedCriticalBatteryCharge, 0, 10000);
        }

        [Fact]
        public void DischargeRateIsNonNegativeIfPluggedIn()
        {
            BatteryState s = PowerManager.GetCurrentBatteryState();
            if (s.ACOnline)
            {
                Assert.True(s.ChargeRate >= 0);
            }            
        }

        [Fact] 
        public void DischargeRateIsNegativeIfNotPluggedIn()
        {
            BatteryState s = PowerManager.GetCurrentBatteryState();
            if (!s.ACOnline)
            {
                Assert.True(s.ChargeRate < 0);
            }
        }
    }
}
