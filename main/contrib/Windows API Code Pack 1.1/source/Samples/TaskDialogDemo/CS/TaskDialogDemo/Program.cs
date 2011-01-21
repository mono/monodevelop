//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TaskDialogDemo
{
    static class Program
    {
        private static int MaxRange = 5000;

        // used by the event handlers incase they need to access the parent taskdialog
        private static TaskDialog currentTaskDialog = null;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            CreateTaskDialogDemo();
        }

        private static void CreateTaskDialogDemo()
        {
            TaskDialog taskDialogMain = new TaskDialog();
            taskDialogMain.Caption = "TaskDialog Samples";
            taskDialogMain.InstructionText = "Pick a sample to try:";
            taskDialogMain.FooterText = "Demo application as part of <a href=\"http://code.msdn.microsoft.com/WindowsAPICodePack\">Windows API Code Pack for .NET Framework</a>";
            taskDialogMain.Cancelable = true;

            // Enable the hyperlinks
            taskDialogMain.HyperlinksEnabled = true;
            taskDialogMain.HyperlinkClick += new EventHandler<TaskDialogHyperlinkClickedEventArgs>(taskDialogMain_HyperlinkClick);

            // Add a close button so user can close our dialog
            taskDialogMain.StandardButtons = TaskDialogStandardButtons.Close;

            #region Creating and adding command link buttons

            TaskDialogCommandLink buttonTestHarness = new TaskDialogCommandLink("test_harness", "TaskDialog Test Harness");
            buttonTestHarness.Click += new EventHandler(buttonTestHarness_Click);

            TaskDialogCommandLink buttonCommon = new TaskDialogCommandLink("common_buttons", "Common Buttons Sample");
            buttonCommon.Click += new EventHandler(buttonCommon_Click);

            TaskDialogCommandLink buttonElevation = new TaskDialogCommandLink("elevation", "Elevation Required Sample");
            buttonElevation.Click += new EventHandler(buttonElevation_Click);
            buttonElevation.UseElevationIcon = true;

            TaskDialogCommandLink buttonError = new TaskDialogCommandLink("error", "Error Sample");
            buttonError.Click += new EventHandler(buttonError_Click);

            TaskDialogCommandLink buttonIcons = new TaskDialogCommandLink("icons", "Icons Sample");
            buttonIcons.Click += new EventHandler(buttonIcons_Click);

            TaskDialogCommandLink buttonProgress = new TaskDialogCommandLink("progress", "Progress Sample");
            buttonProgress.Click += new EventHandler(buttonProgress_Click);

            TaskDialogCommandLink buttonProgressEffects = new TaskDialogCommandLink("progress_effects", "Progress Effects Sample");
            buttonProgressEffects.Click += new EventHandler(buttonProgressEffects_Click);

            TaskDialogCommandLink buttonTimer = new TaskDialogCommandLink("timer", "Timer Sample");
            buttonTimer.Click += new EventHandler(buttonTimer_Click);

            TaskDialogCommandLink buttonCustomButtons = new TaskDialogCommandLink("customButtons", "Custom Buttons Sample");
            buttonCustomButtons.Click += new EventHandler(buttonCustomButtons_Click);

            TaskDialogCommandLink buttonEnableDisable = new TaskDialogCommandLink("enableDisable", "Enable/Disable sample");
            buttonEnableDisable.Click += new EventHandler(buttonEnableDisable_Click);

            taskDialogMain.Controls.Add(buttonTestHarness);
            taskDialogMain.Controls.Add(buttonCommon);
            taskDialogMain.Controls.Add(buttonCustomButtons);
            taskDialogMain.Controls.Add(buttonEnableDisable);
            taskDialogMain.Controls.Add(buttonElevation);
            taskDialogMain.Controls.Add(buttonError);
            taskDialogMain.Controls.Add(buttonIcons);
            taskDialogMain.Controls.Add(buttonProgress);
            taskDialogMain.Controls.Add(buttonProgressEffects);
            taskDialogMain.Controls.Add(buttonTimer);

            #endregion

            // Show the taskdialog
            taskDialogMain.Show();
        }


        private static TaskDialogRadioButton enableDisableRadioButton = null;
        private static TaskDialogButton enableButton = null;
        private static TaskDialogButton disableButton = null;

        static void buttonEnableDisable_Click(object sender, EventArgs e)
        {
            // Enable/disable sample
            TaskDialog tdEnableDisable = new TaskDialog();
            tdEnableDisable.Cancelable = true;
            tdEnableDisable.Caption = "Enable/Disable Sample";
            tdEnableDisable.InstructionText = "Click on the buttons to enable or disable the radiobutton.";

            enableButton = new TaskDialogButton("enableButton", "Enable");
            enableButton.Default = true;
            enableButton.Click += new EventHandler(enableButton_Click);

            disableButton = new TaskDialogButton("disableButton", "Disable");
            disableButton.Click += new EventHandler(disableButton_Click);

            enableDisableRadioButton = new TaskDialogRadioButton("enableDisableRadioButton", "Sample Radio button");
            enableDisableRadioButton.Enabled = false;

            tdEnableDisable.Controls.Add(enableDisableRadioButton);
            tdEnableDisable.Controls.Add(enableButton);
            tdEnableDisable.Controls.Add(disableButton);

            TaskDialogResult tdr = tdEnableDisable.Show();

            enableDisableRadioButton = null;
            enableButton.Click -= new EventHandler(enableButton_Click);
            disableButton.Click -= new EventHandler(disableButton_Click);
            enableButton = null;
            disableButton = null;
        }

        static void disableButton_Click(object sender, EventArgs e)
        {
            if (enableDisableRadioButton != null)
                enableDisableRadioButton.Enabled = false;

            if (enableButton != null)
                enableButton.Enabled = true;

            if (disableButton != null)
                disableButton.Enabled = false;
        }

        static void enableButton_Click(object sender, EventArgs e)
        {
            if (enableDisableRadioButton != null)
                enableDisableRadioButton.Enabled = true;

            if (enableButton != null)
                enableButton.Enabled = false;

            if (disableButton != null)
                disableButton.Enabled = true;
        }

        private static TaskDialog tdCustomButtons = null;
        static void buttonCustomButtons_Click(object sender, EventArgs e)
        {
            // Custom buttons sample
            tdCustomButtons = new TaskDialog();
            tdCustomButtons.Cancelable = true;
            tdCustomButtons.Caption = "Custom Buttons Sample";
            tdCustomButtons.InstructionText = "Click on any of the custom buttons to get a specific message box";

            TaskDialogButton button1 = new TaskDialogButton("button1", "Custom Button 1");
            button1.Click += new EventHandler(button1_Click);
            button1.Default = true;
            tdCustomButtons.Controls.Add(button1);

            TaskDialogButton button2 = new TaskDialogButton("button2", "Custom Button 2");
            button2.Click += new EventHandler(button2_Click);
            tdCustomButtons.Controls.Add(button2);

            TaskDialogButton button3 = new TaskDialogButton("button3", "Custom Close Button");
            button3.Click += new EventHandler(button3_Click);
            tdCustomButtons.Controls.Add(button3);

            TaskDialogResult result = tdCustomButtons.Show();

            tdCustomButtons = null;
        }

        static void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Custom close button was clicked. Closing the dialog...", "Custom Buttons Sample");

            if (tdCustomButtons != null)
                tdCustomButtons.Close(TaskDialogResult.CustomButtonClicked);
        }

        static void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Custom button 2 was clicked", "Custom Buttons Sample");

        }

        static void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Custom button 1 was clicked", "Custom Buttons Sample");
        }

        static void taskDialogMain_HyperlinkClick(object sender, TaskDialogHyperlinkClickedEventArgs e)
        {
            // Launch the application associated with http links
            Process.Start(e.LinkText);
        }

        private static void buttonTestHarness_Click(object sender, EventArgs e)
        {
            TestHarness th = new TestHarness();
            th.ShowDialog();
        }

        private static void buttonCommon_Click(object sender, EventArgs e)
        {
            // Common buttons sample
            TaskDialog tdCommonButtons = new TaskDialog();
            tdCommonButtons.Cancelable = true;
            tdCommonButtons.Caption = "Common Buttons Sample";
            tdCommonButtons.InstructionText = "Click on any of the buttons to get a specific message box";

            tdCommonButtons.StandardButtons =
                TaskDialogStandardButtons.Ok |
                TaskDialogStandardButtons.Cancel |
                TaskDialogStandardButtons.Yes |
                TaskDialogStandardButtons.No |
                TaskDialogStandardButtons.Retry |
                TaskDialogStandardButtons.Cancel |
                TaskDialogStandardButtons.Close;

            TaskDialogResult tdr = tdCommonButtons.Show();

            MessageBox.Show(string.Format("The \"{0}\" button was clicked", tdr == TaskDialogResult.Ok ? "OK" : tdr.ToString()), "Common Buttons Sample");
        }

        private static TaskDialog tdElevation = null;
        private static void buttonElevation_Click(object sender, EventArgs e)
        {
            // Show a dialog with elevation button
            tdElevation = new TaskDialog();
            tdElevation.Cancelable = true;
            tdElevation.InstructionText = "Elevated task example";

            TaskDialogCommandLink adminTaskButton = new TaskDialogCommandLink("adminTaskButton", "Admin stuff", "Run some admin tasks");
            adminTaskButton.UseElevationIcon = true;
            adminTaskButton.Click += new EventHandler(adminTaskButton_Click);
            adminTaskButton.Default = true;

            tdElevation.Controls.Add(adminTaskButton);

            tdElevation.Show();

            tdElevation = null;
        }

        static void adminTaskButton_Click(object sender, EventArgs e)
        {
            if (tdElevation != null)
                tdElevation.Close(TaskDialogResult.Ok);
        }

        private static TaskDialog tdError = null;

        private static void buttonError_Click(object sender, EventArgs e)
        {
            // Error dialog
            tdError = new TaskDialog();
            tdError.DetailsExpanded = false;
            tdError.Cancelable = true;
            tdError.Icon = TaskDialogStandardIcon.Error;

            tdError.Caption = "Error Sample 1";
            tdError.InstructionText = "An unexpected error occured. Please send feedback now!";
            tdError.Text = "Error message goes here...";
            tdError.DetailsExpandedLabel = "Hide details";
            tdError.DetailsCollapsedLabel = "Show details";
            tdError.DetailsExpandedText = "Stack trace goes here...";

            tdError.FooterCheckBoxChecked = true;
            tdError.FooterCheckBoxText = "Don't ask me again";

            tdError.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandFooter;

            TaskDialogCommandLink sendButton = new TaskDialogCommandLink("sendButton", "Send Feedback\nI'm in a giving mood");
            sendButton.Click += new EventHandler(sendButton_Click);

            TaskDialogCommandLink dontSendButton = new TaskDialogCommandLink("dontSendButton", "No Thanks\nI don't feel like being helpful");
            dontSendButton.Click += new EventHandler(dontSendButton_Click);

            tdError.Controls.Add(sendButton);
            tdError.Controls.Add(dontSendButton);

            tdError.Show();

            tdError = null;
        }

        static void dontSendButton_Click(object sender, EventArgs e)
        {
            if (tdError != null)
                tdError.Close(TaskDialogResult.Ok);
        }

        static TaskDialogProgressBar sendFeedbackProgressBar;

        static void sendButton_Click(object sender, EventArgs e)
        {
            // Send feedback button
            TaskDialog tdSendFeedback = new TaskDialog();
            tdSendFeedback.Cancelable = true;

            tdSendFeedback.Caption = "Send Feedback Dialog";
            tdSendFeedback.Text = "Sending your feedback .....";

            // Show a progressbar
            sendFeedbackProgressBar = new TaskDialogProgressBar(0, MaxRange, 0);
            tdSendFeedback.ProgressBar = sendFeedbackProgressBar;

            // Subscribe to the tick event, so we can update the title/caption also close the dialog when done
            tdSendFeedback.Tick += new EventHandler<TaskDialogTickEventArgs>(tdSendFeedback_Tick);
            tdSendFeedback.Show();

            if (tdError != null)
                tdError.Close(TaskDialogResult.Ok);
        }

        static void tdSendFeedback_Tick(object sender, TaskDialogTickEventArgs e)
        {
            if (MaxRange >= e.Ticks)
            {
                ((TaskDialog)sender).InstructionText = string.Format("Sending ....{0}", e.Ticks);
                ((TaskDialog)sender).ProgressBar.Value = e.Ticks;
            }
            else
            {
                ((TaskDialog)sender).InstructionText = "Thanks for the feedback!";
                ((TaskDialog)sender).Text = "Our developers will get right on that...";
                ((TaskDialog)sender).ProgressBar.Value = MaxRange;
            }
        }

        private static void buttonIcons_Click(object sender, EventArgs e)
        {
            // Show icons on the taskdialog

            TaskDialog tdIcons = new TaskDialog();
            currentTaskDialog = tdIcons;
            tdIcons.Cancelable = true;

            tdIcons.Caption = "Icons Sample";
            tdIcons.InstructionText = "Main Instructions";
            tdIcons.FooterText = "Footer Text";

            TaskDialogRadioButton radioNone = new TaskDialogRadioButton("radioNone", "None");
            radioNone.Default = true; // default is no icons
            radioNone.Click += new EventHandler(iconsRadioButton_Click);

            TaskDialogRadioButton radioError = new TaskDialogRadioButton("radioError", "Error");
            radioError.Click += new EventHandler(iconsRadioButton_Click);

            TaskDialogRadioButton radioWarning = new TaskDialogRadioButton("radioWarning", "Warning");
            radioWarning.Click += new EventHandler(iconsRadioButton_Click);

            TaskDialogRadioButton radioInformation = new TaskDialogRadioButton("radioInformation", "Information");
            radioInformation.Click += new EventHandler(iconsRadioButton_Click);

            TaskDialogRadioButton radioShield = new TaskDialogRadioButton("radioShield", "Shield");
            radioShield.Click += new EventHandler(iconsRadioButton_Click);

            tdIcons.Controls.Add(radioNone);
            tdIcons.Controls.Add(radioError);
            tdIcons.Controls.Add(radioWarning);
            tdIcons.Controls.Add(radioInformation);
            tdIcons.Controls.Add(radioShield);

            tdIcons.Show();

            currentTaskDialog = null;
        }

        static void iconsRadioButton_Click(object sender, EventArgs e)
        {
            TaskDialogRadioButton radioButton = sender as TaskDialogRadioButton;

            if (radioButton != null && currentTaskDialog != null)
            {
                switch (radioButton.Name)
                {
                    case "radioNone":
                        currentTaskDialog.Icon = currentTaskDialog.FooterIcon = TaskDialogStandardIcon.None;
                        break;
                    case "radioError":
                        currentTaskDialog.Icon = currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Error;
                        break;
                    case "radioWarning":
                        currentTaskDialog.Icon = currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Warning;
                        break;
                    case "radioInformation":
                        currentTaskDialog.Icon = currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Information;
                        break;
                    case "radioShield":
                        currentTaskDialog.Icon = currentTaskDialog.FooterIcon = TaskDialogStandardIcon.Shield;
                        break;
                }
            }
        }

        static TaskDialogProgressBar progressTDProgressBar;

        private static void buttonProgress_Click(object sender, EventArgs e)
        {
            TaskDialog tdProgressSample = new TaskDialog();
            currentTaskDialog = tdProgressSample;
            tdProgressSample.Cancelable = true;
            tdProgressSample.Caption = "Progress Sample";

            progressTDProgressBar = new TaskDialogProgressBar(0, MaxRange, 0);
            tdProgressSample.ProgressBar = progressTDProgressBar;

            tdProgressSample.Tick += new EventHandler<TaskDialogTickEventArgs>(tdProgressSample_Tick);

            tdProgressSample.Show();

            currentTaskDialog = null;
        }

        static void tdProgressSample_Tick(object sender, TaskDialogTickEventArgs e)
        {
            if (MaxRange >= e.Ticks)
            {
                ((TaskDialog)sender).InstructionText = string.Format("Progress = {0}", e.Ticks);
                ((TaskDialog)sender).ProgressBar.Value = e.Ticks;
            }
            else
            {
                ((TaskDialog)sender).InstructionText = "Progress = Done";
                ((TaskDialog)sender).ProgressBar.Value = MaxRange;
            }
        }

        private static void buttonProgressEffects_Click(object sender, EventArgs e)
        {
            TaskDialog tdProgressEffectsSample = new TaskDialog();
            currentTaskDialog = tdProgressEffectsSample;
            tdProgressEffectsSample.Cancelable = true;
            tdProgressEffectsSample.Caption = "Progress Effects Sample";
            tdProgressEffectsSample.InstructionText = "Shows a dialog with Marquee style";

            TaskDialogProgressBar progressBarMarquee = new TaskDialogProgressBar();
            progressBarMarquee.State = TaskDialogProgressBarState.Marquee;

            tdProgressEffectsSample.ProgressBar = progressBarMarquee;

            tdProgressEffectsSample.Show();

            currentTaskDialog = null;
        }

        private static void buttonTimer_Click(object sender, EventArgs e)
        {
            // Timer example dialog
            TaskDialog tdTimer = new TaskDialog();
            tdTimer.Cancelable = true;
            tdTimer.Tick += new EventHandler<TaskDialogTickEventArgs>(tdTimer_Tick);

            tdTimer.Caption = "Timer Sample";
            tdTimer.InstructionText = "Time elapsed: 0 seconds";

            tdTimer.Show();
        }


        static void tdTimer_Tick(object sender, TaskDialogTickEventArgs e)
        {
            ((TaskDialog)sender).InstructionText = string.Format("Time elapsed: {0} seconds", e.Ticks / 1000);
        }
    }
}

