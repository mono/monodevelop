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
				if (File.Exists (file))
					File.Delete (file);
			} catch (Exception ex) {
				LoggingService.LogError ("Error clearing upload flags", ex);
			}
		}
		
		//may block while it's showing a GUI. project MUST be built before calling this
		public static MonoDroidUploadOperation SignAndUpload (IProgressMonitor monitor, MonoDroidProject project,
			ConfigurationSelector configSel, bool forceReplace, ref AndroidDevice device)
		{	
			var conf = project.GetConfiguration (configSel);
			var opMon = new AggregatedOperationMonitor (monitor);

			InvokeSynch (() => MonoDroidFramework.EnsureSdksInstalled ());
			
			IAsyncOperation signOp = null;
			if (project.PackageNeedsSigning (configSel)) {
				ClearUploadFlags (conf);
				signOp = project.SignPackage (configSel);
				opMon.AddOperation (signOp);
			}
			
			if (device == null)
				device = InvokeSynch (() => ChooseDevice (null));
			
			if (device == null) {
				opMon.Dispose ();
				return null;
			}
			
			//copture the device for a later anonymous method
			AndroidDevice dev = device;
			
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
		
		static MonoDroidUploadOperation Upload (AndroidDevice device, FilePath packageFile, string packageName,
			IAsyncOperation signingOperation, bool replaceIfExists)
		{
			
			var monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (
				GettextCatalog.GetString ("Deploy to Device"), MonoDevelop.Ide.Gui.Stock.RunProgramIcon, true, true);
			
			var op = new MonoDroidUploadOperation (monitor, device, packageFile, packageName, signingOperation, replaceIfExists);
			op.Completed += delegate {
				monitor.Dispose ();
			};
			op.Start ();
			return op;
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
	
	public class MonoDroidUploadOperation : IAsyncOperation
	{
		ChainedAsyncOperationSequence chop;
		const int RuntimeVersion = 2;
		
		public MonoDroidUploadOperation (IProgressMonitor monitor, AndroidDevice device, FilePath packageFile, string packageName,
			IAsyncOperation signingOperation, bool replaceIfExists)
		{
			var toolbox = MonoDroidFramework.Toolbox;
			var project = DefaultUploadToDeviceHandler.GetActiveExecutableMonoDroidProject ();
			var conf = (MonoDroidProjectConfiguration) project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			int apiLevel = MonoDroidFramework.FrameworkVersionToApiLevel (project.TargetFramework.Id.Version);
			string packagesListLocation = null;
			PackageList list = null;
			
			replaceIfExists = replaceIfExists || signingOperation != null;
			
			chop = new ChainedAsyncOperationSequence (monitor,
				new ChainedAsyncOperation () {
					Skip = () => MonoDroidFramework.DeviceManager.GetDeviceIsOnline (device.ID) ? "" : null,
					TaskName = GettextCatalog.GetString ("Waiting for device"),
					Create = () => toolbox.WaitForDevice (device, monitor.Log, monitor.Log),
					Completed = op => { DeviceNotFound = !op.Success; },
					ErrorMessage = GettextCatalog.GetString ("Failed to get device")
				},
				new ChainedAsyncOperation<AdbShellOperation> () {
					TaskName = GettextCatalog.GetString ("Getting the package list location from device"),
					Create = () => new AdbShellOperation (device, "ls /data/system/packages.xml"),
					Completed = op => {
						string output = op.Output.Trim (new char [] { '\n', '\r' });
						if (output == "/data/system/packages.xml")
							packagesListLocation = output;
						else
							packagesListLocation = "/dbdata/system/packages.xml";
					}
				},
				new ChainedAsyncOperation<AdbGetPackagesOperation> () {
					TaskName = GettextCatalog.GetString ("Getting package list from device"),
					Create = () => new AdbGetPackagesOperation (device, packagesListLocation),
					Completed = op => list = op.PackageList,
					ErrorMessage = GettextCatalog.GetString ("Failed to get package list")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Uninstalling old version of shared runtime package"),
					Skip = () => !conf.AndroidUseSharedRuntime || list.GetOldRuntimesAndPlatforms (apiLevel, RuntimeVersion).Count () == 0 ? 
						"" : null,
					Create = () => { // Cleanup task, no need to wait for it
						foreach (InstalledPackage oldPackage in list.GetOldRuntimesAndPlatforms (apiLevel, RuntimeVersion))
							toolbox.Uninstall (device, oldPackage.Name, monitor.Log, monitor.Log);
						return Core.Execution.NullProcessAsyncOperation.Success;
					},
					ErrorMessage = GettextCatalog.GetString ("Failed to uninstall package")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Installing shared runtime package on device"),
					Skip = () => !conf.AndroidUseSharedRuntime || list.IsCurrentRuntimeInstalled (RuntimeVersion) ? 
						"" : null,
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
					ErrorMessage = GettextCatalog.GetString ("Failed to install shared runtime package")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Installing the platform framework"),
					Skip = () => !conf.AndroidUseSharedRuntime || list.IsCurrentPlatformInstalled (apiLevel, RuntimeVersion) ? 
						"" : null,
					Create = () => {
						var platformApk = MonoDroidFramework.GetPlatformPackage (apiLevel);
						if (!File.Exists (platformApk)) {
							var msg = GettextCatalog.GetString ("Could not find platform package file");
							monitor.ReportError (msg, null);
							LoggingService.LogError ("{0} '{1}'", msg, platformApk);
							return null;
						}
						return toolbox.Install (device, platformApk, monitor.Log, monitor.Log);
					},
					ErrorMessage = GettextCatalog.GetString ("Failed to install the platform framework")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Uninstalling old version of package"),
					Skip = () => (!replaceIfExists || !list.ContainsPackage (packageName))? "" : null,
					Create = () => toolbox.Uninstall (device, packageName, monitor.Log, monitor.Log),
					ErrorMessage = GettextCatalog.GetString ("Failed to uninstall package")
				},
				new ChainedAsyncOperation () {
					TaskName = GettextCatalog.GetString ("Waiting for packaging signing to complete"),
					Skip = () => (signingOperation == null || signingOperation.IsCompleted) ? "" : null,
					Create = () => signingOperation,
					ErrorMessage = GettextCatalog.GetString ("Package signing failed"),
				},
				new ChainedAsyncOperation () {
					Skip = () => (list.ContainsPackage (packageName) && !replaceIfExists)
						? GettextCatalog.GetString ("Package is already up to date") : null,
					TaskName = GettextCatalog.GetString ("Installing package"),
					Create = () => toolbox.Install (device, packageFile, monitor.Log, monitor.Log),
					ErrorMessage = GettextCatalog.GetString ("Failed to install package")
				}
			);
		}
		
		public bool DeviceNotFound {
			get; private set;
		}

		public void Start ()
		{
			chop.Start ();
		}

		#region IAsyncOperation implementation
		
		public event OperationHandler Completed {
			add { chop.Completed += value; }
			remove { chop.Completed -= value; }
		}

		public void Cancel ()
		{
			chop.Cancel ();
		}

		public void WaitForCompleted ()
		{
			chop.WaitForCompleted ();
		}

		public bool IsCompleted {
			get { return chop.IsCompleted; }
		}

		public bool Success {
			get { return chop.Success; }
		}

		public bool SuccessWithWarnings {
			get { return chop.SuccessWithWarnings; }
		}
		#endregion
	}
	
	public class ChainedAsyncOperationSequence : IAsyncOperation
	{
		ChainedAsyncOperation[] chain;
		IAsyncOperation op;
		int index = 0;
		System.Threading.ManualResetEvent completedEvent;
		IProgressMonitor monitor;
		bool success;
		bool cancel;
		
		public ChainedAsyncOperationSequence (params ChainedAsyncOperation[] chain) : this (null, chain)
		{
		}
		
		public ChainedAsyncOperationSequence (IProgressMonitor monitor, params ChainedAsyncOperation[] chain)
		{
			this.chain = chain;
			if (monitor != null) {
				this.monitor = monitor;
				monitor.CancelRequested += CancelRequested;
			}
		}
		
		public void Start ()
		{
			if (index != 0 || op != null)
				throw new InvalidOperationException ("Already started");
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
				if (skipmsg.Length > 0 && monitor != null) {
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
			if (chop.TaskName != null && monitor != null)
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
				
				if (chop.TaskName != null && monitor != null)
					monitor.EndTask ();
				
				success = op.Success;
				if (success) {
					index++;
				} else {
					index = -1;
					if (chop.ErrorMessage != null && monitor != null) {
						Exception ex = null;
						if (op is AdbOperation)
							ex = ((AdbOperation)op).Error;
						monitor.ReportError (chop.ErrorMessage, ex);
						LoggingService.LogError (chop.ErrorMessage, ex);
					}
				}
				
				DisposeOp ();
				
				if (cancel || monitor != null && monitor.IsCancelRequested)
					index = -1;
				
				if (IsCompleted) {
					OnCompleted ();
				} else {
					RunNext ();
				}
			} catch (Exception ex) {
				if (monitor != null)
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
			if (monitor != null)
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
			cancel = true;
			if (op == null)
				return;
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
				return index == chain.Length && success;
			}
		}

		public bool SuccessWithWarnings {
			get {
				return Success;
			}
		}
		
		public void Dispose ()
		{
		}
	}
	
	public class ChainedAsyncOperation	
	{
		// null = don't skip, string = skip with message, string.empty = skip silently
		public Func<string> Skip { get; set; }
		public Func<IAsyncOperation> Create { get; set; }
		public Action<IAsyncOperation> Completed { get; set; }
		public string TaskName { get; set; }
		public string ErrorMessage { get; set; }
	}
	
	public class ChainedAsyncOperation<T> : ChainedAsyncOperation where T : IAsyncOperation	
	{
		public ChainedAsyncOperation ()
		{
			base.Create = () => Create ();
			base.Completed = (op) => Completed ((T)op);
		}
		
		// null = don't skip, string = skip with message, string.empty = skip silently
		public new Func<T> Create { get; set; }
		public new Action<T> Completed { get; set; }
	}
}

