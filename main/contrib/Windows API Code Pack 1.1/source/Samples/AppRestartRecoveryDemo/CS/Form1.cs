// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Windows.Forms;
using System.Diagnostics;
using System;
using System.Threading;
using System.Timers;
using Microsoft.WindowsAPICodePack.Shell;
using System.IO;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Microsoft.WindowsAPICodePack.Samples.AppRestartRecoveryDemo
{
    public partial class Form1 : Form
    {
        private static string AppTitle = "Application Restart/Recovery Demo"; 
        private static FileSettings CurrentFile = new FileSettings();
        private static string RecoveryFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "AppRestartRecoveryDemoData.xml");
        private static string DataSeparatorString = "@@@@@@@@@@";

        //
        private bool internalLoad = false;
        private bool recovered = false;
        private DateTime startTime;

        public Form1()
        {
            Debug.WriteLine("ARR: Demo started");

            InitializeComponent();

            
            Form1.CurrentFile.IsDirty = false;

            UpdateAppTitle();

            RegisterForRestart();           
            RegisterForRecovery();


            statusLabel.Text = "Application successfully registered for restart / recovery. Wait 60s before crashing the application.";

            // SetupTimerNotifyForRestart sets a timer to 
            // beep when 60 seconds have elapsed, indicating that
            // WER will restart the program after a crash.
            // WER will not restart applications that crash
            // within 60 seconds of startup.
            SetupTimerNotifyForRestart();

            // If we started with /restart command line argument 
            // then we were automatically restarted and should
            // try to resume the previous session.
            if (System.Environment.GetCommandLineArgs().Length > 1 && System.Environment.GetCommandLineArgs()[1] == "/restart")
            {
                recovered = true;
                RecoverLastSession(System.Environment.GetCommandLineArgs()[1]);
            }
        }

        private void SetupTimerNotifyForRestart()
        {
            // Beep when 60 seconds has elapsed.
            System.Timers.Timer notify = new System.Timers.Timer(60000);
            notify.Elapsed += new ElapsedEventHandler(NotifyUser);
            notify.AutoReset = false; // Only beep once.
            notify.Enabled = true;
        }

        private void NotifyUser(object source, ElapsedEventArgs e)
        {
            statusLabel.Text = "It is \"safe\" to crash now! (click App Restart Recovery->Crash!)";
        }

        private void Crash()
        {
            Environment.FailFast("ARR Demo intentional crash.");
        }

        private void RegisterForRestart()
        {
            // Register for automatic restart if the 
            // application was terminated for any reason
            // other than a system reboot or a system update.
            ApplicationRestartRecoveryManager.RegisterForApplicationRestart(
                new RestartSettings("/restart", RestartRestrictions.NotOnReboot | RestartRestrictions.NotOnPatch));

            Debug.WriteLine("ARR: Registered for restart");
        }

        private void RegisterForRecovery()
        {
            // Don't pass any state. We'll use our static variable "CurrentFile" to determine
            // the current state of the application.
            // Since this registration is being done on application startup, we don't have a state currently.
            // In some cases it might make sense to pass this initial state.
            // Another approach: When doing "auto-save", register for recovery everytime, and pass
            // the current state at that time. 
            RecoveryData data = new RecoveryData(new RecoveryCallback(RecoveryProcedure), null);
            RecoverySettings settings = new RecoverySettings(data, 0);

            ApplicationRestartRecoveryManager.RegisterForApplicationRecovery(settings);

            Debug.WriteLine("ARR: Registered for recovery");
        }

        // This method is invoked by WER. 
        private int RecoveryProcedure(object state)
        {
            Debug.WriteLine("ARR: Recovery procedure called!!!");

            PingSystem();

            // Do recovery work here.
            // Signal to WER that the recovery
            // is still in progress.
            
            // Write the contents of the file, as well as some other data that we need
            File.WriteAllText(RecoveryFile, string.Format("{1}{0}{2}{0}{3}", DataSeparatorString, CurrentFile.Filename, CurrentFile.IsDirty, CurrentFile.Contents));

            Debug.WriteLine("File path: " + RecoveryFile);
            Debug.WriteLine("File exists: " + File.Exists(RecoveryFile));
            Debug.WriteLine("Application shutting down...");

            ApplicationRestartRecoveryManager.ApplicationRecoveryFinished(true);
            return 0;
        }

        // This method is called periodically to ensure
        // that WER knows that recovery is still in progress.
        private void PingSystem()
        {
            // Find out if the user canceled recovery.
            bool isCanceled = ApplicationRestartRecoveryManager.ApplicationRecoveryInProgress();

            if (isCanceled)
            {
                Console.WriteLine("Recovery has been canceled by user.");   
                Environment.Exit(2);
            }
        }

        // This method gets called by main when the 
        // commandline arguments indicate that this
        // application was automatically restarted 
        // by WER.
        private void RecoverLastSession(string command)
        {
            if (!File.Exists(RecoveryFile))
            {
                MessageBox.Show(this, string.Format("Recovery file {0} does not exist", RecoveryFile));
                internalLoad = true;
                textBox1.Text = "Could not recover the data. Recovery data file does not exist";
                internalLoad = false;
                UpdateAppTitle();
                return;
            }

            // Perform application state restoration 
            // actions here.
            string contents = File.ReadAllText(RecoveryFile);

            CurrentFile.Filename = contents.Remove(contents.IndexOf(Form1.DataSeparatorString));

            contents = contents.Remove(0, contents.IndexOf(Form1.DataSeparatorString) + Form1.DataSeparatorString.Length);

            CurrentFile.IsDirty = contents.Remove(contents.IndexOf(Form1.DataSeparatorString)) == "True" ? true : false;

            contents = contents.Remove(0, contents.IndexOf(Form1.DataSeparatorString) + Form1.DataSeparatorString.Length);

            CurrentFile.Contents = contents;

            // Load our textbox
            textBox1.Text = CurrentFile.Contents;

            // Update the title
            UpdateAppTitle();

            // Reset our variable so next title updates we don't show the "recovered" text
            recovered = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PromptForSave())
            {
                Application.Exit();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PromptForSave())
            {
                CommonOpenFileDialog cfd = new CommonOpenFileDialog();
                cfd.Filters.Add(new CommonFileDialogFilter("Text files", ".txt"));

                CommonFileDialogResult result = cfd.ShowDialog();

                if (result == CommonFileDialogResult.Ok)
                {
                    internalLoad = true;
                    Form1.CurrentFile.Load(cfd.FileName);
                    textBox1.Text = CurrentFile.Contents;
                    internalLoad = false;

                    UpdateAppTitle();
                }
            }
        }

        private bool PromptForSave()
        {
            if (!CurrentFile.IsDirty)
            {
                return true;
            }

            // ask the user to save.
            DialogResult dr = MessageBox.Show(this, "Current document has changed. Would you like to save?", "Save current document", MessageBoxButtons.YesNoCancel);

            if (dr == DialogResult.Cancel)
            {
                return false;
            }

            if (dr == DialogResult.Yes)
            {
                // Does the current file have a name?
                if (string.IsNullOrEmpty(Form1.CurrentFile.Filename))
                {
                    CommonSaveFileDialog saveAsCFD = new CommonSaveFileDialog();
                    saveAsCFD.Filters.Add(new CommonFileDialogFilter("Text files", ".txt"));
                    saveAsCFD.AlwaysAppendDefaultExtension = true;

                    if (saveAsCFD.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        Form1.CurrentFile.Save(saveAsCFD.FileName);
                        UpdateAppTitle();
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // just save it
                    Form1.CurrentFile.Save(CurrentFile.Filename);
                    UpdateAppTitle();
                }
            }

            return true;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!internalLoad && Form1.CurrentFile != null)
            {
                Form1.CurrentFile.IsDirty = true;
                Form1.CurrentFile.Contents = textBox1.Text;
                UpdateAppTitle();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Does the current file have a name?
            if (string.IsNullOrEmpty(Form1.CurrentFile.Filename))
            {
                CommonSaveFileDialog saveAsCFD = new CommonSaveFileDialog();
                saveAsCFD.Filters.Add(new CommonFileDialogFilter("Text files", ".txt"));

                if (saveAsCFD.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Form1.CurrentFile.Save(saveAsCFD.FileName);
                    UpdateAppTitle();
                }
                else
                    return;
            }
            else
            {
                // just save it
                Form1.CurrentFile.Save(Form1.CurrentFile.Filename);
                UpdateAppTitle();
            }
        }

        private void UpdateAppTitle()
        {
            string dirtyState = Form1.CurrentFile.IsDirty ? "*" : "";
            string filename = string.IsNullOrEmpty(Form1.CurrentFile.Filename) ?
                "Untitled" : Path.GetFileName(Form1.CurrentFile.Filename);

            this.Text = string.Format("{0}{1} - {2}", filename, dirtyState, AppTitle);

            if (recovered)
                this.Text += " (RECOVERED FROM CRASH)";
        }

        private void crashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Crash();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Undo();
        }

        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Cut();
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Copy();
        }

        private void pasteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Paste();
        }

        private void selectAllToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.SelectAll();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Application Restart and Recovery demo", "Windows API Code Pack for .NET Framework");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan span = DateTime.Now - startTime;
            timerLabel.Text = string.Format("App running for {0}s", (int)span.TotalSeconds);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            startTime = DateTime.Now;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PromptForSave())
            {
                textBox1.Clear();
                CurrentFile = new FileSettings();
                CurrentFile.IsDirty = false;
                UpdateAppTitle();
            }
        }
    }
}
