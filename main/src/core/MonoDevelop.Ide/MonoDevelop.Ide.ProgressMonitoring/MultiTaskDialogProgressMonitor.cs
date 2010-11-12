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

namespace MonoDevelop.Ide.ProgressMonitoring
{
	
	
	public class MultiTaskDialogProgressMonitor : BaseProgressMonitor 
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
		
		public MultiTaskDialogProgressMonitor (bool showProgress, bool allowCancel, bool showDetails, IDictionary<string, string> taskLabelAliases)
		{
			if (showProgress) {
				var parent = MessageService.GetDefaultModalParent ();
				dialog = new MultiTaskProgressDialog (allowCancel, showDetails, taskLabelAliases) {
					DestroyWithParent = true,
					Modal = true,
					TransientFor = parent,
				};
				MessageService.PlaceDialog (dialog, parent);
				dialog.Show ();
				dialog.AsyncOperation = AsyncOperation;
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
				dialog.SetProgress (CurrentTaskWork);
				DispatchService.RunPendingEvents ();
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
			DispatchService.RunPendingEvents ();
		}
						
		public override void ReportWarning (string message)
		{
			if (dialog != null) {
				dialog.WriteText (GettextCatalog.GetString ("WARNING: ") + message + "\n");
				DispatchService.RunPendingEvents ();
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
				LoggingService.LogError (ex.ToString ());
				errorException = ex;
			}
			
			if (dialog != null) {
				dialog.WriteText (GettextCatalog.GetString ("ERROR: ") + message + "\n");
				DispatchService.RunPendingEvents ();
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
				dialog.AllDone ();
			}
			
			if (showDetails)
				return;
			
			if (errorsMessages.Count > 0) {
				string s = "";
				foreach (string m in errorsMessages)
					s += m + "\n";
				MessageService.ShowException (errorException, s);
			}
			
			if (warningMessages.Count > 0) {
				string s = "";
				foreach (string m in warningMessages)
					s += m + "\n";
				MessageService.ShowError (s);
			}
		}
	}
}
