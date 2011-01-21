//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    public partial class BackupProcessPage : PageFunction<WizardResult>
    {
        private IEnumerable backupList;
        private BackgroundWorker bw;

        public BackupProcessPage(IEnumerable list)
        {
            InitializeComponent();

            // The list of folders from the previous page
            backupList = list;

            // Add the list of folders to our listbox. This won't actually start the backup
            UpdateList(backupList);

            // Create a BackgroundBorker thread to do the actual backup
            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// Gets called when the actual backup process is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // if finished, change button text to done.
            if (!e.Cancelled)
                buttonStopBackup.Content = "Backup Done!";
            else
                buttonStopBackup.Content = "Backup Cancelled!";

            // Disable the start backup button as files are already backed up.
            buttonStopBackup.IsEnabled = false;
        }

        /// <summary>
        /// The method that does the real work. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // Our counter for the folder that we are currently backing up
            int current = 1;

            // Loop through all the items and back each folder up
            // Since the item is just a string path, we could create a ShellFolder (using ShellFolder.FromPath)
            // and then enumerate all the subitems in that folder.
            foreach (ListBoxItem lbi in listBox1.Items)
            {
                // If user has requested a cancel, set our event arg
                if (((BackgroundWorker)sender).CancellationPending)
                    e.Cancel = true;
                else
                {
                    // Do a fake copy/backup of folder ...

                    // Sleep two seconds
                    Thread.Sleep(2000);

                    // Once the copy has been done, report progress back to the Background Worker.
                    // This could be used for a ProgressBar, or in our case, show "check" icon next
                    // to each folder that was backed up.
                    ((BackgroundWorker)sender).ReportProgress((current / listBox1.Items.Count) * 100, lbi);

                    // Increment our counter for folders.
                    current++;
                }
            }
        }

        /// <summary>
        /// When each folder is backed up, some progress is reported. This method will get called each time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // The item we get passed in is the actual ListBoxItem 
            // (contains the StackPanel and label for the folder name)
            ListBoxItem lbi = e.UserState as ListBoxItem;

            if (lbi != null)
            {
                // Get the stack panel so we can get to it's contents
                StackPanel sp = lbi.Content as StackPanel;

                if (sp != null)
                {
                    // Get the image control and set our checked state.
                    Image img = sp.Children[0] as Image;
                    if (img != null)
                        img.Source = StarBackupHelper.ConvertGDI_To_WPF(Properties.Resources.Check);
                }

                // Select the item and make sure its in view. This will give good feedback to the user
                // as we are going down the list and performing some operation on the items.
                listBox1.SelectedItem = lbi;
                listBox1.ScrollIntoView(lbi);
            }
        }

        /// <summary>
        /// Goes through the list of folders to backup and adds each folder name (and an empty image control)
        /// to the listbox.
        /// </summary>
        /// <param name="backupList"></param>
        private void UpdateList(IEnumerable backupList)
        {
            listBox1.Items.Clear();

            foreach (object item in backupList)
            {
                // Start creating our listbox items..
                ListBoxItem lbi = new ListBoxItem();

                // Create a stackpanel to hold our image and textblock
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                Image img = new Image();
                TextBlock tb = new TextBlock();
                tb.Margin = new Thickness(3);
                tb.Text = item.ToString();
                sp.Children.Add(img);
                sp.Children.Add(tb);

                // Set the StackPanel as the content of the listbox Item.
                lbi.Content = sp;

                //
                listBox1.Items.Add(lbi);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void wizardPage_Return(object sender, ReturnEventArgs<WizardResult> e)
        {
            CancelBackup();
            
            // If returning, wizard was completed (finished or canceled),
            // so continue returning to calling page
            OnReturn(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStopBackup_Click(object sender, RoutedEventArgs e)
        {
            CancelBackup();
        }

        /// <summary>
        /// Cancel the backup operation
        /// </summary>
        private void CancelBackup()
        {
            bw.CancelAsync();
            buttonStopBackup.IsEnabled = false;
        }
    }
}