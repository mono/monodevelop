//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Navigation;

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    public partial class WizardDialogBox : NavigationWindow
    {
        public WizardDialogBox()
        {
            InitializeComponent();

            // Launch the wizard
            WizardLauncher wizardLauncher = new WizardLauncher();
            wizardLauncher.WizardReturn += new WizardReturnEventHandler(wizardLauncher_WizardReturn);
            this.Navigate(wizardLauncher);
        }

        void wizardLauncher_WizardReturn(object sender, WizardReturnEventArgs e)
        {
            // Handle wizard return
            if (this.DialogResult == null)
            {
                this.DialogResult = (e.Result == WizardResult.Finished);
            }
        }
    }
}
