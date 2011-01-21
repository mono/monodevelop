//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.WindowsAPICodePack.Shell;
using System.Collections.Generic;
using System.Collections;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    public partial class StartBackupPage : PageFunction<WizardResult>
    {
        public StartBackupPage()
        {
            InitializeComponent();
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

        private void buttonAddFolders_Click(object sender, RoutedEventArgs e)
        {
            // Show an Open File Dialog
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            
            // Allow users to select folders and non-filesystem items such as Libraries
            cfd.AllowNonFileSystemItems = true;
            cfd.IsFolderPicker = true;
            
            // MultiSelect = true will allow mutliple selection of folders/libraries.
            cfd.Multiselect = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ICollection<ShellObject> items = cfd.FilesAsShellObject;

                foreach (ShellObject item in items)
                {
                    // If it's a library, need to add the actual folders (scopes)
                    if (item is ShellLibrary)
                    {
                        foreach (ShellFileSystemFolder folder in ((ShellLibrary)item))
                            listBox1.Items.Add(folder.Path);
                    }
                    else if (item is ShellFileSystemFolder)
                    {
                        // else, just add it...
                        listBox1.Items.Add(((ShellFileSystemFolder)item).Path);
                    }
                    else
                    {
                        // For unsupported locations, display an error message.
                        // The above code could be expanded to backup Known Folders that are not virtual,
                        // Search Folders, etc.
                        MessageBox.Show(string.Format("The {0} folder was skipped because it cannot be backed up.", item.Name), "Star Backup");
                    }
                }
            }

            // If we added something, Enable the "Start Backup" button
            if (listBox1.Items.Count > 0)
                buttonStartBackup.IsEnabled = true;
        }

        private void buttonStartBackup_Click(object sender, RoutedEventArgs e)
        {
            // Go to next wizard page (Processing or doing the actual backup)
            BackupProcessPage processPage = new BackupProcessPage(listBox1.Items);
            processPage.Return += new ReturnEventHandler<WizardResult>(wizardPage_Return);
            this.NavigationService.Navigate(processPage);
        }
    }
}
