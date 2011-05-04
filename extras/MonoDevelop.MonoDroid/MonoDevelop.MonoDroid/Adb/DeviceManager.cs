// 
// DeviceManager.cs
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
using System.Collections.Generic;

using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.IO;
using System.Linq;
using System.Threading;

namespace MonoDevelop.MonoDroid
{
	public class DeviceManager
	{
		EventHandler devicesUpdated;
		IDisposable pop; 
		IAsyncOperation op;
		DevicePropertiesTracker propTracker;
		object lockObj = new object ();
		int openProjects = 0;

		string lastForwarded;
		
		//this should be a singleton created from MonoDroidFramework
		internal DeviceManager ()
		{
			Devices = new AndroidDevice[0];
		}
		
		internal void IncrementOpenProjectCount ()
		{
			lock (lockObj) {
				openProjects++;
				CheckTracker ();
			}
		}
		
		internal void DecrementOpenProjectCount ()
		{
			lock (lockObj) {
				openProjects--;
				CheckTracker ();
			}
		}
		
		internal void AndroidSdkChanged ()
		{
			lock (lockObj) {
				if (op != null)
					StopTracker ();
				CheckTracker ();
			}
		}
		
		void CheckTracker ()
		{
			if (pop != null)
				return;
			bool needed = openProjects > 0 || devicesUpdated != null;
			if (op == null) {
				if (needed)
					StartTracker ();
			} else {
				if (!needed)
					StopTracker ();
			}
		}
		
		void StartTracker ()
		{
			LoggingService.LogInfo ("Starting Android device monitor");
			
			//toolbox could be null if the android SDK is not found and not yet configured
			var tb = MonoDroidFramework.Toolbox;
			if (tb == null)
				return;

			if (pop is AdbStartServerProcess)
				((AdbStartServerProcess)pop).Exited += StartServerProcessDone;
			
			pop = new AdbStartServerProcess (tb, StartServerProcessDone);
		}

		void StartServerProcessDone (object sender, EventArgs e)
		{
			var startOp = (AdbStartServerProcess) sender;

			pop = null;
			LoggingService.LogInfo ("Adb server launch operation completed");
			try {
				if (!startOp.Success) {
					LoggingService.LogError ("Error starting adb server: " + startOp.GetOutput ());
					ClearTracking ();
					return;
				}
				try {
					lock (lockObj) {
						op = CreateTracker ();
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Error creating device tracker: ", ex);
					ClearTracking ();
				}
			} finally {
				try {
					((IDisposable)startOp).Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError ("Error disposing adb start operation: ", ex);
				}
			}
		}
		
		AdbTrackDevicesOperation CreateTracker ()
		{
			LoggingService.LogInfo ("Creating android device tracker");
			var trackerOp = new AdbTrackDevicesOperation ();
			propTracker = new DevicePropertiesTracker ();
			propTracker.Changed += delegate {
				OnChanged (null, null);
			};
			trackerOp.DevicesChanged += delegate (List<AndroidDevice> list) {
				LoggingService.LogInfo ("Got new device list from adb");
				Devices = list;
				OnChanged (null, null);
			};
			trackerOp.Completed += delegate (IAsyncOperation op) {
				var err = ((AdbTrackDevicesOperation)op).Error;
				if (err != null) {
					LoggingService.LogError ("Error in device tracker", err);
					ClearTracking ();
				}
			};
			Devices = trackerOp.Devices;
			return trackerOp;
		}
		
		void StopTracker ()
		{
			LoggingService.LogInfo ("Stopping Android device monitor");
			ClearTracking ();
		}
		
		void ClearTracking ()
		{
			lock (lockObj) {
				if (op != null)
					((IDisposable)op).Dispose ();
				op = null;
				if (propTracker != null)
					propTracker.Dispose ();
				propTracker = null;
				if (pop != null)
					pop.Dispose ();
				pop = null;
				Devices = new AndroidDevice[0];
				lastForwarded = null;
				OnChanged (null, null);
			}
		}

		void OnChanged (object sender, EventArgs e)
		{
			if (propTracker != null)
				propTracker.AnnotateProperties (Devices);
			if (lastForwarded != null && !Devices.Any (d => d.ID == lastForwarded))
				lastForwarded = null;
			if (devicesUpdated != null)
				devicesUpdated (this, EventArgs.Empty);
		}
		
		public void RestartAdbServer (Action serverKilledCallback)
		{
			lock (lockObj) {
				if (op != null)
					StopTracker ();
			}

			//toolbox could be null if the android SDK is not found and not yet configured
			var tb = MonoDroidFramework.Toolbox;
			if (tb == null)
				return;
			
			pop = new AdbKillServerOperation ();
			((AdbKillServerOperation)pop).Completed += delegate (IAsyncOperation killOp) {
				if (!object.ReferenceEquals (pop, killOp)) {
					LoggingService.LogInfo ("Adb kill operation completed but is no longer valid");
					return;
				}
				LoggingService.LogInfo ("Adb server kill operation completed");
				if (!killOp.Success)
					LoggingService.LogError ("Error killing adb server: " + ((AdbKillServerOperation)killOp).Error);
				try {
					((IDisposable)killOp).Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError ("Error disposing adb kill operation: ", ex);
				}
				pop = null;
				CheckTracker ();
				if (serverKilledCallback != null)
					serverKilledCallback ();
			};
		}
		
		public event EventHandler DevicesUpdated {
			add {
				lock (lockObj) {
					devicesUpdated += value;
					CheckTracker ();
				}
			}
			remove {
				lock (lockObj) {
					devicesUpdated -= value;
					CheckTracker ();
				}
			}
		}
		
		public IList<AndroidDevice> Devices { get; set; }
		
		public AndroidDevice GetDevice (string id)
		{
			return Devices.FirstOrDefault (d => d.ID == id);
		}
		
		public bool GetDeviceIsOnline (string id)
		{
			var device = GetDevice (id);
			return device != null && device.IsOnline;
		}

		// We only track the last forwarded device as long as the tracker is alive.
		public bool GetDeviceIsForwarded (string id)
		{
			return lastForwarded == id;
		}

		public void SetDeviceLastForwarded (string id)
		{
			lock (lockObj) {
				if (op != null)
					lastForwarded = id;
			}
		}
	}
	
