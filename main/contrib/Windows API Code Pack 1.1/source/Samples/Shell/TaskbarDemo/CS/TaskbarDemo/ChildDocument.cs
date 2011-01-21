// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Reflection;

namespace TaskbarDemo
{
    public partial class ChildDocument : Form
    {
        // Keep a reference to the Taskbar instance
        private TaskbarManager windowsTaskbar = TaskbarManager.Instance;

        private JumpList childWindowJumpList;
        private string childWindowAppId;

        public ChildDocument(int count)
        {
            childWindowAppId = "TaskbarDemo.ChildWindow" + count;

            InitializeComponent();

            // Progress Bar
            foreach (string state in Enum.GetNames(typeof(TaskbarProgressBarState)))
                comboBoxProgressBarStates.Items.Add(state);

            //
            comboBoxProgressBarStates.SelectedItem = "NoProgress";

            this.Shown += new EventHandler(ChildDocument_Shown);

            HighlightOverlaySelection(labelNoIconOverlay);
        }

        void ChildDocument_Shown(object sender, EventArgs e)
        {
            // Set our default
            windowsTaskbar.SetProgressState(TaskbarProgressBarState.NoProgress, this.Handle);
        }

        #region Progress Bar

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            // When the user changes the trackBar value,
            // update the progress bar in our UI as well as Taskbar
            progressBar1.Value = trackBar1.Value;

            windowsTaskbar.SetProgressValue(trackBar1.Value, 100, this.Handle);
        }


        private void comboBoxProgressBarStates_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update the status of the taskbar progress bar

            TaskbarProgressBarState state = (TaskbarProgressBarState)(Enum.Parse(typeof(TaskbarProgressBarState), (string)comboBoxProgressBarStates.SelectedItem));

            windowsTaskbar.SetProgressState(state, this.Handle);

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
                    windowsTaskbar.SetProgressValue(trackBar1.Value, 100, this.Handle);
                    trackBar1.Enabled = true;
                    break;
                case TaskbarProgressBarState.Paused:
                    if (trackBar1.Value == 0)
                    {
                        trackBar1.Value = 20;
                        progressBar1.Value = trackBar1.Value;
                    }

                    progressBar1.Style = ProgressBarStyle.Continuous;
                    windowsTaskbar.SetProgressValue(trackBar1.Value, 100, this.Handle);
                    trackBar1.Enabled = true;
                    break;
                case TaskbarProgressBarState.Error:
                    if (trackBar1.Value == 0)
                    {
                        trackBar1.Value = 20;
                        progressBar1.Value = trackBar1.Value;
                    }

                    progressBar1.Style = ProgressBarStyle.Continuous;
                    windowsTaskbar.SetProgressValue(trackBar1.Value, 100, this.Handle);
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

        #endregion

        #region Icon Overlay

        private void HighlightOverlaySelection(Control ctlOverlay)
        {
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, labelNoIconOverlay);
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay1);
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay2);
            TaskbarDemoMainForm.CheckOverlaySelection(ctlOverlay, pictureIconOverlay3);
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

        private void buttonRefreshTaskbarList_Click(object sender, EventArgs e)
        {
            // Start from an empty list for user tasks
            childWindowJumpList.ClearAllUserTasks();

            // Path to Windows system folder
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);

            // Path to the Program Files folder
            string programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            // Path to Windows folder (if targeting .NET 4.0, can use Environment.SpecialFolder.Windows instead)
            string windowsFolder = Environment.GetEnvironmentVariable("windir");

            foreach (object item in listBox1.SelectedItems)
            {
                switch (item.ToString())
                {
                    case "Notepad":
                        childWindowJumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "notepad.exe"), "Open Notepad")
                        {
                            IconReference = new IconReference(Path.Combine(systemFolder, "notepad.exe"), 0)
                        });
                        break;
                    case "Calculator":
                        childWindowJumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "calc.exe"), "Open Calculator")
                        {
                            IconReference = new IconReference(Path.Combine(systemFolder, "calc.exe"), 0)
                        });
                        break;
                    case "Paint":
                        childWindowJumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "mspaint.exe"), "Open Paint")
                        {
                            IconReference = new IconReference(Path.Combine(systemFolder, "mspaint.exe"), 0)
                        });
                        break;
                    case "WordPad":
                        childWindowJumpList.AddUserTasks(new JumpListLink(Path.Combine(programFilesFolder, "Windows NT\\Accessories\\wordpad.exe"), "Open WordPad")
                        {
                            IconReference = new IconReference(Path.Combine(programFilesFolder, "Windows NT\\Accessories\\wordpad.exe"), 0)
                        });
                        break;
                    case "Windows Explorer":
                        childWindowJumpList.AddUserTasks(new JumpListLink(Path.Combine(windowsFolder, "explorer.exe"), "Open Windows Explorer")
                        {
                            IconReference = new IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0)
                        });
                        break;
                    case "Internet Explorer":
                        childWindowJumpList.AddUserTasks(new JumpListLink(Path.Combine(programFilesFolder, "Internet Explorer\\iexplore.exe"), "Open Internet Explorer")
                        {
                            IconReference = new IconReference(Path.Combine(programFilesFolder, "Internet Explorer\\iexplore.exe"), 0)
                        });
                        break;
                    case "Control Panel":
                        childWindowJumpList.AddUserTasks(new JumpListLink(((ShellObject)KnownFolders.ControlPanel).ParsingName, "Open Control Panel")
                        {
                            IconReference = new IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0)
                        });
                        break;
                    case "Documents Library":
                        if (ShellLibrary.IsPlatformSupported)
                        {
                            childWindowJumpList.AddUserTasks(new JumpListLink(KnownFolders.DocumentsLibrary.Path, "Open Documents Library")
                            {
                                IconReference = new IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0)
                            });
                        }
                        break;
                }
            }

            childWindowJumpList.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            childWindowJumpList = JumpList.CreateJumpListForIndividualWindow(childWindowAppId, this.Handle);

            ((Button)sender).Enabled = false;
            groupBoxCustomCategories.Enabled = true;
            buttonRefreshTaskbarList.Enabled = true;
        }
    }
}
