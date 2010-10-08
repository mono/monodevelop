// 
// MonoDroidUtility.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using System;

namespace MonoDevelop.MonoDroid
{
	public static class MonoDroidUtility
	{
		public static IAsyncOperation Upload (AndroidDevice device, FilePath packageFile, string packageName,
			IAsyncOperation signingOperation)
		{
			var toolbox = MonoDroidFramework.Toolbox;
			
			var monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Deploy to Device"), MonoDevelop.Ide.Gui.Stock.RunProgramIcon, true, true);
			
			List<string> packages = null;
			
			var chop = new ChainedAsyncOperationSequence (monitor,
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Starting adb server"),
					Create = () => toolbox.EnsureServerRunning (monitor.Log, monitor.Log),
					Error = GettextCatalog.GetString ("Failed to start adb server")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Waiting for device"),
					Create = () => toolbox.WaitForDevice (device, monitor.Log, monitor.Log),
					Error = GettextCatalog.GetString ("Failed to get device")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Getting package list from device"),
					Create = () => toolbox.GetInstalledPackagesOnDevice (device, monitor.Log),
					Completed = (IAsyncOperation op) => {
						packages = ((AndroidToolbox.GetPackagesOperation)op).Result;
					},
					Error = GettextCatalog.GetString ("Failed to get package list")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Installing shared runtime package on device"),
					Condition = () => !toolbox.IsSharedRuntimeInstalled (packages),
					Create = () => {
						var pkg = MonoDroidFramework.SharedRuntimePackage;
						if (!File.Exists (pkg)) {
							var msg = GettextCatalog.GetString ("Could not find shared runtime package file");
							monitor.ReportError (msg, null);
							LoggingService.LogError ("{0} '{1}'", msg, pkg);
							return null;
						}
						return toolbox.Install (device, pkg, monitor.Log, monitor.Log);
					},
					Error = GettextCatalog.GetString ("Failed to install shared runtime package")
				},
				//TODO: use per-device flag file and installed packages list to skip re-installing unchanged packages
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Uninstalling old version of package"),
					Condition = () => packages.Contains ("package:" + packageName),
					Create = () => toolbox.Uninstall (device, packageName, monitor.Log, monitor.Log),
					Error = GettextCatalog.GetString ("Failed to uninstall package")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Waiting for packaging signing to complete"),
					Condition = () => signingOperation != null && !signingOperation.IsCompleted,
					Create = () => signingOperation,
					Error = GettextCatalog.GetString ("Package signing failed"),
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Installing package"),
					Create = () => toolbox.Install (device, packageFile, monitor.Log, monitor.Log),
					Error = GettextCatalog.GetString ("Failed to install package")
				}
			);
			chop.Completed += delegate {
				monitor.Dispose ();
			};
			return chop;
		}
	}
	
	class ChainedAsyncOperationSequence : IAsyncOperation, IDisposable
	{
		ChainedAsyncOperation[] chain;
		IAsyncOperation op;
		int index = 0;
		System.Threading.ManualResetEvent completedEvent;
		IProgressMonitor monitor;
		bool success;
		
		public ChainedAsyncOperationSequence (IProgressMonitor monitor, params ChainedAsyncOperation[] chain)
		{
			this.chain = chain;
			this.monitor = monitor;
			monitor.CancelRequested += CancelRequested;
			RunNext ();
		}

		void CancelRequested (IProgressMonitor monitor)
		{
			Cancel ();
		}
		
		public event OperationHandler Completed;
		
		void RunNext ()
		{
			var chop = chain[index];
			while (chop.Condition != null && !chop.Condition ()) {
				index++;
				if (IsCompleted) {
					OnCompleted ();
					return;
				}
				chop = chain[index];
			}
			if (chop.TaskName != null)
				monitor.BeginTask (chop.TaskName, 0);
			op = chop.Create ();
			if (op == null) {
				index = -1;
				OnCompleted ();
			} else {
				op.Completed += OpCompleted;
			}
		}

		void OpCompleted (IAsyncOperation op)
		{
			try {
				var chop = chain[index];
				if (chop.Completed != null) {
					chop.Completed (this.op);
				}
				if (chop.TaskName != null)
					monitor.EndTask ();
				index++;
				
				if (IsCompleted) {
					success = op.Success;
				}
				
				DisposeOp ();
				
				if (monitor.IsCancelRequested)
					index = -1;
				
				if (IsCompleted) {
					OnCompleted ();
				} else {
					RunNext ();
				}
			} catch (Exception ex) {
				monitor.ReportError ("Internal error", ex);
				LoggingService.LogError ("Error in async operation chain", ex);
				index = -1;
				OnCompleted ();
			}
		}
		
		void OnCompleted ()
		{
			if (completedEvent != null)
				completedEvent.Set ();
			if (Completed != null)
				Completed (this);
			monitor.CancelRequested -= CancelRequested;
		}
		
		void DisposeOp ()
		{
			if (op == null)
				return;
			var d = op as IDisposable;
			op.Completed -= OpCompleted;
			if (d != null)
				d.Dispose ();
			op = null;
		}

		public void Cancel ()
		{
			op.Cancel ();
		}

		public void WaitForCompleted ()
		{
			completedEvent = new System.Threading.ManualResetEvent (false);
			completedEvent.WaitOne ();
		}

		public bool IsCompleted {
			get {
				return index < 0 || index == chain.Length;
			}
		}

		public bool Success {
			get {
				return index > 0 && IsCompleted && success;
			}
		}

		public bool SuccessWithWarnings {
			get {
				return index > 0 && IsCompleted && success;
			}
		}
		
		public void Dispose ()
		{
		}
	}
	
	class ChainedAsyncOperation	
	{
		public Func<bool> Condition { get; set; }
		public Func<IAsyncOperation> Create { get; set; }
		public Action<IAsyncOperation> Completed { get; set; }
		public string TaskName { get; set; }
		public string Error { get; set; }
	}
}

