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
        volatile Form rootForm;

        public bool Run (AddFileDialogData data)
        {
            var parentWindow = data.TransientFor ?? MessageService.RootWindow;
            parentWindow.FocusInEvent += OnParentFocusIn;

            bool result = SelectFileDialogHandler.RunWinUIMethod (RunDialog, data);

            parentWindow.FocusInEvent -= OnParentFocusIn;
            parentWindow.Present ();

            return result;
        }

        void OnParentFocusIn (object o, EventArgs args)
        {
            if (rootForm != null)
                rootForm.BeginInvoke (new Action (() => rootForm.Activate ()));
        }

        bool RunDialog (AddFileDialogData data)
        {
			Application.EnableVisualStyles ();
			
            CustomAddFilesDialog adlg = new CustomAddFilesDialog();
            adlg.StartLocation = AddonWindowLocation.Bottom;
            adlg.BuildActions = data.BuildActions;
            bool result = false;
			
			SelectFileDialogHandler.SetCommonFormProperties (data, adlg.FileDialog);
			
            try
            {
                rootForm = new WinFormsRoot ();
                if (adlg.ShowDialog (rootForm) == DialogResult.Cancel)
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
                adlg.Dispose();
            }
			
            return result;
        }
    }
}
