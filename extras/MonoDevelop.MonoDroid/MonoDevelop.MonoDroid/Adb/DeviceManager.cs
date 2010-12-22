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
using System.IO;
using System.Linq;

namespace MonoDevelop.MonoDroid
{
	public class DeviceManager
	{
		EventHandler devicesUpdated;
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
			var stdOut = new StringWriter ();

			//toolbox could be null if the android SDK is not found and not yet configured
			var tb = MonoDroidFramework.Toolbox;
			if (tb == null)
				return;
			
			op = tb.EnsureServerRunning (stdOut, stdOut);
			op.Completed += delegate (IAsyncOperation esop) {
				if (!esop.Success) {
					LoggingService.LogError ("Error starting adb server: " + stdOut);
					ClearTracking ();
					return;
				}	
				try {
					lock (lockObj) {
						if (op != null) {
							op = CreateTracker ();
						}
					}
					((IDisposable)esop).Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError ("Error creating device tracker: ", ex);
					ClearTracking ();
				}
			};
		}
		
		AdbTrackDevicesOperation CreateTracker ()
		{
			var trackerOp = new AdbTrackDevicesOperation ();
			propTracker = new DevicePropertiesTracker ();
			propTracker.Changed += delegate {
				OnChanged (null, null);
			};
			trackerOp.DevicesChanged += delegate (List<AndroidDevice> list) {
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
			var killOp = new AdbKillServerOperation ();
			killOp.Completed += delegate(IAsyncOperation op) {
				var err = ((AdbKillServerOperation)op).Error;
				if (err != null)
					LoggingService.LogError ("Error stopping adb server", err);
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
}

