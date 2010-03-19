using System;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Platform;
using System.Windows.Forms;
using CustomControls.Controls;

namespace MonoDevelop.Platform
{
    class AddFileDialogHandler: IAddFileDialogHandler
    {
        public bool Run (AddFileDialogData data)
        {
            CustomAddFilesDialog adlg = new CustomAddFilesDialog();
            adlg.StartLocation = AddonWindowLocation.Bottom;
            adlg.OpenDialog.InitialDirectory = data.CurrentFolder;
            adlg.OpenDialog.AddExtension = true;
            adlg.BuildActions = data.BuildActions;
//            adlg.OpenDialog.Filter = "Image Files(*.bmp;*.jpg;*.gif;*.png)|*.bmp;*.jpg;*.gif;*.png";
            WinFormsRunner runner = new WinFormsRunner();
            bool result = false;

            Timer t = new Timer();
            t.Interval = 20;
            try
            {
                t.Tick += delegate
                {
//                    MonoDevelop.Core.Gui.DispatchService.RunPendingEvents();
                };
                //t.Enabled = true;
                WinFormsRoot root = new WinFormsRoot();
                if (adlg.ShowDialog(root) == DialogResult.Cancel)
                    result = false;
                else
                {
                    data.SelectedFiles = adlg.OpenDialog.FileNames;
                    data.OverrideAction = adlg.OverrideAction;
                    result = true;
                }
            }
            finally
            {
                t.Enabled = false;
                adlg.Dispose();
            }

            return result;
        }
    }
}
