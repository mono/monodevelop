//
// StatusProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.ProgressMonitoring;

namespace MonoDevelop.Ide.Gui
{
	internal class StatusProgressMonitor: BaseProgressMonitor
	{
		Gtk.Image icon;
		bool showErrorDialogs;
		bool showTaskTitles;
		bool lockGui;
		string title;
		StatusBarContext statusBar;
		Pad statusSourcePad;
		
		public StatusProgressMonitor (string title, string iconName, bool showErrorDialogs, bool showTaskTitles, bool lockGui, Pad statusSourcePad)
		{
			this.lockGui = lockGui;
			this.showErrorDialogs = showErrorDialogs;
			this.showTaskTitles = showTaskTitles;
			this.title = title;
			this.statusSourcePad = statusSourcePad;
			icon = ImageService.GetImage (iconName, Gtk.IconSize.Menu);
			statusBar = IdeApp.Workbench.StatusBar.CreateContext ();
			statusBar.StatusSourcePad = statusSourcePad;
			statusBar.BeginProgress (icon, title);
			if (lockGui)
				IdeApp.Workbench.LockGui ();
		}
		
		protected override void OnProgressChanged ()
		{
			if (showTaskTitles)
				statusBar.ShowMessage (icon, CurrentTask);
			if (!UnknownWork)
				statusBar.SetProgressFraction (GlobalWork);
			DispatchService.RunPendingEvents ();
		}
		
		public void UpdateStatusBar ()
		{
			if (showTaskTitles)
				statusBar.ShowMessage (icon, CurrentTask);
			else
				statusBar.ShowMessage (icon, title);
			if (!UnknownWork)
				statusBar.SetProgressFraction (GlobalWork);
			else
				statusBar.SetProgressFraction (0);
		}
		
		protected override void OnCompleted ()
		{
			if (lockGui)
				IdeApp.Workbench.UnlockGui ();
			
			statusBar.Dispose ();

			if (Errors.Count > 0 || Warnings.Count > 0) {
				if (Errors.Count > 0) {
					Gtk.Image img = ImageService.GetImage (Stock.Error, Gtk.IconSize.Menu);
					IdeApp.Workbench.StatusBar.ShowMessage (img, Errors [Errors.Count - 1]);
				} else if (SuccessMessages.Count == 0) {
					Gtk.Image img = ImageService.GetImage (Stock.Warning, Gtk.IconSize.Menu);
					IdeApp.Workbench.StatusBar.ShowMessage (img, Warnings [Warnings.Count - 1]);
				}
				
				base.OnCompleted ();
				
				if (showErrorDialogs) {
					MultiMessageDialog resultDialog = new MultiMessageDialog ();
					foreach (string m in Errors)
						resultDialog.AddError (m);
					foreach (string m in Warnings)
						resultDialog.AddWarning (m);
					resultDialog.TransientFor = IdeApp.Workbench.RootWindow;
					resultDialog.Run ();
					resultDialog.Destroy ();
				}
				IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
				return;
			}
			
			if (SuccessMessages.Count > 0)
				IdeApp.Workbench.StatusBar.ShowMessage (SuccessMessages [SuccessMessages.Count - 1]);
			
			IdeApp.Workbench.StatusBar.SetMessageSourcePad (statusSourcePad);
			base.OnCompleted ();
		}
	}
}
