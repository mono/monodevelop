// MultiTaskDialogProgressMonitor.cs: IProgressMonitor for displaying the 
//    progress of multiple tasks in a dialog. N.B. Much of this code for this 
//    dialog is adapted from MessageDialogProgressMonitor.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.ProgressMonitoring
{
	
	
	public class MultiTaskDialogProgressMonitor : ProgressMonitor 
	{
		StringCollection errorsMessages = new StringCollection ();
		StringCollection warningMessages = new StringCollection ();
		Exception errorException;
		MultiTaskProgressDialog dialog;
		bool showDetails;
		
		public MultiTaskDialogProgressMonitor (): this (true)
		{
		}
		
		public MultiTaskDialogProgressMonitor (bool showProgress): this (showProgress, true, true)
		{
		}
		
		public MultiTaskDialogProgressMonitor (bool showProgress, bool allowCancel): this (showProgress, allowCancel, true)
		{
		}
		
		public MultiTaskDialogProgressMonitor (bool showProgress, bool allowCancel, bool showDetails)
			: this (showProgress, allowCancel, showDetails, null)
		{
		}
		
		public MultiTaskDialogProgressMonitor (bool showProgress, bool allowCancel, bool showDetails, IDictionary<string, string> taskLabelAliases): base (Runtime.MainSynchronizationContext)
		{
			if (showProgress) {
				var parent = DesktopService.GetParentForModalWindow ();
				dialog = new MultiTaskProgressDialog (allowCancel, showDetails, taskLabelAliases) {
					DestroyWithParent = true,
					Modal = true,
					TransientFor = parent,
				};
				MessageService.PlaceDialog (dialog, parent);
				GtkWorkarounds.PresentWindowWithNotification (dialog);
				dialog.CancellationTokenSource = CancellationTokenSource;
				DispatchService.RunPendingEvents ();
				this.showDetails = showDetails;
			}
		}
		
		[AsyncDispatch]
		public void SetDialogTitle (string title)
		{
			if (dialog != null && !string.IsNullOrEmpty (title))
				dialog.Title = title;
		}
		
		[AsyncDispatch]
		public void SetOperationTitle (string title)
		{
			if (dialog != null && !string.IsNullOrEmpty (title))
				dialog.OperationTitle = title;
		}
		
		protected override void OnWriteLog (string text)
		{
			if (dialog != null) {
				dialog.WriteText (text);
				DispatchService.RunPendingEvents ();
			}
		}
		
		protected override void OnProgressChanged ()
		{
			if (dialog != null) {
				dialog.SetProgress (Progress);
			}
		}
		
		protected override void OnBeginTask (string name, int totalWork, int stepWork)
		{
			if (dialog != null) {
				dialog.BeginTask (name);
			}
			base.OnBeginTask (name, totalWork, stepWork);
		}

		protected override void OnEndTask (string name, int totalWork, int stepWork)
		{
			if (dialog != null) {
				dialog.EndTask ();
			}
			DispatchService.RunPendingEvents ();
			base.OnEndTask (name, totalWork, stepWork);
		}

		protected override void OnWarningReported (string message)
		{
			if (dialog != null) {
				dialog.WriteText (GettextCatalog.GetString ("WARNING: ") + message + "\n");
				DispatchService.RunPendingEvents ();
			}
			warningMessages.Add (message);
			base.OnWarningReported (message);
		}

		protected override void OnErrorReported (string message, Exception ex)
		{
			errorsMessages.Add (message);
			if (ex != null) {
				LoggingService.LogError (ex.ToString ());
				errorException = ex;
			}

			if (dialog != null) {
				dialog.WriteText (GettextCatalog.GetString ("ERROR: ") + ErrorHelper.GetErrorMessage (message, ex) + "\n");
				DispatchService.RunPendingEvents ();
			}
			base.OnErrorReported (message, ex);
		}
		
		protected override void OnCompleted ()
		{
			Runtime.RunInMainThread ((Action) ShowDialogs);
			base.OnCompleted ();
		}
		
		void ShowDialogs ()
		{
			if (dialog != null) {
				dialog.AllDone ();
			}
			
			if (showDetails)
				return;
			
			if (errorsMessages.Count > 0) {
				string s = "";
				foreach (string m in errorsMessages)
					s += m + "\n";
				MessageService.ShowError (s, errorException);
			}
			
			if (warningMessages.Count > 0) {
				string s = "";
				foreach (string m in warningMessages)
					s += m + "\n";
				MessageService.ShowWarning (s);
			}
		}
	}
}
