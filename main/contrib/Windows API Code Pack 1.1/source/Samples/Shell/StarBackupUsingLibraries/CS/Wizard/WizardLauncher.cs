//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    public class WizardLauncher : PageFunction<WizardResult>
    {
        public event WizardReturnEventHandler WizardReturn;

        protected override void Start()
        {
            base.Start();

            // So we remember the WizardCompleted event registration
            this.KeepAlive = true;
            
            // Launch the wizard
            StarBackupMain StarBackupMain = new StarBackupMain();
            StarBackupMain.Return += new ReturnEventHandler<WizardResult>(wizardPage_Return);
            this.NavigationService.Navigate(StarBackupMain);
        }

        public void wizardPage_Return(object sender, ReturnEventArgs<WizardResult> e)
        {
            // Notify client that wizard has completed
            // NOTE: We need this custom event because the Return event cannot be
            // registered by window code - if WizardDialogBox registers an event handler with
            // the WizardLauncher's Return event, the event is not raised.
            if (this.WizardReturn != null)
            {
                this.WizardReturn(this, new WizardReturnEventArgs(e.Result, null));
            }
            OnReturn(null);
        }
    }
}
