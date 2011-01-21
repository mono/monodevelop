//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;

namespace Microsoft.WindowsAPICodePack.Samples.PowerMgmtDemoApp
{
    internal class MyPowerSettings : INotifyPropertyChanged
    {
        string powerPersonality;
        string powerSource;
        bool batteryPresent;
        bool upsPresent;
        bool monitorOn;
        bool batteryShortTerm;
        int batteryLifePercent;
        string batteryStateACOnline;
        bool monitorRequired;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public string PowerPersonality
        {
            get { return powerPersonality; }
            set
            {
                if (powerPersonality != value)
                {
                    powerPersonality = value;
                    OnPropertyChanged("PowerPersonality");
                }
            }
        }

        public string PowerSource
        {
            get { return powerSource; }
            set
            {
                if (powerSource != value)
                {
                    powerSource = value;
                    OnPropertyChanged("PowerSource");
                }
            }
        }
        public bool BatteryPresent
        {
            get { return batteryPresent; }
            set
            {
                if (batteryPresent != value)
                {
                    batteryPresent = value;
                    OnPropertyChanged("BatteryPresent");
                }
            }
        }
        public bool UpsPresent
        {
            get { return upsPresent; }
            set
            {
                if (upsPresent != value)
                {
                    upsPresent = value;
                    OnPropertyChanged("UPSPresent");
                }
            }
        }

        public bool MonitorOn
        {
            get { return monitorOn; }
            set
            {
                if (monitorOn != value)
                {
                    monitorOn = value;
                    OnPropertyChanged("MonitorOn");
                }
            }
        }
        public bool BatteryShortTerm
        {
            get { return batteryShortTerm; }
            set
            {
                if (batteryShortTerm != value)
                {
                    batteryShortTerm = value;
                    OnPropertyChanged("BatteryShortTerm");
                }
            }
        }
        public int BatteryLifePercent
        {
            get { return batteryLifePercent; }
            set
            {
                if (batteryLifePercent != value)
                {
                    batteryLifePercent = value;
                    OnPropertyChanged("BatteryLifePercent");
                }
            }
        }
        public String BatteryState
        {
            get { return batteryStateACOnline; }
            set
            {
                if (batteryStateACOnline != value)
                {
                    batteryStateACOnline = value;
                    OnPropertyChanged("BatteryState");
                }
            }
        }
        public bool MonitorRequired
        {
            get { return monitorRequired; }
            set
            {
                if (monitorRequired != value)
                {
                    monitorRequired = value;
                    OnPropertyChanged("MonitorRequired");
                }
            }
        }

        // Create the OnPropertyChanged method to raise the event

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
