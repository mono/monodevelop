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
		object lockObj = new object ();
		int openProjects = 0;
		
		//this should be a singleton created from MonoDroidFramework
		internal DeviceManager ()
		{
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
			op = MonoDroidFramework.Toolbox.EnsureServerRunning (stdOut, stdOut);
			op.Completed += delegate (IAsyncOperation esop) {
				if (!esop.Success) {
					LoggingService.LogError ("Error starting adb server: " + stdOut);
					((IDisposable)esop).Dispose ();
					op = null;
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
					op = null;
				}
			};
		}
		
		AdbTrackDevicesOperation CreateTracker ()
		{
			var trackerOp = new AdbTrackDevicesOperation ();
			trackerOp.DevicesChanged += delegate (List<AndroidDevice> list) {
				Devices = list;
				OnChanged (null, null);
			};
			trackerOp.Completed += delegate (IAsyncOperation op) {
				var err = ((AdbTrackDevicesOperation)op).Error;
				if (err != null) {
					lock (lockObj) {
						((IDisposable)op).Dispose ();
						op = null;
					}
					LoggingService.LogError ("Error in device tracker", err);
				}
			};
			Devices = trackerOp.Devices;
			return trackerOp;
		}
		
		void StopTracker ()
		{
			LoggingService.LogInfo ("Stopping Android device monitor");
			((IDisposable)op).Dispose ();
			Devices = new AndroidDevice[0];
			op = null;
		}

		void OnChanged (object sender, EventArgs e)
		{
			if (devicesUpdated != null)
				devicesUpdated (this, EventArgs.Empty);
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
			if (device == null)
				return false;
			return device.State == "device";
		}
	}
}

