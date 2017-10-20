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
using MonoDevelop.Core;
using MonoDevelop.Ide.Status;

namespace MonoDevelop.Ide.Gui
{
	internal class StatusProgressMonitor: ProgressMonitor
	{
		string icon;
		bool showErrorDialogs;
		bool showTaskTitles;
		bool lockGui;
		bool showCancelButton;
		string title;
		StatusMessageContext statusContext;
		Pad statusSourcePad;
		
		public StatusProgressMonitor (string title, string iconName, bool showErrorDialogs, bool showTaskTitles, bool lockGui, Pad statusSourcePad, bool showCancelButton): base (Runtime.MainSynchronizationContext)
		{

			this.lockGui = lockGui;
			this.showErrorDialogs = showErrorDialogs;
			this.showTaskTitles = showTaskTitles;
			this.title = title;
			this.statusSourcePad = statusSourcePad;
			this.showCancelButton = showCancelButton;
			icon = iconName;
			statusContext = StatusService.CreateContext ();
			statusContext.StatusSourcePad = statusSourcePad;
			if (showCancelButton)
				statusContext.CancellationTokenSource = CancellationTokenSource;
			statusContext.BeginProgress (iconName, title);
			if (lockGui)
				IdeApp.Workbench.LockGui ();
		}
		
		protected override void OnProgressChanged ()
		{
			if (showTaskTitles)
				statusContext.ShowMessage (icon, CurrentTaskName);
			if (!ProgressIsUnknown) {
				statusContext.SetProgressFraction (Progress);
				DesktopService.SetGlobalProgress (Progress);
			} else
				DesktopService.ShowGlobalProgressIndeterminate ();
		}
		
		public void UpdateStatusBar ()
		{
			if (showTaskTitles)
				statusContext.ShowMessage (icon, CurrentTaskName);
			else
				statusContext.ShowMessage (icon, title);
			if (!ProgressIsUnknown)
				statusContext.SetProgressFraction (Progress);
			else
				statusContext.SetProgressFraction (0);
		}
		
		protected override void OnCompleted ()
		{
			if (lockGui)
				IdeApp.Workbench.UnlockGui ();

			// We want any errors to remain on the statusbar,
			// so dispose the context here and use the main context
			statusContext.EndProgress ();
			statusContext.Dispose ();

			try {
				if (Errors.Length > 0 || Warnings.Length > 0) {
					if (Errors.Length > 0) {
						StatusService.MainContext.ShowError (Errors [Errors.Length - 1].DisplayMessage);
					} else if (SuccessMessages.Length == 0) {
						StatusService.MainContext.ShowWarning (Warnings [Warnings.Length - 1]);
					}

					DesktopService.ShowGlobalProgressError ();

					base.OnCompleted ();

					if (!CancellationToken.IsCancellationRequested && showErrorDialogs)
						this.ShowResultDialog ();
					return;
				}

				if (SuccessMessages.Length > 0)
					StatusService.MainContext.ShowMessage (MonoDevelop.Ide.Gui.Stock.StatusSuccess, SuccessMessages [SuccessMessages.Length - 1]);

			} finally {
				StatusService.MainContext.StatusSourcePad = statusSourcePad;
			}

			DesktopService.SetGlobalProgress (Progress);

			base.OnCompleted ();
		}
	}
}
