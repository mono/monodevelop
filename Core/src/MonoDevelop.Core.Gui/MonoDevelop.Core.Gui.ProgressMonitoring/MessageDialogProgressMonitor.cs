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
using System.Collections.Specialized;
using System.IO;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Core.Gui.ProgressMonitoring
{
	// Progress monitor that reports errors and warnings in message dialogs.
	
	public class MessageDialogProgressMonitor: BaseProgressMonitor
	{
		StringCollection errorsMessages = new StringCollection ();
		StringCollection warningMessages = new StringCollection ();
		Exception errorException;
		ProgressDialog dialog;
		
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
		{
			if (showProgress) {
				dialog = new ProgressDialog (allowCancel, showDetails);
				dialog.Message = "";
				dialog.Show ();
				dialog.AsyncOperation = AsyncOperation;
				Services.DispatchService.RunPendingEvents ();
			}
		}
		
		protected override void OnWriteLog (string text)
		{
			if (dialog != null) {
				dialog.WriteText (text);
				Services.DispatchService.RunPendingEvents ();
			}
		}
		
		protected override void OnProgressChanged ()
		{
			if (dialog != null) {
				dialog.Message = CurrentTask;
				dialog.Progress = GlobalWork;
				Services.DispatchService.RunPendingEvents ();
			}
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			base.BeginTask (name, totalWork);
			if (dialog != null) {
				dialog.BeginTask (name);
				Services.DispatchService.RunPendingEvents ();
			}
		}
		
		public override void BeginStepTask (string name, int totalWork, int stepSize)
		{
			base.BeginStepTask (name, totalWork, stepSize);
			if (dialog != null) {
				dialog.BeginTask (name);
				Services.DispatchService.RunPendingEvents ();
			}
		}
		
		public override void EndTask ()
		{
			base.EndTask ();
			if (dialog != null) {
				dialog.EndTask ();
				Services.DispatchService.RunPendingEvents ();
			}
		}
						
		public override void ReportWarning (string message)
		{
			if (dialog != null) {
				dialog.WriteText ("WARNING: " + message);
				Services.DispatchService.RunPendingEvents ();
			}
			warningMessages.Add (message);
		}
		
		public override void ReportError (string message, Exception ex)
		{
			if (message == null && ex != null)
				message = ex.Message;
			else if (message != null && ex != null) {
				if (!message.EndsWith (".")) message += ".";
				message += " " + ex.Message;
			}
			
			errorsMessages.Add (message);
			if (ex != null) {
				Runtime.LoggingService.Error (ex);
				errorException = ex;
			}
			
			if (dialog != null) {
				dialog.WriteText ("ERROR: " + message);
				Services.DispatchService.RunPendingEvents ();
			}
		}
		
		protected override void OnCompleted ()
		{
			if (dialog != null) {
				dialog.Dispose ();
			}
			Services.DispatchService.GuiDispatch (new MessageHandler (ShowDialogs));
			base.OnCompleted ();
		}
		
		void ShowDialogs ()
		{
			if (errorsMessages.Count > 0) {
				string s = "";
				foreach (string m in errorsMessages)
					s += m + "\n";
				Services.MessageService.ShowError (errorException, s);
			}
			
			if (warningMessages.Count > 0) {
				string s = "";
				foreach (string m in warningMessages)
					s += m + "\n";
				Services.MessageService.ShowWarning (s);
			}
		}
	}
}
