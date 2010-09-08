// 
// AndroidToolbox.cs
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

using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.MonoDroid
{
	public class AndroidToolbox
	{
		FilePath androidToolsPath;
		string pathOverride;
		
		public AndroidToolbox (FilePath androidToolsPath, FilePath javaBinPath)
		{
			this.androidToolsPath = androidToolsPath;
			
			pathOverride = Environment.GetEnvironmentVariable ("PATH");
			if (string.IsNullOrEmpty (pathOverride))
				pathOverride = javaBinPath;
			else
				pathOverride = javaBinPath + Path.PathSeparator + pathOverride;
		}
		
		public string AdbExe {
			get {
				return androidToolsPath.Combine (PropertyService.IsWindows? "adb.exe" : "adb");
			}
		}
		
		public string AndroidExe {
			get {
				return androidToolsPath.Combine (PropertyService.IsWindows? "android.bat" : "android");
			}
		}
		
		public string EmulatorExe {
			get {
				return androidToolsPath.Combine (PropertyService.IsWindows? "emulator.exe" : "emulator");
			}
		}
		
		bool EnsureServerRunning (AggregatedOperationMonitor op, IProgressMonitor monitor)
		{
			var psi = new ProcessStartInfo (AdbExe, "start-server") {
				UseShellExecute = false,
			};
			psi.EnvironmentVariables["PATH"] = pathOverride;
			
			monitor.BeginTask ("Starting adb server", 0);
			using (var p = Runtime.ProcessService.StartProcess (psi, monitor.Log, monitor.Log, null)) {
				p.WaitForOutput ();
				monitor.EndTask ();
				if (p.ExitCode > 0) {
					monitor.ReportError ("Failed to start adb server", null);
					return false;
				}
				monitor.ReportSuccess ("Started adb server");
				return true;
			}
		}
		
		bool RunCommand (IProgressMonitor monitor, bool ensureServer, string exe, string command,
			StringWriter output, string taskMessage, string failMessage)
		{
			using (var op = new AggregatedOperationMonitor (monitor)) {
				if (ensureServer) {
					if (!EnsureServerRunning (op, monitor))
						return false;
				}
				
				var errorWriter = new StringWriter ();
				
				monitor.BeginTask (taskMessage, 0);
				
				var psi = new ProcessStartInfo (exe, command) {
					UseShellExecute = false,
				};
				psi.EnvironmentVariables["PATH"] = pathOverride;
				
				var process = StartProcess (exe, command, output, errorWriter);
				op.AddOperation (process);
				
				process.WaitForOutput ();
				monitor.EndTask ();
				
				if (monitor.IsCancelRequested)
					return false;
				
				if (process.ExitCode > 0) {
					monitor.Log.Write (errorWriter.ToString ());
					monitor.ReportError (failMessage, null);
					return false;
				}
				return true;
			}
		}
		
		ProcessWrapper StartProcess (string name, string args, TextWriter output, TextWriter error)
		{
			var psi = new ProcessStartInfo (name, args) {
				UseShellExecute = false,
			};
			psi.EnvironmentVariables["PATH"] = pathOverride;
			return  Runtime.ProcessService.StartProcess (psi, output, error, null);
		}
		
		public List<AndroidDevice> GetDevices (IProgressMonitor monitor)
		{
			var output = new StringWriter ();
			if (RunCommand (monitor, true, AdbExe, "devices", output,
				"Getting running devices",
				"Failed to get running devices"
				))
				return ParseDeviceListOutput (output.ToString ());
			return null;
		}
		
		public List<AndroidVirtualDevice> GetAllVirtualDevices (IProgressMonitor monitor)
		{
			var output = new StringWriter ();
			if (RunCommand (monitor, true, AndroidExe, "list avd", output,
				"Getting virtual devices",
				"Failed to get virtual devices"
				))
				return ParseAndroidVirtualDeviceOutput (output.ToString ());
			return null;
		}
		
		public bool StartAvd (IProgressMonitor monitor, AndroidVirtualDevice avd)
		{
			using (var op = new AggregatedOperationMonitor (monitor)) {
				var output = new StringWriter ();
				var error = new StringWriter ();
				
				monitor.BeginTask (string.Format ("Starting virtual device '{0}'", avd.Name), 0);
				
				string args = string.Format ("partition-size 512 -avd '{0}'", avd.Name);
				var process = StartProcess (EmulatorExe, args, output, error);
				op.AddOperation (process);
				
				process.WaitForOutput ();
				monitor.EndTask ();
				
				if (monitor.IsCancelRequested)
					return false;
				
				if (process.ExitCode > 0 && !error.ToString ().Contains ("used by another emulator")) {
					monitor.Log.Write (error.ToString ());
					monitor.ReportError ("Failed to start virtual device", null);
					return false;
				}
				return true;
			}
		}
		
		public ProcessWrapper StartAvdManager ()
		{
			return Runtime.ProcessService.StartProcess (AndroidExe, "", null, (TextWriter)null, null, null);
		}
		
		public bool PushFile (IProgressMonitor monitor, AndroidDevice device, string source, string destination)
		{
			var args = string.Format ("-s '{0}' push '{1} '{2}'", device.ID, source, destination);
			return RunCommand (monitor, true, AdbExe, args, null,
				string.Format ("Uploading file '{0} to '{1}' on device '{2}'", source, destination, device.ID),
				"Failed to upload file");
		}

		public bool WaitForDevice (IProgressMonitor monitor, AndroidDevice device)
		{
			var args = string.Format ("-s '{0}' wait-for-device");
			return RunCommand (monitor, true, AdbExe, args, null,
				string.Format ("Waiting for device '{0}'", device.ID),
				"Error waiting for device");
		}

		public bool PullFile (IProgressMonitor monitor, AndroidDevice device, string source, string destination)
		{
			var args = string.Format ("-s '{0}' pull '{1} '{2}'", device.ID, source, destination);
			return RunCommand (monitor, true, AdbExe, args, null,
				string.Format ("Downloading file '{0} to '{1}' from device '{2}'", source, destination, device.ID),
				"Failed to download file");
		}

		public bool Install (IProgressMonitor monitor, AndroidDevice device, string package)
		{
			var args = string.Format ("-s '{0}' install '{1}'", device.ID, package);
			return RunCommand (monitor, true, AdbExe, args, null,
				string.Format ("Installing package '{0} on device '{1}'", package, device.ID),
				"Failed to install package");
		}

		public bool Uninstall (IProgressMonitor monitor, AndroidDevice device, string package)
		{
			var args = string.Format ("-s '{0}' uninstall '{1}", device.ID, package);
			return RunCommand (monitor, true, AdbExe, args, null,
				string.Format ("Uninstalling package '{0}' from device '{1}'", package, device.ID),
				"Failed to uninstall package");
		}
		
		//monoRuntimeArgs: debug={0}:{1}:{2}", DebuggerHost, SdbPort, StdoutPort
		public bool StartActivity (IProgressMonitor monitor, AndroidDevice device, string activity, string monoRuntimeArgs)
		{
			var args = string.Format ("-s '{0}' shell am start -a android.intent.action.MAIN -n '{1}'",
				device.ID, activity);
			
			if (!string.IsNullOrEmpty (monoRuntimeArgs)) {
				args = args + " -e mono:runtime-args " + monoRuntimeArgs;
			}
			return RunCommand (monitor, true, AdbExe, args, null,
				string.Format ("Launching activity '{0}' on device '{1}'", activity, device.ID),
				"Failed to run activity");
		}

		public bool ForwardPort (IProgressMonitor monitor, AndroidDevice device, int devicePort, int localPort)
		{
			var args = string.Format ("-s '{0}' forward tcp:{1} tcp:{2}", device.ID, localPort, devicePort);
			return RunCommand (monitor, true, AdbExe, args, null,
				string.Format ("Forwarding port {0 on device '{1}' to local port {2}", devicePort, device.ID, localPort),
				"Failed to forward port");
		}

		public List<string> GetInstalledPackagesOnDevice (IProgressMonitor monitor, AndroidDevice device)
		{
			var args = string.Format ("-s '{0}' shell pm list packages", device.ID);
			var output = new StringWriter ();
			if (RunCommand (monitor, true, AdbExe, args, output,
				"Getting installed packages in device '{0}'",
				"Failed to get installed packages"
				))
				return ParsePackageList (output.ToString ());
			return null;
		}

		public bool IsSharedRuntimeInstalled (IProgressMonitor monitor, AndroidDevice device)
		{
			var list = GetInstalledPackagesOnDevice (monitor, device);
			if (list == null)
				return false;
			return list.Contains ("package:com.novell.monodroid.runtimeservice");
		}
		
		static List<AndroidDevice> ParseDeviceListOutput (string output)
		{
			var devices = new List<AndroidDevice> ();
			
			using (var sr = new StringReader (output)) {
				string s;
	
				while ((s = sr.ReadLine ()) != null) {
					s = s.Trim ();
					
					if (s.Length == 0 || s == "List of devices attached")
						continue;
	
					string[] data = s.Split ('\t');
					devices.Add (new AndroidDevice (data[0].Trim (), data[1].Trim ()));
				}
			}
			return devices;
		}
		
		static List<AndroidVirtualDevice> ParseAndroidVirtualDeviceOutput (string output)
		{
			var devices = new List<AndroidVirtualDevice> ();

			var sr = new StringReader (output);
			AndroidVirtualDevice current_device = null;

			string s;

			while ((s = sr.ReadLine ()) != null) {
				s = s.Trim ();

				if (s.Length == 0)
					continue;

				if (s.StartsWith ("Available") || s.StartsWith ("------")) {
					if (current_device != null)
						devices.Add (current_device);

					continue;
				}

				if (s.StartsWith ("Name:"))
					current_device = new AndroidVirtualDevice ();

				string[] data = s.Split (':');

				// Check for a multi-line property, and ignore it
				if (data.Length > 1 && current_device != null)
					current_device.Properties[data[0].Trim ()] = data[1].Trim ();
			}

			if (current_device != null)
				devices.Add (current_device);

			return devices;
		}
		
		static List<string> ParsePackageList (string output)
		{
			var sr = new StringReader (output);
			var list = new List<string> ();
			
			string s;
			while ((s = sr.ReadLine ()) != null) {
				s.Trim ();
				if (!string.IsNullOrEmpty (s))
					list.Add (s);
			}
			return list;
		}
	}
	
	public class AndroidDevice
	{
		public string ID { get; set; }
		public string State { get; set; }

		public AndroidDevice () {}

		public AndroidDevice (string id, string state)
		{
			ID = id;
			State = state;
		}

		public bool IsEmulator {
			get { return ID.ToLowerInvariant ().StartsWith ("emulator"); }
		}

		public override string ToString ()
		{
			return string.Format ("Device: {0} [{1}]", ID, State);
		}
	}
	
	public class AndroidVirtualDevice
	{
		public Dictionary<string, string> Properties { get; private set; }

		public AndroidVirtualDevice ()
		{
			Properties = new Dictionary<string,string> ();
		}

		#region Convenience Properties
		public string Name { get { return Properties["Name"]; } }
		public string Path { get { return Properties["Path"]; } }
		public string Target { get { return Properties["Target"]; } }
		public string Skin { get { return Properties["Skin"]; } }
		#endregion

		public override string ToString ()
		{
			return string.Format ("Virtual Device: {0} - {2} [{1}]", Name, Path, Target);
		}
	}
	
	public abstract class WrapperOperation : IAsyncOperation, IDisposable
	{
		bool disposed;
		
		protected abstract IAsyncOperation Wrapped { get; }
		
		public virtual event OperationHandler Completed {
			add { Wrapped.Completed += value; }
			remove { Wrapped.Completed -= value; }
		}
		
		public virtual void Cancel ()
		{
			Wrapped.Cancel ();
		}
		
		public virtual void WaitForCompleted ()
		{
			Wrapped.WaitForCompleted ();
		}
		
		public virtual bool IsCompleted {
			get { return Wrapped.IsCompleted; }
		}
		
		public virtual bool Success {
			get { return Wrapped.Success; }
		}
		
		public virtual bool SuccessWithWarnings {
			get { return Wrapped.SuccessWithWarnings; }
		}
		
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		protected virtual void Dispose (bool disposing)
		{
			if (!disposing)
				return;
			
			if (Wrapped is IDisposable)
				((IDisposable)Wrapped).Dispose ();
		}
		
		~WrapperOperation ()
		{
			
		}
	}
}

