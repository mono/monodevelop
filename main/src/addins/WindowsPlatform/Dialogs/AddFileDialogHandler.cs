using System;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Platform;
using System.Windows.Forms;
using CustomControls.Controls;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{
    class AddFileDialogHandler: IAddFileDialogHandler
    {
        public bool Run (AddFileDialogData data)
        {
			var parentWindow = data.TransientFor ?? MessageService.RootWindow;
            CustomAddFilesDialog adlg = new CustomAddFilesDialog();
            adlg.StartLocation = AddonWindowLocation.Bottom;
            adlg.BuildActions = data.BuildActions;
            WinFormsRunner runner = new WinFormsRunner();
            bool result = false;
			
			SelectFileDialogHandler.SetCommonFormProperties (data, adlg.FileDialog);
			
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
					FilePath[] paths = new FilePath [adlg.FileDialog.FileNames.Length];
					for (int n=0; n<adlg.FileDialog.FileNames.Length; n++)
						paths [n] = adlg.FileDialog.FileNames [n];
                    data.SelectedFiles = paths;
                    data.OverrideAction = adlg.OverrideAction;
                    result = true;
                }
            }
            finally
            {
                t.Enabled = false;
                adlg.Dispose();
            }
			
			parentWindow.Present ();
            return result;
        }
    }
}
