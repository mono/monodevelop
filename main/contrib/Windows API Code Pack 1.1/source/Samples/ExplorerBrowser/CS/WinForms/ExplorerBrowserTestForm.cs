//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using System.Windows.Forms.Integration;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Controls;
using System.Threading;

namespace Microsoft.WindowsAPICodePack.Samples
{
    public partial class ExplorerBrowserTestForm : Form
    {
        System.Windows.Forms.Timer uiDecoupleTimer = new System.Windows.Forms.Timer();
        AutoResetEvent selectionChanged = new AutoResetEvent(false);
        AutoResetEvent itemsChanged = new AutoResetEvent(false);

        public ExplorerBrowserTestForm()
        {
            InitializeComponent();

            // initialize known folder combo box
            List<string> knownFolderList = new List<string>();
            foreach (IKnownFolder folder in KnownFolders.All)
            {
                knownFolderList.Add(folder.CanonicalName);
            }
            knownFolderList.Sort();
            knownFolderCombo.Items.AddRange(knownFolderList.ToArray());

            // initial property grids
            navigationPropertyGrid.SelectedObject = explorerBrowser.NavigationOptions;
            visibilityPropertyGrid.SelectedObject = explorerBrowser.NavigationOptions.PaneVisibility;
            contentPropertyGrid.SelectedObject = explorerBrowser.ContentOptions;

            // setup ExplorerBrowser navigation events
            explorerBrowser.NavigationPending += new EventHandler<NavigationPendingEventArgs>(explorerBrowser_NavigationPending);
            explorerBrowser.NavigationFailed += new EventHandler<NavigationFailedEventArgs>(explorerBrowser_NavigationFailed);
            explorerBrowser.NavigationComplete += new EventHandler<NavigationCompleteEventArgs>(explorerBrowser_NavigationComplete);
            explorerBrowser.ItemsChanged += new EventHandler(explorerBrowser_ItemsChanged);
            explorerBrowser.SelectionChanged += new EventHandler(explorerBrowser_SelectionChanged);
            explorerBrowser.ViewEnumerationComplete += new EventHandler(explorerBrowser_ViewEnumerationComplete);

            // set up Navigation log event and button state
            explorerBrowser.NavigationLog.NavigationLogChanged += new EventHandler<NavigationLogEventArgs>(NavigationLog_NavigationLogChanged);
            this.backButton.Enabled = false;
            this.forwardButton.Enabled = false;

            uiDecoupleTimer.Tick += new EventHandler(uiDecoupleTimer_Tick);
            uiDecoupleTimer.Interval = 100;
            uiDecoupleTimer.Start();
        }

        void uiDecoupleTimer_Tick(object sender, EventArgs e)
        {
            if (selectionChanged.WaitOne(1))
            {
                StringBuilder itemsText = new StringBuilder();

                foreach (ShellObject item in explorerBrowser.SelectedItems)
                {
                    if (item != null)
                        itemsText.AppendLine("\tItem = " + item.GetDisplayName(DisplayNameType.Default));
                }

                this.selectedItemsTextBox.Text = itemsText.ToString();
                this.itemsTabControl.TabPages[1].Text = "Selected Items (Count=" + explorerBrowser.SelectedItems.Count.ToString() + ")";
            }

            if (itemsChanged.WaitOne(1))
            {
                // update items text box
                StringBuilder itemsText = new StringBuilder();

                foreach (ShellObject item in explorerBrowser.Items)
                {
                    if (item != null)
                        itemsText.AppendLine("\tItem = " + item.GetDisplayName(DisplayNameType.Default));
                }

                this.itemsTextBox.Text = itemsText.ToString();
                this.itemsTabControl.TabPages[0].Text = "Items (Count=" + explorerBrowser.Items.Count.ToString() + ")";
            }
        }

        void explorerBrowser_ViewEnumerationComplete(object sender, EventArgs e)
        {
            // This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            BeginInvoke(new MethodInvoker(delegate()
            {
                this.eventHistoryTextBox.Text =
                    this.eventHistoryTextBox.Text +
                    "View enumeration complete.\n";
            }));

