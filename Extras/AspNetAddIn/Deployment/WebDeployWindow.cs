// WebDeployWindow.cs
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
using System.Threading;
using Gtk;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Deployment;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using AspNetAddIn;

namespace MonoDevelop.AspNet.Deployment
{
	
	public partial class WebDeployWindow : Gtk.Dialog//, IProgressMonitor, IAsyncOperation
	{
		IList<WebDeployTarget> targets;
		AspNetAppProject project;
		DeployFileCollection deployFiles; 

		public WebDeployWindow (AspNetAppProject project, IList<WebDeployTarget> targets)
		{
			this.Build();
			this.targets = targets;
			this.project = project;
			deployFiles = project.GetDeployFiles ();
		}
		/*
		protected override void OnRealized ()
		{
			base.OnRealized ();
		}
		
		void LaunchDeployThread ()
		{
			Thread deployThread = new Thread (DeployThread);
			deployThread.Start ();
		}
		
		void DeployThread ()
		{
			IFileReplacePolicy replacePolicy = new MonoDevelop.Deployment.Gui.DialogFileReplacePolicy ();
			
			foreach (WebDeployTarget target in targets) {
				FileCopyHandler handler = target.FileCopier.Handler;
				//handler.CopyFiles (this, replacePolicy, target.FileCopier, deployFiles);
			}
		
#region IProgressMonitor
		
		ProgressTracker progressTracker = new ProgressTracker ();

		public void IProgressMonitor.ReportError (string message, System.Exception exception)
		{
		}

		public void IProgressMonitor.ReportSuccess (string message)
		{
		}

		public void IProgressMonitor.ReportWarning (string message)
		{
		}

		public void IProgressMonitor.Step (int work)
		{
			progressTracker.Step (work);
		}

		public void IProgressMonitor.EndTask ()
		{
			progressTracker.EndTask ();
		}

		public void IProgressMonitor.BeginStepTask (string name, int totalWork, int stepSize)
		{
			progressTracker.BeginStepTask (name, totalWork, stepSize);
		}

		public void IProgressMonitor.BeginTask (string name, int totalWork)
		{
			progressTracker.BeginTask (name, totalWork);
		}
		
		public System.IO.TextWriter IProgressMonitor.Log {
			get { return null; }
		}
		
		MonitorHandler IProgressMonitor_cancelRequested;
		event MonitorHandler IProgressMonitor.CancelRequested {
			add {
				lock(privateLock)
					IProgressMonitor_cancelRequested += value;
			}
			remove {
				lock (privateLock)
					IProgressMonitor_cancelRequested -= value;
			}
		}
		
		bool IProgressMonitor_cancelRequested = false;
		bool IProgressMonitor.IsCancelRequested {
			//FIXME
			get { return false; }
		}
		
		IAsyncOperation IProgressMonitor.AsyncOperation {
			get { return (IAsyncOperation) this; }
		}
		
		object privateLock = new object ();
		object IProgressMonitor.SyncRoot {
			get { return privateLock; }
		}
		
#endregion

#region IAsyncOperation
		void IAsyncOperation.WaitForCompleted ()
		{
		}

		void IAsyncOperation.Cancel ()
		{
		}
		
		event MonoDevelop.Core.OperationHandler IAsyncOperation.Completed;

		bool IAsyncOperation.IsCompleted {
			get {
			}
		}

		bool IAsyncOperation.Success {
			get {
			}
		}
		
#endregion*/
	}
}