	class DevicePropertiesTracker : IDisposable
	{
		Dictionary<string,Dictionary<string,string>> props = new Dictionary<string, Dictionary<string, string>> ();
		HashSet<IAsyncOperation> outstandingQueries = new HashSet<IAsyncOperation> ();
		bool disposed;
		
		// Given a full list of devices, returns a list of device properties dictionaries
		// if there in no cached property set for devices in the list, an async query is made
		// and the chnage event is fired when it's done
		// Devices not in the list are purged from the cache
		public void AnnotateProperties (IList<AndroidDevice> devices)
		{
			if (devices == null || devices.Count == 0) {
				lock (props)
					props.Clear ();
				return;
			}
			
			var toClear = new HashSet<string> ();
			lock (props) {
				toClear.UnionWith (props.Keys);
				foreach (var device in devices) {
					Dictionary<string,string> val = null;
					if (device.IsOnline) {
						if (props.TryGetValue (device.ID, out val)) {
							toClear.Remove (device.ID);
							device.Properties = val;
						} else {
							AsyncGetProperties (device);
						}
					}
				}
				foreach (var k in toClear)
					props.Remove (k);
			}
		}
		
		public event Action Changed;
		
		void AsyncGetProperties (AndroidDevice device)
		{
			var gpop = new AdbGetPropertiesOperation (device);
			lock (outstandingQueries) {
				outstandingQueries.Add (gpop);
			}
			gpop.Completed += delegate (IAsyncOperation op) {
				lock (outstandingQueries) {
					if (disposed)
						return;
					outstandingQueries.Remove (gpop);
					gpop.Dispose ();
				}
				if (!op.Success) {
					LoggingService.LogError (string.Format ("Error getting properties from device '{0}'", device.ID), gpop.Error);
					//fall through, to cache the null result for failed queries
				}
				lock (props) {
					props [device.ID] = gpop.Properties	;
				}
				if (Changed != null)
					Changed ();
			};
		}
		
		public void Dispose ()
		{
			if (disposed)
				return;
			lock (outstandingQueries) {
				if (disposed)
					return;
				disposed = true;
			}
			foreach (IDisposable disp in outstandingQueries)
				disp.Dispose ();
			outstandingQueries.Clear ();
		}
	}
		
