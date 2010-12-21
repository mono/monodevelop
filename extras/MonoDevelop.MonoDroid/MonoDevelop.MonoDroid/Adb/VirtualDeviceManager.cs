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
	// NOT threadsafe
	public class VirtualDeviceManager
	{
		Action<IList<AndroidVirtualDevice>> changed;
		List<AndroidToolbox.StartAvdOperation> emulatorHandles = new List<AndroidToolbox.StartAvdOperation> ();
		AvdWatcher avdWatcher;
		
		//this should be a singleton created from MonoDroidFramework
		internal VirtualDeviceManager ()
		{
			MonoDevelop.Ide.IdeApp.Exited += IdeAppExited;
		}
		
		void StartWatcher ()
		{
			System.Diagnostics.Debug.Assert (avdWatcher == null);
			avdWatcher = new AvdWatcher ();
			avdWatcher.Changed += HandleAvdWatcherChanged;
			VirtualDevices = avdWatcher.VirtualDevices;
		}
		
		void StopWatcher ()
		{
			avdWatcher.Changed -= HandleAvdWatcherChanged;
			avdWatcher.Dispose ();
			avdWatcher = null;
		}

		void HandleAvdWatcherChanged (IList<AndroidVirtualDevice> list)
		{
			VirtualDevices = list;
			try {
				if (changed != null)
					changed (list);
			} catch (Exception ex) {
				LoggingService.LogError ("Error in VirtualDeviceManager event handler", ex);
			}
		}
		
		/// <summary>
		/// List of Avds. Only updated while an event handler is connected.
		/// </summary>
		public IList<AndroidVirtualDevice> VirtualDevices { get; set; }
		
		public event Action<IList<AndroidVirtualDevice>> Changed {
			add {
				if (changed == null)
					StartWatcher ();
				changed += value;
			}
			remove {
				changed -= value;
				if (changed == null)
					StopWatcher ();
			}
		}

		void IdeAppExited (object sender, EventArgs e)
		{
			lock (emulatorHandles) {
				foreach (var p in emulatorHandles) {
					p.Completed -= HandleEmulatorStarted;
					p.Cancel ();
					p.Dispose ();
				}
			}
		}
		
		public void StartEmulator (AndroidVirtualDevice avd)
		{
			//FIXME: actually log the output and status
			var op = MonoDroidFramework.Toolbox.StartAvd (avd);
			emulatorHandles.Add (op);
			op.Completed += HandleEmulatorStarted;
		}

		void HandleEmulatorStarted (IAsyncOperation op)
		{
			var p = (AndroidToolbox.StartAvdOperation)op;
			lock (emulatorHandles) {
				emulatorHandles.Remove (p);
				op.Completed -= HandleEmulatorStarted;
			}
			if (!op.Success) {
				MonoDevelop.Ide.MessageService.ShowError (
					"Failed to start AVD", p.ErrorText);
			}
		}
	}
}
