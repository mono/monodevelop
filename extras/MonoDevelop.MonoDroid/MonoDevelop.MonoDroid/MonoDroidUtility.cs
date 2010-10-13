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
using System.Threading;
using System.Linq;

namespace MonoDevelop.MonoDroid
{
	public static class MonoDroidUtility
	{
		//simple flag used to determine whether the current binary has been uploaded to the device
		//we clear it before signing, and set it for each device after successful upload
		static bool GetUploadFlag (MonoDroidProjectConfiguration conf, AndroidDevice device)
		{
			try {
				var file = GetUploadFlagFileName (conf);
				if (File.Exists (file)) {
					var deviceIds = File.ReadAllLines (file);
					return deviceIds.Contains (device.ID);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error reading upload flag", ex);
			}
			return true;
		}
		
		static void SetUploadFlag (MonoDroidProjectConfiguration conf, AndroidDevice device)
		{
			try {
				var file = GetUploadFlagFileName (conf);
				if (!File.Exists (file)) {
					File.WriteAllText (file, device.ID);
				} else {
					File.AppendAllText (file, "\n" + device.ID);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error setting upload flag", ex);
			}
		}
		
		static string GetUploadFlagFileName (MonoDroidProjectConfiguration conf)
		{
			return conf.ObjDir.Combine ("uploadflags.txt");
		}
		
		static void ClearUploadFlags (MonoDroidProjectConfiguration conf)
		{
			try {
				var file = GetUploadFlagFileName (conf);
				if (!File.Exists (file))
					File.Delete (file);
			} catch (Exception ex) {
				LoggingService.LogError ("Error clearing upload flags", ex);
			}
		}
		
		//may block while it's showing a GUI. project MUST be built before calling this
		public static IAsyncOperation SignAndUpload (IProgressMonitor monitor, MonoDroidProject project,
			ConfigurationSelector configSel, bool forceReplace, out AndroidDevice device)
		{	
			var conf = project.GetConfiguration (configSel);
			var opMon = new AggregatedOperationMonitor (monitor);
			
			IAsyncOperation signOp = null;
			if (project.PackageNeedsSigning (conf)) {
				ClearUploadFlags (conf);
				signOp = project.SignPackage (configSel);
				opMon.AddOperation (signOp);
			}
			
			//copture the device for a later anonymous method
			AndroidDevice dev = device = InvokeSynch (() => ChooseDevice (null));
			
			if (device == null) {
				opMon.Dispose ();
				return NullAsyncOperation.Failure;
			}
			
			bool replaceIfExists = forceReplace || !GetUploadFlag (conf, device);
			
			var uploadOp = Upload (device, conf.ApkSignedPath, conf.PackageName, signOp, replaceIfExists);
			opMon.AddOperation (uploadOp);
			uploadOp.Completed += delegate (IAsyncOperation op) {
				if (op.Success) {
					SetUploadFlag (conf, dev);
				}
				opMon.Dispose ();
			};
			return uploadOp;
		}
		
		static IAsyncOperation Upload (AndroidDevice device, FilePath packageFile, string packageName,
			IAsyncOperation signingOperation, bool replaceIfExists)
		{
			var toolbox = MonoDroidFramework.Toolbox;
			
			var monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Deploy to Device"), MonoDevelop.Ide.Gui.Stock.RunProgramIcon, true, true);
			
			List<string> packages = null;
			
			replaceIfExists = replaceIfExists || signingOperation != null;
			
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
					Skip = () => toolbox.IsSharedRuntimeInstalled (packages)? "" : null,
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
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Uninstalling old version of package"),
					Skip = () => (!replaceIfExists || !packages.Contains ("package:" + packageName))? "" : null,
					Create = () => toolbox.Uninstall (device, packageName, monitor.Log, monitor.Log),
					Error = GettextCatalog.GetString ("Failed to uninstall package")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Waiting for packaging signing to complete"),
					Skip = () => (signingOperation == null || signingOperation.IsCompleted) ? "" : null,
					Create = () => signingOperation,
					Error = GettextCatalog.GetString ("Package signing failed"),
				},
				new ChainedAsyncOperation () {
					Skip = () => (packages.Contains ("package:" + packageName) && !replaceIfExists)
						? GettextCatalog.GetString ("Package is already up to date") : null,
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
		
		public static AndroidDevice ChooseDevice (Gtk.Window parent)
		{
			var dlg = new MonoDevelop.MonoDroid.Gui.DeviceChooserDialog ();
			try {
				var result = MessageService.ShowCustomDialog (dlg, parent);
				if (result != (int)Gtk.ResponseType.Ok)
					return null;
				return dlg.Device;
			} finally {
				dlg.Destroy ();
			}
		}
		
		static T InvokeSynch<T> (Func<T> func)
		{
			if (DispatchService.IsGuiThread)
				return func ();
			
			var ev = new ManualResetEvent (false);
			T val = default (T);
			Gtk.Application.Invoke (delegate {
				val = func ();
				ev.Set ();
			});
			ev.WaitOne ();
			return val;
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
			string skipmsg = null;
			while (chop.Skip != null && (skipmsg = chop.Skip ()) != null) {
				if (skipmsg.Length > 0) {
					monitor.BeginTask (chop.TaskName, 0);
					monitor.Log.WriteLine (skipmsg);
					monitor.EndTask ();
				}
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
				
				success = op.Success;
				
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
				return IsCompleted && success;
			}
		}

		public bool SuccessWithWarnings {
			get {
				return IsCompleted && success;
			}
		}
		
		public void Dispose ()
		{
		}
	}
	
	class ChainedAsyncOperation	
	{
		// null = don't skip, string = skip with message, string.empty = skip silently
		public Func<string> Skip { get; set; }
		public Func<IAsyncOperation> Create { get; set; }
		public Action<IAsyncOperation> Completed { get; set; }
		public string TaskName { get; set; }
		public string Error { get; set; }
	}
}

