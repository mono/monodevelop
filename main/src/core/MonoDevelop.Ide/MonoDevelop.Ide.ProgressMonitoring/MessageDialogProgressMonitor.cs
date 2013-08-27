//
// MessageDialogProgressMonitor.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.ProgressMonitoring
{
	// Progress monitor that reports errors and warnings in message dialogs.
	
	public class MessageDialogProgressMonitor: BaseProgressMonitor
	{
		ProgressDialog dialog;
		bool hideWhenDone;
		bool showDetails;
		
		public MessageDialogProgressMonitor (): this (false)
		{
		}
		
		public MessageDialogProgressMonitor (bool showProgress): this (showProgress, true, true)
		{
		}
		
		public MessageDialogProgressMonitor (bool showProgress, bool allowCancel): this (showProgress, allowCancel, true)
		{
		}
		
		public MessageDialogProgressMonitor (bool showProgress, bool allowCancel, bool showDetails)
		: this (showProgress, allowCancel, showDetails, true)
		{
		}
		
		public MessageDialogProgressMonitor (bool showProgress, bool allowCancel, bool showDetails, bool hideWhenDone)
		{
			if (showProgress) {
				dialog = new ProgressDialog (MessageService.RootWindow, allowCancel, showDetails);
				dialog.Message = "";
				MessageService.PlaceDialog (dialog, MessageService.RootWindow);
				dialog.Show ();
				dialog.AsyncOperation = AsyncOperation;
				dialog.OperationCancelled += delegate {
					OnCancelRequested ();
				};
				RunPendingEvents ();
				this.hideWhenDone = hideWhenDone;
				this.showDetails = showDetails;
			}
		}
		
		protected override void OnWriteLog (string text)
		{
			if (dialog != null) {
				dialog.WriteText (text);
				RunPendingEvents ();
			}
		}
		
		protected override void OnProgressChanged ()
		{
			if (dialog != null) {
				dialog.Message = CurrentTask;
				dialog.Progress = GlobalWork;
				RunPendingEvents ();
			}
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			if (dialog != null) {
				dialog.BeginTask (name);
			}
			base.BeginTask (name, totalWork);
		}
		
		public override void BeginStepTask (string name, int totalWork, int stepSize)
		{
			if (dialog != null) {
				dialog.BeginTask (name);
			}
			base.BeginStepTask (name, totalWork, stepSize);
		}
		
		public override void EndTask ()
		{
			if (dialog != null) {
				dialog.EndTask ();
			}
			base.EndTask ();
			RunPendingEvents ();
		}
						
		public override void ReportWarning (string message)
		{
			base.ReportWarning (message);
			if (dialog != null) {
				dialog.WriteText (GettextCatalog.GetString ("WARNING: ") + message + "\n");
				RunPendingEvents ();
			}
		}
		
		public override void ReportError (string message, Exception ex)
		{
			base.ReportError (message, ex);
			
			if (dialog != null) {
				dialog.WriteText (GettextCatalog.GetString ("ERROR: ") + Errors [Errors.Count - 1] + "\n");
				RunPendingEvents ();
			}
		}
		
		protected override void OnCompleted ()
		{
			DispatchService.GuiDispatch (new MessageHandler (ShowDialogs));
			base.OnCompleted ();
		}

		void ShowDialogs ()
		{
			if (dialog != null) {
				dialog.ShowDone (Warnings.Count > 0, Errors.Count > 0);
				if (hideWhenDone)
					dialog.Destroy ();
			}
			
			if (showDetails)
				return;
			
			ShowResultDialog ();
		}
	}
}
