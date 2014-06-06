//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// A snapshot of the state of the battery.
    /// </summary>
    public class BatteryState
    {
        internal BatteryState()
        {
            var state = Power.GetSystemBatteryState();

            if (!state.BatteryPresent)
            {
                throw new InvalidOperationException(LocalizedMessages.PowerManagerBatteryNotPresent);
            }

            ACOnline = state.AcOnLine;
            MaxCharge = (int)state.MaxCapacity;
            CurrentCharge = (int)state.RemainingCapacity;
            ChargeRate = (int)state.Rate;

            uint estimatedTime = state.EstimatedTime;
            if (estimatedTime != uint.MaxValue) // uint.MaxValue signifies indefinite estimated time (plugged in)
            {
                EstimatedTimeRemaining = new TimeSpan(0, 0, (int)estimatedTime);
            }
            else
            {
                EstimatedTimeRemaining = TimeSpan.MaxValue;
            }

            SuggestedCriticalBatteryCharge = (int)state.DefaultAlert1;
            SuggestedBatteryWarningCharge = (int)state.DefaultAlert2;
        }

        #region Public properties

        /// <summary>
        /// Gets a value that indicates whether the battery charger is 
        /// operating on external power.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value. <b>True</b> indicates the battery charger is operating on AC power.</value>
        public bool ACOnline { get; private set; }

        /// <summary>
        /// Gets the maximum charge of the battery (in mW).
        /// </summary>
        /// <value>An <see cref="System.Int32"/> value.</value>
        public int MaxCharge { get; private set; }

        /// <summary>
        /// Gets the current charge of the battery (in mW).
        /// </summary>
        /// <value>An <see cref="System.Int32"/> value.</value>
        public int CurrentCharge { get; private set; }
        /// <summary>
        /// Gets the rate of discharge for the battery (in mW). 
        /// </summary>
        /// <remarks>
        /// If plugged in, fully charged: DischargeRate = 0.
        /// If plugged in, charging: DischargeRate = positive mW per hour.
        /// If unplugged: DischargeRate = negative mW per hour.
        /// </remarks>
        /// <value>An <see cref="System.Int32"/> value.</value>
        public int ChargeRate { get; private set; }

        /// <summary>
        /// Gets the estimated time remaining until the battery is empty.
        /// </summary>
        /// <value>A <see cref="System.TimeSpan"/> object.</value>
        public TimeSpan EstimatedTimeRemaining { get; private set; }

        /// <summary>
        /// Gets the manufacturer's suggested battery charge level 
        /// that should cause a critical alert to be sent to the user.
        /// </summary>
        /// <value>An <see cref="System.Int32"/> value.</value>
        public int SuggestedCriticalBatteryCharge { get; private set; }

        /// <summary>
        /// Gets the manufacturer's suggested battery charge level
        /// that should cause a warning to be sent to the user.
        /// </summary>
        /// <value>An <see cref="System.Int32"/> value.</value>
        public int SuggestedBatteryWarningCharge { get; private set; }

        #endregion

        /// <summary>
        /// Generates a string that represents this <b>BatteryState</b> object.
        /// </summary>
        /// <returns>A <see cref="System.String"/> representation of this object's current state.</returns>        
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LocalizedMessages.BatteryStateStringRepresentation,
                Environment.NewLine,
                ACOnline,
                MaxCharge,
                CurrentCharge,
                ChargeRate,
                EstimatedTimeRemaining,
                SuggestedCriticalBatteryCharge,
                SuggestedBatteryWarningCharge
                );
        }
    }
}
