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
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;

			// Use the classic dialogs, as the new ones (WPF based) can't handle child controls.
			FileDialog.AutoUpgradeEnabled = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            foreach (string a in BuildActions)
            {
				if (a == "--") // Ignore separators for now
					continue;
                comboActions.Items.Add(a);
            }
            comboActions.SelectedIndex = 0;
        }

        // Need the dialog to be visible to access its controls coordinates.
        protected override void OnShow (EventArgs args)
        {
            base.OnShow (args);
            HorizontalLayout ();
        }

        void HorizontalLayout ()
        {
            var xOffset = FileNameLabelRect.X;
            checkOverride.Left += xOffset;
            comboActions.Left += xOffset;
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
