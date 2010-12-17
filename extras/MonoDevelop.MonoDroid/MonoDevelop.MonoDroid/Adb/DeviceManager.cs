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

namespace MonoDevelop.MonoDroid
{
	public class DeviceManager
	{
		EventHandler devicesUpdated;
		AdbTrackDevicesOperation trackerOp;
		
		//this should be a singleton created from MonoDroidFramework
		internal DeviceManager ()
		{
		}
		
		void StartTracker ()
		{
			trackerOp = new AdbTrackDevicesOperation ();
			trackerOp.DevicesChanged += delegate (List<AndroidDevice> list) {
				Devices = list;
				OnChanged (null, null);
			};
			trackerOp.Completed += delegate (IAsyncOperation op) {
				var err = ((AdbTrackDevicesOperation)op).Error;
				if (err != null)
					LoggingService.LogError ("Error in device tracker", err);
			};
			Devices = trackerOp.Devices;
		}
		
		void StopTracker ()
		{
			trackerOp.Dispose ();
			trackerOp = null;
		}

		void OnChanged (object sender, EventArgs e)
		{
			if (devicesUpdated != null)
				devicesUpdated (this, EventArgs.Empty);
		}
		
		public event EventHandler DevicesUpdated {
			add {
				if (devicesUpdated == null)
					StartTracker ();
				devicesUpdated += value;
			}
			remove {
				devicesUpdated -= value;
				if (devicesUpdated == null)
					StopTracker ();
			}
		}
		
		public IList<AndroidDevice> Devices { get; set; }
	}
}

