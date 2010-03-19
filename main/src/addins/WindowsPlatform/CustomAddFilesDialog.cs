using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using CustomControls.Controls;

namespace MonoDevelop.Platform
{
    public partial class CustomAddFilesDialog : OpenFileDialogEx
    {
        public string[] BuildActions;

        public CustomAddFilesDialog()
        {
            InitializeComponent();
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            foreach (string a in BuildActions)
            {
                comboActions.Items.Add(a);
            }
            comboActions.SelectedIndex = 0;
        }

        private void checkOverride_CheckedChanged(object sender, EventArgs e)
        {
            comboActions.Enabled = checkOverride.Checked;
        }

        public string OverrideAction
        {
            get
            {
                if (checkOverride.Checked)
                    return (string)comboActions.SelectedItem;
                else
                    return null;
            }
        }
    }
}