	//HACK: using Process and a thread instead of MD process APIs because of weird stuff adb start-server does
	// When using adb start-server, .NET's StandardOutput.Read blocks or even throws a "stdout not redirected" 
	// after all data is read and the process has ended
	// This seems to be because adb forks, and even though the original process exits, the output stream somehow 
	// stays alive. Iit's probably been passed over to the new process somehow.
	//
	class AdbStartServerProcess : IDisposable
	{
		//ManualResetEvent endEventErr = new ManualResetEvent (false);
		Thread captureOutputThread; //, captureErrorThread;
		System.Diagnostics.Process proc;
		object lockObj = new object ();
		EventHandler exited;
		StringWriter output = new StringWriter ();
		bool success = false;
		
		public bool Success { get { return success; } }
		
		public string GetOutput ()
		{
			return output.ToString ();
		}
		
		public AdbStartServerProcess (AndroidToolbox tb, EventHandler exited)
		{
			this.exited = exited;
			
			proc = new System.Diagnostics.Process ();
			proc.StartInfo = new System.Diagnostics.ProcessStartInfo (tb.AdbExe, "start-server") {
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			proc.StartInfo.EnvironmentVariables["PATH"] = tb.AdbPathOverride;
			proc.Start ();
			
			captureOutputThread = new Thread (CaptureOutput) {
				Name = "Adb output reader",
				IsBackground = true,
			};
			captureOutputThread.Start ();
			
			/*
			captureErrorThread = new Thread (CaptureError) {
				Name = "Adb error reader",
				IsBackground = true,
			};
			captureErrorThread.Start ();*/
		}

		public event EventHandler Exited {
			add { exited += value; }
			remove { exited -= value; }
		}
		
		void CaptureOutput ()
		{
			try {
				//HACK: this is just long enough to contain the expected adb output string when successfully starting the server
				//if we try to read too much, we will hang somewhere in native code
				char[] buffer = new char [86];
				int nr;
				while ((nr = proc.StandardOutput.Read (buffer, 0, buffer.Length)) > 0) {
					var s = new string (buffer, 0, nr);
					output.Write (s);
					if (s.Contains ("daemon started successfully")) {
						success = true;
						/*
						lock (lockObj) {
							captureErrorThread.Abort ();
							captureErrorThread = null;
						}*/
						break;
					}
				}
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled exception in adb output reader", ex);
			} finally {
				/*
				if (endEventErr != null)
					WaitHandle.WaitAll (new WaitHandle[] {endEventErr} );
				*/
				
				//HACK: if success is true at this point, then we have determined that adb is forking a new server 
				// and bailed out early to avoid the Windows native hang that happens when we read too far in the adb
				// stdout stream that gets passed over to the new process.
				// Unfortunately, if the fork happens then the error stream thread hangs in the same way, and cannot 
				// even be aborted. We avoid this by *only* reading stderr if this condition is false. Instead we 
				// only read stderr after the output thread is done. Sadly this means we lose stderr/stdout 
				// interleaving, and the adb process could deadlock if stderr fills up too much.
				if (!success) {
					string line;
					while ((line = proc.StandardError.ReadLine ()) != null)
						output.WriteLine (line);
				}
				
				if (!success && proc.HasExited && proc.ExitCode <= 0)
					success = true;
				
				captureOutputThread = null;
				exited (this, EventArgs.Empty);
			}
		}
		/*
		void CaptureError ()
		{
			try {
				char[] buffer = new char [1024];
				int nr;
				while ((nr = proc.StandardError.Read (buffer, 0, buffer.Length)) > 0) {
					var s = new string (buffer, 0, nr);
					output.Write (s);
				}					
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled exception in adb error reader", ex);
			} finally {
				lock (lockObj) {
					if (endEventErr != null)
						endEventErr.Set ();
				}
			}
		}*/
		
		public void Dispose ()
		{
			var proc = this.proc;
			lock (lockObj) {
				if (this.proc == null)
					return;
				this.proc = null;
			}
			if (captureOutputThread != null) {
				if (captureOutputThread.IsAlive) {
					try {
						captureOutputThread.Abort ();
					} catch {}
				}
				captureOutputThread = null;
			}
			/*
			if (captureErrorThread != null) {
				if (captureErrorThread.IsAlive) {
					try {
						captureErrorThread.Abort ();
					} catch {}
				}
				captureErrorThread = null;
			}*/
			proc.Dispose ();
		}
	}
}

