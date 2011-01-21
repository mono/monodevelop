//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TaskbarDemo
{
    /// <summary>
    /// A word about known/custom categories.  In order for an application
    /// to have known/custom categories, a file type must be registered with
    /// that application.  This demo provides two menu items that allows you
    /// to register and unregister .txt files with this demo. By default
    /// shell displays the 'Recent' category for an application with a
    /// registered file type.
    /// 
    /// An exception will be thrown if you try to add a shell item to
    /// 'Custom Category 1' before registering a file type with this demo
    /// application.
    /// 
    /// Also, once a file type has been registered with this demo, setting
    /// jumpList.KnownCategoryToDisplay = KnownCategoryType.Neither will have
    /// no effect until at least one custom category or user task has been
    /// added to the taskbar jump list.
    /// </summary>    
    public partial class TaskbarDemoMainForm : Form
    {
        private const string appId = "TaskbarDemo";
        private const string progId = "TaskbarDemo";

        private JumpListCustomCategory category1 = new JumpListCustomCategory("Custom Category 1");
        private JumpListCustomCategory category2 = new JumpListCustomCategory("Custom Category 2");

        private JumpList jumpList;

        private string executableFolder;
        private readonly string executablePath;

        private TaskDialog td = null;

        // Keep a reference to the Taskbar instance
        private TaskbarManager windowsTaskbar = TaskbarManager.Instance;

        private int childCount = 0;

        #region Form Initialize
        
        public TaskbarDemoMainForm()
        {
            InitializeComponent();

            this.Shown += new EventHandler(TaskbarDemoMainForm_Shown);

            // Set the application specific id
            windowsTaskbar.ApplicationId = appId;

            // Save current folder and path of running executable
            executablePath = Assembly.GetEntryAssembly().Location;
            executableFolder = Path.GetDirectoryName(executablePath);

            // Sanity check - will avoid throwing exceptions if the file type is not registered.
            CheckFileRegistration();

            // Set our title if we were launched from the Taskbar
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length > 2 && args[1] == "/doc")
            {
                string fileName = string.Join(" ", args, 2, args.Length - 2);
                this.Text = string.Format("{0} - Taskbar Demo", Path.GetFileName(fileName));
            }
            else
                this.Text = "Taskbar Demo";

            HighlightOverlaySelection(labelNoIconOverlay);
        }

        void TaskbarDemoMainForm_Shown(object sender, EventArgs e)
        {
            // create a new taskbar jump list for the main window
            jumpList = JumpList.CreateJumpList();

            // Add custom categories
            jumpList.AddCustomCategories(category1, category2);

            // Default values for jump lists
            comboBoxKnownCategoryType.SelectedItem = "Recent";

            // Progress Bar
            foreach (string state in Enum.GetNames(typeof(TaskbarProgressBarState)))
                comboBoxProgressBarStates.Items.Add(state);

            //
            comboBoxProgressBarStates.SelectedItem = "NoProgress";

            // Update UI
            UpdateStatusBar("Application ready...");

            // Set our default
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
        }

        private void CheckFileRegistration()
        {
            bool registered = false;

            try
            {
                RegistryKey openWithKey = Registry.ClassesRoot.OpenSubKey(Path.Combine(".txt", "OpenWithProgIds"));
                string value = openWithKey.GetValue(progId, null) as string;

                if (value == null)
                    registered = false;
                else
                    registered = true;
            }
            finally
            {
                // Let the user know
                if (!registered)
                {
                    td = new TaskDialog();

                    td.Text = "File type is not registered";
                    td.InstructionText = "This demo application needs to register .txt files as associated files to properly execute the Taskbar related features.";
                    td.Icon = TaskDialogStandardIcon.Information;
                    td.Cancelable = true;

                    TaskDialogCommandLink button1 = new TaskDialogCommandLink("registerButton", "Register file type for this application",
                        "Register .txt files with this application to run this demo application correctly.");

                    button1.Click += new EventHandler(button1_Click);
                    // Show UAC shield as this task requires elevation
                    button1.UseElevationIcon = true;

                    td.Controls.Add(button1);

                    TaskDialogResult tdr = td.Show();
                }
            }
        }

        void button1_Click(object sender, EventArgs e)
        {
            registerFileTypeToolStripMenuItem_Click(null, EventArgs.Empty);
            td.Close();
        }

        #endregion

        #region File Registration Helpers

        private void registerFileTypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RegistrationHelper.RegisterFileAssociations(
                progId,
                false,
                appId,
                executablePath + " /doc %1",
                ".txt");
        }

        private void unregisterFileTypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RegistrationHelper.UnregisterFileAssociations(
                progId,
                false,
                appId,
                executablePath + " /doc %1",
                ".txt");
        }

        #endregion

        #region Menu Open/Close

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select a text document to load";
            dialog.Filters.Add(new CommonFileDialogFilter("Text files (*.txt)", "*.txt"));

            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                ReportUsage(dialog.FileName);
                Process.Start(executablePath, "/doc " + dialog.FileName);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonSaveFileDialog dialog = new CommonSaveFileDialog();
            dialog.Title = "Select where to save your file";
            dialog.Filters.Add(new CommonFileDialogFilter("Text files (*.txt)", "*.txt"));

            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                ReportUsage(dialog.FileName);
        }

        private void ReportUsage(string fileName)
        {
            // Report file usage to shell.  Note: The dialog box automatically
            // reports usage to shell, but it's still recommeneded that the user
            // explicitly calls AddToRecent. Shell will automatically handle
            // duplicate additions.
            JumpList.AddToRecent(fileName);

            UpdateStatusBar("File added to recent documents");
        }

        #endregion;

        #region Known Categories

        private void comboBoxKnownCategoryType_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBoxKnownCategoryType.SelectedItem as string)
            {
                case "None":
                    jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Neither;
                    break;
                case "Recent":
                    jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;
                    break;
                case "Frequent":
                    jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Frequent;
                    break;
            }
        }

        #endregion

        #region Custom Categories

        private int category1ItemsCount = 0;
        private int category2ItemsCount = 0;

        private void buttonCategoryOneAddLink_Click(object sender, EventArgs e)
        {
            category1ItemsCount++;

            // Specify path for shell item
            string path = String.Format("{0}\\test{1}.txt",
                executableFolder,
                category1ItemsCount);

            // Make sure this file exists
            EnsureFile(path);

            // Add shell item to custom category
            category1.AddJumpListItems(new JumpListItem(path));

            // Update status
            UpdateStatusBar(Path.GetFileName(path) + " added to 'Custom Category 1'");
        }

        private void buttonCategoryTwoAddLink_Click(object sender, EventArgs e)
        {
            category2ItemsCount++;

            // Specify path for file
            string path = String.Format("{0}\\test{1}.txt",
                executableFolder,
                category2ItemsCount);

            // Make sure this file exists
            EnsureFile(path);

            // Add jump list item to custom category
            category2.AddJumpListItems(new JumpListItem(path));

            // Update status
            UpdateStatusBar(Path.GetFileName(path) + " added to 'Custom Category 2'");
        }

        private void EnsureFile(string path)
        {
            if (File.Exists(path))
                return;

            // Simply create an empty file with the specified path
            FileStream fileStream = File.Create(path);
            fileStream.Close();
        }

        private void buttonUserTasksAddTasks_Click(object sender, EventArgs e)
        {
            // Path to Windows system folder
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);

            // Add our user tasks
            jumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "notepad.exe"), "Open Notepad")
            {
                IconReference = new IconReference(Path.Combine(systemFolder, "notepad.exe"), 0)
            });

            jumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "mspaint.exe"), "Open Paint")
            {
                IconReference = new IconReference(Path.Combine(systemFolder, "mspaint.exe"), 0)
            });

            jumpList.AddUserTasks(new JumpListSeparator());

            jumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "calc.exe"), "Open Calculator")
            {
                IconReference = new IconReference(Path.Combine(systemFolder, "calc.exe"), 0)
            });

            // Update status
            UpdateStatusBar("Three user tasks added to jump list");
        }

        private void buttonCategoryOneRename_Click(object sender, EventArgs e)
        {
            category1.Name = "Updated Category Name";
        }

        #endregion

        #region Progress Bar

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            // When the user changes the trackBar value,
            // update the progress bar in our UI as well as Taskbar
            progressBar1.Value = trackBar1.Value;

            TaskbarManager.Instance.SetProgressValue(trackBar1.Value, 100);
        }


        private void comboBoxProgressBarStates_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update the status of the taskbar progress bar

            TaskbarProgressBarState state = (TaskbarProgressBarState)(Enum.Parse(typeof(TaskbarProgressBarState), (string)comboBoxProgressBarStates.SelectedItem));

            windowsTaskbar.SetProgressState(state);

            // Update the application progress bar,
            // as well disable the trackbar in some cases
            switch (state)
            {
                case TaskbarProgressBarState.Normal:
                    if (trackBar1.Value == 0)
                    {
                        trackBar1.Value = 20;
                        progressBar1.Value = trackBar1.Value;
                    }

                    progressBar1.Style = ProgressBarStyle.Continuous;
                    windowsTaskbar.SetProgressValue(trackBar1.Value, 100);
                    trackBar1.Enabled = true;
                    break;
                case TaskbarProgressBarState.Paused:
                    if (trackBar1.Value == 0)
                    {
                        trackBar1.Value = 20;
                        progressBar1.Value = trackBar1.Value;
                    }

                    progressBar1.Style = ProgressBarStyle.Continuous;
                    windowsTaskbar.SetProgressValue(trackBar1.Value, 100);
                    trackBar1.Enabled = true;
                    break;
                case TaskbarProgressBarState.Error:
                    if (trackBar1.Value == 0)
                    {
                        trackBar1.Value = 20;
                        progressBar1.Value = trackBar1.Value;
                    }
                    
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    windowsTaskbar.SetProgressValue(trackBar1.Value , 100);
                    trackBar1.Enabled = true;
                    break;
                case TaskbarProgressBarState.Indeterminate:
                    progressBar1.Style = ProgressBarStyle.Marquee;
                    progressBar1.MarqueeAnimationSpeed = 30;
                    trackBar1.Enabled = false;
                    break;
                case TaskbarProgressBarState.NoProgress:
                    progressBar1.Value = 0;
                    trackBar1.Value = 0;
                    progressBar1.Style = ProgressBarStyle.Continuous;
                    trackBar1.Enabled = false;
                    break;
            }
        }

        #endregion;

        #region Icon Overlay

        private void HighlightOverlaySelection(Control ctlOverlay)
        {
            CheckOverlaySelection(ctlOverlay, labelNoIconOverlay);
            CheckOverlaySelection(ctlOverlay, pictureIconOverlay1);
            CheckOverlaySelection(ctlOverlay, pictureIconOverlay2);
            CheckOverlaySelection(ctlOverlay, pictureIconOverlay3);
        }

        internal static void CheckOverlaySelection(Control ctlOverlay, Label ctlCheck)
        {
            ctlCheck.BorderStyle = ctlCheck == ctlOverlay ? BorderStyle.Fixed3D : BorderStyle.None;
        }

        internal static void CheckOverlaySelection(Control ctlOverlay, PictureBox ctlCheck)
        {
            ctlCheck.BorderStyle = ctlCheck == ctlOverlay ? BorderStyle.Fixed3D : BorderStyle.None;
        }

        private void labelNoIconOverlay_Click(object sender, EventArgs e)
        {
            windowsTaskbar.SetOverlayIcon(this.Handle, null, null);
            HighlightOverlaySelection(labelNoIconOverlay);
        }

        private void pictureIconOverlay1_Click(object sender, EventArgs e)
        {
            windowsTaskbar.SetOverlayIcon(this.Handle, TaskbarDemo.Properties.Resources.Green, "Green");

            HighlightOverlaySelection(pictureIconOverlay1);
        }

        private void pictureIconOverlay2_Click(object sender, EventArgs e)
        {
            windowsTaskbar.SetOverlayIcon(this.Handle, TaskbarDemo.Properties.Resources.Yellow, "Yellow");

            HighlightOverlaySelection(pictureIconOverlay2);
        }

        private void pictureIconOverlay3_Click(object sender, EventArgs e)
        {
            windowsTaskbar.SetOverlayIcon(this.Handle, TaskbarDemo.Properties.Resources.Red, "Red");

            HighlightOverlaySelection(pictureIconOverlay3);
        }

        #endregion

        private void UpdateStatusBar(string status)
        {
            toolStripStatusLabel1.Text = status;
        }

        private void numericUpDownKnownCategoryLocation_ValueChanged(object sender, EventArgs e)
        {
            jumpList.KnownCategoryOrdinalPosition = Convert.ToInt32(numericUpDownKnownCategoryLocation.Value);
        }

        private void buttonRefreshTaskbarList_Click(object sender, EventArgs e)
        {
            jumpList.Refresh();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            childCount++;
            ChildDocument childWindow = new ChildDocument(childCount);
            childWindow.Text = string.Format("Child Document Window ({0})", childCount);
            childWindow.Show();
        }

    }
}