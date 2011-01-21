//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TaskDialogDemo
{
    public partial class TestHarness : Form
    {
        public TestHarness()
        {
            InitializeComponent();
        }

        private void cmdShow_Click(object sender, EventArgs e)
        {
            TaskDialog td = new TaskDialog();

            #region Button(s)

            TaskDialogStandardButtons button = TaskDialogStandardButtons.None;

            if (chkOK.Checked) button |= TaskDialogStandardButtons.Ok; 
            if (chkCancel.Checked) button |= TaskDialogStandardButtons.Cancel;

            if (chkYes.Checked) button |= TaskDialogStandardButtons.Yes;
            if (chkNo.Checked) button |= TaskDialogStandardButtons.No;

            if (chkClose.Checked) button |= TaskDialogStandardButtons.Close;
            if (chkRetry.Checked) button |= TaskDialogStandardButtons.Retry;

            #endregion

            #region Icon

            if (rdoError.Checked)
            {
                td.Icon = TaskDialogStandardIcon.Error;
            }
            else if (rdoInformation.Checked)
            {
                td.Icon = TaskDialogStandardIcon.Information;
            }
            else if (rdoShield.Checked)
            {
                td.Icon = TaskDialogStandardIcon.Shield;
            }
            else if (rdoWarning.Checked)
            {
                td.Icon = TaskDialogStandardIcon.Warning;
            }

            #endregion

            #region Prompts

            string title = txtTitle.Text;
            string instruction  = txtInstruction.Text;
            string content = txtContent.Text;

            #endregion

            td.StandardButtons = button;
            td.InstructionText = instruction;
            td.Caption = title;
            td.Text = content;
            td.OwnerWindowHandle = this.Handle;
            
            TaskDialogResult res = td.Show();

            this.resultLbl.Text = "Result = " + res.ToString();
        }
    }
}
