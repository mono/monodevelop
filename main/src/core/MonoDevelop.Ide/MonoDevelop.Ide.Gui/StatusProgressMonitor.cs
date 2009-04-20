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


using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui
{
	internal class StatusProgressMonitor: BaseProgressMonitor
	{
		Gtk.Image icon;
		bool showErrorDialogs;
		bool showTaskTitles;
		bool lockGui;
		string title;
		
		static List<StatusProgressMonitor> monitorQueue = new List<StatusProgressMonitor> ();
		
		public StatusProgressMonitor (string title, string iconName, bool showErrorDialogs, bool showTaskTitles, bool lockGui)
		{
			this.lockGui = lockGui;
			this.showErrorDialogs = showErrorDialogs;
			this.showTaskTitles = showTaskTitles;
			this.title = title;
			icon = ImageService.GetImage (iconName, Gtk.IconSize.Menu);
			IdeApp.Workbench.StatusBar.BeginProgress (title);
			IdeApp.Workbench.StatusBar.ShowMessage (icon, title);
			if (lockGui)
				IdeApp.Workbench.LockGui ();
			monitorQueue.Add (this);
		}
		
		protected override void OnProgressChanged ()
		{
			if (monitorQueue [monitorQueue.Count - 1] != this)
				return;
			if (showTaskTitles)
				IdeApp.Workbench.StatusBar.ShowMessage (icon, CurrentTask);
			if (!UnknownWork)
				IdeApp.Workbench.StatusBar.SetProgressFraction (GlobalWork);
			DispatchService.RunPendingEvents ();
		}
		
		public void UpdateStatusBar ()
		{
			if (showTaskTitles)
				IdeApp.Workbench.StatusBar.ShowMessage (icon, CurrentTask);
			else
				IdeApp.Workbench.StatusBar.ShowMessage (icon, title);
			if (!UnknownWork)
				IdeApp.Workbench.StatusBar.SetProgressFraction (GlobalWork);
			else
				IdeApp.Workbench.StatusBar.SetProgressFraction (0);
		}
		
		protected override void OnCompleted ()
		{
			if (lockGui)
				IdeApp.Workbench.UnlockGui ();
			
			int i = monitorQueue.IndexOf (this);
			bool uniqueMonitor = monitorQueue.Count == 1;
			
			if (uniqueMonitor)
				IdeApp.Workbench.StatusBar.EndProgress ();
			else if (i == monitorQueue.Count - 1)
				monitorQueue [i - 1].UpdateStatusBar ();
			
			monitorQueue.RemoveAt (i);

			if (Errors.Count > 0 || Warnings.Count > 0) {
				if (uniqueMonitor) {
					if (Errors.Count > 0) {
						Gtk.Image img = ImageService.GetImage (Stock.Error, Gtk.IconSize.Menu);
						IdeApp.Workbench.StatusBar.ShowMessage (img, Errors [Errors.Count - 1]);
					} else if (SuccessMessages.Count == 0) {
						Gtk.Image img = ImageService.GetImage (Stock.Warning, Gtk.IconSize.Menu);
						IdeApp.Workbench.StatusBar.ShowMessage (img, Warnings [Warnings.Count - 1]);
					}
				}
				
				base.OnCompleted ();
				
				if (showErrorDialogs) {
					MultiMessageDialog resultDialog = new MultiMessageDialog ();
					foreach (string m in Errors)
						resultDialog.AddError (m);
					foreach (string m in Warnings)
						resultDialog.AddWarning (m);
					resultDialog.Run ();
					resultDialog.Destroy ();
				}
				return;
			}

			if (uniqueMonitor) {
				if (SuccessMessages.Count > 0)
					IdeApp.Workbench.StatusBar.ShowMessage (SuccessMessages [SuccessMessages.Count - 1]);
				else
					IdeApp.Workbench.StatusBar.ShowReady ();
			}
			
			base.OnCompleted ();
		}
	}
}
