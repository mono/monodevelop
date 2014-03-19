// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Threading;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class PowerManagerTests
    {
        [Fact]
        public void BatteryLifePercentIsValid()
        {
            Assert.InRange<int>(PowerManager.BatteryLifePercent, 0, 100);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MonitorRequiredPropertyWorks(bool newValue)
        {
            bool originalValue = PowerManager.MonitorRequired;

            PowerManager.MonitorRequired = newValue;
            Assert.Equal<bool>(newValue, PowerManager.MonitorRequired);

            PowerManager.MonitorRequired = originalValue;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RequestBlockSleepPropertyWorks(bool newValue)
        {
            bool originalValue = PowerManager.RequestBlockSleep;

            PowerManager.RequestBlockSleep = newValue;
            Assert.Equal<bool>(newValue, PowerManager.RequestBlockSleep);

            PowerManager.RequestBlockSleep = originalValue;
        }

        [Theory]
        [InlineData(true, PowerSource.AC)]
        [InlineData(false, PowerSource.Battery)]
        public void PowerSourceCorrespondsToAcOnlineProperty(bool acOnlineState, PowerSource powerSourceState)
        {
            Assert.Equal<bool>(
                acOnlineState == PowerManager.GetCurrentBatteryState().ACOnline,
                powerSourceState == PowerManager.PowerSource);
        }

        [Theory]
        [InlineData(true, PowerSource.Ups)]
        public void PowerSourceCorrespondsToIsUpsPresentProperty(bool isUpsPresentState, PowerSource powerSourceState)
        {
            // BUG: It is strange that IsUpsPresent is exposed on PM, while ACOnline is exposed on the BatteryState property
            Assert.Equal<bool>(
                isUpsPresentState == PowerManager.IsUpsPresent,
                powerSourceState == PowerManager.PowerSource);
        }

        [Fact]
        public void PowerPersonalityPropertyDoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                PowerPersonality pp = PowerManager.PowerPersonality;
            });
        }

        [Theory(Skip = "Event dependent property does not return before timeout on some computers.")]
        [InlineData(true, true)]
        // [InlineData(false, false)] // BUG: Possible bug
        public void IsMonitorOnPropertyWorks(bool monitorRequired, bool expectedIsMonitorOn)
        {
            bool monitorRequiredOriginal = PowerManager.MonitorRequired;

            PowerManager.MonitorRequired = monitorRequired;
            Assert.Equal<bool>(expectedIsMonitorOn, PowerManager.IsMonitorOn);

            PowerManager.MonitorRequired = monitorRequiredOriginal;
        }

        // TODO: Remove the skip attribute when test is complete.
        [Theory(Skip = "Not Implemented")]
        [InlineData(true)]
        [InlineData(false)]
        public void IsMonitorOnChangedEventWorks(bool isMonitorOnValueToSet)
        {
            bool eventFired = false;
            PowerManager.IsMonitorOnChanged += new EventHandler(
                (object sender, EventArgs args) =>
                {
                    eventFired = true;
                });

            // TODO: Fire PowerManager.IsMonitorOnChanged event

            int secTimeout = 5; //wait 5 seconds for event to be fired.
            for (int i = 0; i < secTimeout * 10 && !eventFired; i++)
            {
                Thread.Sleep(100);
            }

            Assert.True(eventFired);

        }

        // TODO: Remove the skip attribute when test is complete.
        [Theory(Skip = "Not Implemented")]
        [InlineData(PowerPersonality.Automatic)]
        [InlineData(PowerPersonality.HighPerformance)]
        [InlineData(PowerPersonality.PowerSaver)]
        public void PowerPersonalityChangedEventWorks(PowerPersonality powerPersonalityToSet)
        {
            PowerPersonality original = PowerManager.PowerPersonality;
            bool eventFired = false;

            PowerManager.PowerPersonalityChanged += new System.EventHandler(
                (object sender, EventArgs e) =>
                {
                    eventFired = true;
                }
            );

            // TODO: Change PowerManager.PowerPersonality, it is readonly.

            int secTimeout = 5; //wait 5 seconds for event to be fired.
            for (int i = 0; i < secTimeout * 10 && !eventFired; i++)
            {
                Thread.Sleep(100);
            }

            // TODO: PowerManager.PowerPersonality = original;

            Assert.True(eventFired);
        }

        // TODO: Remove skip attribute when test is complete.
        [Theory(Skip = "Not Implemented")]
        [InlineData(PowerSource.AC)]
        [InlineData(PowerSource.Battery)]
        [InlineData(PowerSource.Ups)]
        public void PowerSourceChangedEventWorks(PowerSource powerSourceToSet)
        {
            bool eventFired = false;
            PowerManager.PowerSourceChanged += new EventHandler(
                (object sender, EventArgs args) =>
                {
                    eventFired = true;
                });

            // TODO: Fire Powermanager.PowerSourceChanged event.

            int secTimeout = 5; //wait 5 seconds for event to be fired.
            for (int i = 0; i < secTimeout * 10 && !eventFired; i++)
            {
                Thread.Sleep(100);
            }

            Assert.True(eventFired);
        }

        // TODO: Remove skip attribute when test is complete.
        [Fact(Skip = "Not Implemented")]
        public void SystemBusyChangedEventWorks()
        {
            bool eventFired = false;
            PowerManager.SystemBusyChanged += new EventHandler(
                (object sender, EventArgs args) =>
                {
                    eventFired = true;
                });

            // TODO: Fire PowerManager.SystemBusyChanged event.

            int secTimeout = 5; //wait 5 seconds for event to be fired.
            for (int i = 0; i < secTimeout * 10 && !eventFired; i++)
            {
                Thread.Sleep(100);
            }

            Assert.True(eventFired);
        }
    }
}
