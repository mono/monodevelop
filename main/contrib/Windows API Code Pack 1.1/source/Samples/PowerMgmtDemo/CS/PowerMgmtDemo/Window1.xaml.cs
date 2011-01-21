//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.Samples.PowerMgmtDemoApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    /// 
    public partial class Window1 : Window
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage(int hWnd, int hMsg, int wParam, int lParam);


        public delegate void MethodInvoker();

        private MyPowerSettings settings;
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        private string cancelReason = string.Empty;
        private System.Windows.Threading.DispatcherTimer TimerClock;

        public Window1()
        {
            InitializeComponent();
            settings = (MyPowerSettings)this.FindResource("powerSettings");

            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            
            // Start a timer/clock so we can periodically ping for the power settings.
            TimerClock = new DispatcherTimer();
            TimerClock.Interval = new TimeSpan(0, 0, 5);
            TimerClock.IsEnabled = true;
            TimerClock.Tick += new EventHandler(TimerClock_Tick);

        }

        void TimerClock_Tick(object sender, EventArgs e)
        {
            GetPowerSettings();
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Once the thread is finished / i.e. indexing is done,
            // update our labels
            if (string.IsNullOrEmpty(cancelReason))
            {
                SetLabelButtonStatus(IndexerCurrentFileLabel, "Indexing completed!");
                SetLabelButtonStatus(IndexerStatusLabel, "Click \"Start Search Indexer\" to run the indexer again.");
                SetLabelButtonStatus(StartStopIndexerButton, "Start Search Indexer!");
            }

            // Clear our the cancel reason as the operation has completed.
            cancelReason = "";
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SetLabelButtonStatus(IndexerCurrentFileLabel, "Running search indexer ....");

            IKnownFolder docs;

            if (ShellLibrary.IsPlatformSupported)
                docs = KnownFolders.DocumentsLibrary;
            else
                docs = KnownFolders.Documents;

            ShellContainer docsContainer = docs as ShellContainer;

            foreach (ShellObject so in docs)
            {
                RecurseDisplay(so);

                if (backgroundWorker.CancellationPending)
                {
                    SetLabelButtonStatus(StartStopIndexerButton, "Start Search Indexer");
                    SetLabelButtonStatus(IndexerStatusLabel, "Click \"Start Search Indexer\" to run the indexer");
                    SetLabelButtonStatus(IndexerCurrentFileLabel, (cancelReason == "powerSourceChanged") ?
                                "Indexing cancelled due to a change in power source" :
                                "Indexing cancelled by the user");

                    return;
                }

                Thread.Sleep(1000); // sleep a second to indicate indexing the file
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CapturePowerManagementEvents();
            GetPowerSettings();
        }

        // Get the current property values from PowerManager.
        // This method is called on startup.
        private void GetPowerSettings()
        {
            settings.PowerPersonality = PowerManager.PowerPersonality.ToString();
            settings.PowerSource = PowerManager.PowerSource.ToString();
            settings.BatteryPresent = PowerManager.IsBatteryPresent;
            settings.UpsPresent = PowerManager.IsUpsPresent;
            settings.MonitorOn = PowerManager.IsMonitorOn;
            settings.MonitorRequired = PowerManager.MonitorRequired;

            if (PowerManager.IsBatteryPresent)
            {
                settings.BatteryShortTerm = PowerManager.IsBatteryShortTerm;
                settings.BatteryLifePercent = PowerManager.BatteryLifePercent;
                
                BatteryState batteryState = PowerManager.GetCurrentBatteryState();

                string batteryStateStr = string.Format(
                "ACOnline: {1}{0}Max Charge: {2} mWh{0}Current Charge: {3} mWh{0}Charge Rate: {4} {0}Estimated Time Remaining: {5}{0}Suggested Critical Battery Charge: {6} mWh{0}Suggested Battery Warning Charge: {7} mWh{0}",
                Environment.NewLine, 
                batteryState.ACOnline, 
                batteryState.MaxCharge, 
                batteryState.CurrentCharge, 
                batteryState.ACOnline == true ? "N/A" : batteryState.ChargeRate.ToString() + " mWh", 
                batteryState.ACOnline == true ? "N/A" : batteryState.EstimatedTimeRemaining.ToString(), 
                batteryState.SuggestedCriticalBatteryCharge, 
                batteryState.SuggestedBatteryWarningCharge
                );

                settings.BatteryState = batteryStateStr;
            }
        }

        // Adds event handlers for PowerManager events.
        private void CapturePowerManagementEvents()
        {
            PowerManager.IsMonitorOnChanged += new EventHandler(MonitorOnChanged);
            PowerManager.PowerPersonalityChanged += new EventHandler(
                PowerPersonalityChanged);
            PowerManager.PowerSourceChanged += new EventHandler(PowerSourceChanged);
            if (PowerManager.IsBatteryPresent)
            {
                PowerManager.BatteryLifePercentChanged += new EventHandler(BatteryLifePercentChanged);

                // Set the label for the battery life
                SetLabelButtonStatus(batteryLifePercentLabel, string.Format("{0}%", PowerManager.BatteryLifePercent.ToString()));
            }

            PowerManager.SystemBusyChanged += new EventHandler(SystemBusyChanged);
        }

        // PowerManager event handlers.

        void MonitorOnChanged(object sender, EventArgs e)
        {
            settings.MonitorOn = PowerManager.IsMonitorOn;
            AddEventMessage(string.Format("Monitor status changed (new status: {0})", PowerManager.IsMonitorOn ? "On" : "Off"));
        }

        void PowerPersonalityChanged(object sender, EventArgs e)
        {
            settings.PowerPersonality = PowerManager.PowerPersonality.ToString();
            AddEventMessage(string.Format("Power Personality changed (current setting: {0})", PowerManager.PowerPersonality.ToString()));
        }

        void PowerSourceChanged(object sender, EventArgs e)
        {
            settings.PowerSource = PowerManager.PowerSource.ToString();
            AddEventMessage(string.Format("Power source changed (current source: {0})", PowerManager.PowerSource.ToString()));

            //
            if (backgroundWorker.IsBusy)
            {
                if (PowerManager.PowerSource == PowerSource.Battery)
                {
                    // for now just stop
                    cancelReason = "powerSourceChanged";
                    backgroundWorker.CancelAsync();
                }
                else
                {
                    // If we are currently on AC or UPS and switch to UPS or AC, just ignore.
                }
            }
            else
            {
                if (PowerManager.PowerSource == PowerSource.AC || PowerManager.PowerSource == PowerSource.Ups)
                {
                    SetLabelButtonStatus(IndexerStatusLabel, "Click \"Start Search Indexer\" to run the indexer");
                }
            }
        }

        void BatteryLifePercentChanged(object sender, EventArgs e)
        {
            settings.BatteryLifePercent = PowerManager.BatteryLifePercent;
            AddEventMessage(string.Format("Battery life percent changed (new value: {0})", PowerManager.BatteryLifePercent));

            // Set the label for the battery life
            SetLabelButtonStatus(batteryLifePercentLabel, string.Format("{0}%", PowerManager.BatteryLifePercent.ToString()));
        }

        // The event handler must use the window's Dispatcher
        // to update the UI directly. This is necessary because
        // the event handlers are invoked on a non-UI thread.
        void SystemBusyChanged(object sender, EventArgs e)
        {
            AddEventMessage(string.Format("System busy changed at {0}", DateTime.Now.ToLongTimeString()));
        }

        void AddEventMessage(string message)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                (Window1.MethodInvoker)delegate
                {
                    ListBoxItem lbi = new ListBoxItem();
                    lbi.Content = message;
                    messagesListBox.Items.Add(lbi);
                    messagesListBox.ScrollIntoView(lbi);
                });
        }

        private void StartIndexer(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker.IsBusy && ((Button)sender).Content.ToString() == "Stop Indexer")
            {
                cancelReason = "userCancelled";
                backgroundWorker.CancelAsync();
                SetLabelButtonStatus(IndexerStatusLabel, "Click \"Start Search Indexer\" to run the indexer");
                return;
            }

            // If running on battery, don't start the indexer
            if (PowerManager.PowerSource != PowerSource.Battery)
            {
                backgroundWorker.RunWorkerAsync();
                SetLabelButtonStatus(IndexerStatusLabel, "Indexer running....");
                SetLabelButtonStatus(StartStopIndexerButton, "Stop Indexer");
            }
            else
            {
                SetLabelButtonStatus(IndexerCurrentFileLabel, "Running on battery. Not starting the indexer");
            }
        }

        private void RecurseDisplay(ShellObject so)
        {
            if (backgroundWorker.CancellationPending)
                return;

            SetLabelButtonStatus(IndexerCurrentFileLabel,
                string.Format("Current {0}: {1}", so is ShellContainer ? "Folder" : "File", so.ParsingName));

            // Loop through this object's child items if it's a container
            ShellContainer container = so as ShellContainer;

            if (container != null)
            {
                foreach (ShellObject child in container)
                    RecurseDisplay(child);
            }
        }

        private void SetLabelButtonStatus(ContentControl control, string status)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                (Window1.MethodInvoker)delegate
                {
                    control.Content = status;
                });
        }
    }
}