            selectionChanged.Set();
            itemsChanged.Set();
        }


        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            explorerBrowser.Navigate((ShellObject)KnownFolders.Desktop);
        }

        void NavigationLog_NavigationLogChanged(object sender, NavigationLogEventArgs args)
        {
            // This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            BeginInvoke(new MethodInvoker(delegate()
            {
                // calculate button states
                if (args.CanNavigateBackwardChanged)
                {
                    this.backButton.Enabled = explorerBrowser.NavigationLog.CanNavigateBackward;
                }
                if (args.CanNavigateForwardChanged)
                {
                    this.forwardButton.Enabled = explorerBrowser.NavigationLog.CanNavigateForward;
                }

                // update history combo box
                if (args.LocationsChanged)
                {
                    this.navigationHistoryCombo.Items.Clear();
                    foreach (ShellObject shobj in this.explorerBrowser.NavigationLog.Locations)
                    {
                        this.navigationHistoryCombo.Items.Add(shobj.Name);
                    }
                }
                if (this.explorerBrowser.NavigationLog.CurrentLocationIndex == -1)
                    this.navigationHistoryCombo.Text = "";
                else
                    this.navigationHistoryCombo.SelectedIndex = this.explorerBrowser.NavigationLog.CurrentLocationIndex;
            }));
        }

        void explorerBrowser_SelectionChanged(object sender, EventArgs e)
        {
            selectionChanged.Set();
        }

        void explorerBrowser_ItemsChanged(object sender, EventArgs e)
        {
            itemsChanged.Set();
        }

        void explorerBrowser_NavigationComplete(object sender, NavigationCompleteEventArgs args)
        {
            // This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            BeginInvoke(new MethodInvoker(delegate()
            {
                // update event history text box
                string location = (args.NewLocation == null) ? "(unknown)" : args.NewLocation.Name;
                this.eventHistoryTextBox.Text =
                    this.eventHistoryTextBox.Text +
                    "Navigation completed. New Location = " + location + "\n";
            }));
        }

        void explorerBrowser_NavigationFailed(object sender, NavigationFailedEventArgs args)
        {
            // This event is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            BeginInvoke(new MethodInvoker(delegate()
            {
                // update event history text box
                string location = (args.FailedLocation == null) ? "(unknown)" : args.FailedLocation.Name;
                this.eventHistoryTextBox.Text =
                    this.eventHistoryTextBox.Text +
                    "Navigation failed. Failed Location = " + location + "\n";

                if (this.explorerBrowser.NavigationLog.CurrentLocationIndex == -1)
                    this.navigationHistoryCombo.Text = "";
                else
                    this.navigationHistoryCombo.SelectedIndex = this.explorerBrowser.NavigationLog.CurrentLocationIndex;
            }));
        }

        void explorerBrowser_NavigationPending(object sender, NavigationPendingEventArgs args)
        {
            // fail navigation if check selected (this must be synchronous)
            args.Cancel = this.failNavigationCheckBox.Checked;


            // This portion is BeginInvoked to decouple the ExplorerBrowser UI from this UI
            BeginInvoke(new MethodInvoker(delegate()
            {
                // update event history text box
                string message = "";
                string location = (args.PendingLocation == null) ? "(unknown)" : args.PendingLocation.Name;
                if (args.Cancel)
                {
                    message = "Navigation Failing. Pending Location = " + location;
                }
                else
                {
                    message = "Navigation Pending. Pending Location = " + location;
                }
                this.eventHistoryTextBox.Text =
                    this.eventHistoryTextBox.Text + message + "\n";
            }));
        }

        private void navigateButton_Click(object sender, EventArgs e)
        {
            try
            {
                // navigate to specific folder
                explorerBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(pathEdit.Text));
            }
            catch (COMException)
            {
                MessageBox.Show("Navigation not possible.");
            }
        }

        private void filePathNavigate_Click(object sender, EventArgs e)
        {
            try
            {
                // Navigates to a specified file (must be a container file to work, i.e., ZIP, CAB)         
                this.explorerBrowser.Navigate(ShellFile.FromFilePath(this.filePathEdit.Text));
            }
            catch (COMException)
            {
                MessageBox.Show("Navigation not possible.");
            }
        }

        private void knownFolderNavigate_Click(object sender, EventArgs e)
        {
            try
            {
                // Navigate to a known folder
                IKnownFolder kf =
                    KnownFolderHelper.FromCanonicalName(
                        this.knownFolderCombo.Items[knownFolderCombo.SelectedIndex].ToString());

                this.explorerBrowser.Navigate((ShellObject)kf);
            }
            catch (COMException)
            {
                MessageBox.Show("Navigation not possible.");
            }
        }

        private void forwardButton_Click(object sender, EventArgs e)
        {
            // Move forwards through navigation log
            explorerBrowser.NavigateLogLocation(NavigationLogDirection.Forward);
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            // Move backwards through navigation log
            explorerBrowser.NavigateLogLocation(NavigationLogDirection.Backward);
        }

        private void navigationHistoryCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            // navigating to specific index in navigation log
            explorerBrowser.NavigateLogLocation(this.navigationHistoryCombo.SelectedIndex);
        }

        private void clearHistoryButton_Click(object sender, EventArgs e)
        {
            // clear navigation log
            explorerBrowser.NavigationLog.ClearLog();
        }

        private void filePathEdit_TextChanged(object sender, EventArgs e)
        {
            filePathNavigate.Enabled = (filePathEdit.Text.Length > 0);
        }

        private void pathEdit_TextChanged(object sender, EventArgs e)
        {
            navigateButton.Enabled = (pathEdit.Text.Length > 0);
        }

        private void knownFolderCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            knownFolderNavigate.Enabled = (knownFolderCombo.Text.Length > 0);
        }
    }
}
