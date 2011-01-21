//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    public partial class StarBackupMain : PageFunction<WizardResult>
    {
        public StarBackupMain()
        {
            InitializeComponent();
            
            // Images for the command link buttons
            BitmapSource backupBitmapSource = StarBackupHelper.ConvertGDI_To_WPF(Properties.Resources.Backup);
            BitmapSource restoreBitmapSource = StarBackupHelper.ConvertGDI_To_WPF(Properties.Resources.Restore);

            commandLink1.Icon = StarBackupHelper.CreateResizedImage(backupBitmapSource, 32, 32);
            commandLink2.Icon = StarBackupHelper.CreateResizedImage(restoreBitmapSource, 32, 32);

        }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Cancel the wizard and don't return any data
            OnReturn(new ReturnEventArgs<WizardResult>(WizardResult.Canceled));
        }

        public void wizardPage_Return(object sender, ReturnEventArgs<WizardResult> e)
        {
            // If returning, wizard was completed (finished or canceled),
            // so continue returning to calling page
            OnReturn(e);
        }

        void BackupClicked(object sender, RoutedEventArgs e)
        {
            // Go to next wizard page
            StartBackupPage backupPage = new StartBackupPage();
            backupPage.Return += new ReturnEventHandler<WizardResult>(wizardPage_Return);
            this.NavigationService.Navigate(backupPage);
        }

        void RestoreClicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Backup application example: This will perform the restore operation in a real backup application.", "Star Backup Wizard");
        }
    }
}