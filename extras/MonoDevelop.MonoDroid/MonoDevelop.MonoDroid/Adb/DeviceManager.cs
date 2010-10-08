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

namespace MonoDevelop.MonoDroid
{
	public class DeviceManager
	{
		EventHandler devicesUpdated;
		List<AndroidToolbox.StartAvdOperation> emulatorHandles = new List<AndroidToolbox.StartAvdOperation> ();
		
		DevicePoller poller = new DevicePoller ();
		
		//this should be a singleton created from MonoDroidFramework
		internal DeviceManager ()
		{
			poller.Changed += OnChanged;
			MonoDevelop.Ide.IdeApp.Exited += IdeAppExited;
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

		void OnChanged (object sender, EventArgs e)
		{
			if (devicesUpdated != null)
				devicesUpdated (this, EventArgs.Empty);
		}
		
		public event EventHandler DevicesUpdated {
			add {
				if (devicesUpdated == null)
					poller.Start ();
				devicesUpdated += value;
			}
			remove {
				devicesUpdated -= value;
				if (devicesUpdated == null)
					poller.Stop ();
			}
		}
		
		public void Refresh ()
		{			
			poller.Refresh ();
		}
		
		public void StartEmulator (AndroidVirtualDevice avd)
		{
			//FIXME: actually log the output and status
			var op = MonoDroidFramework.Toolbox.StartAvd (avd);
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
			Refresh ();
		}
		
		public List<AndroidDevice> Devices {
			get { return poller.Devices; }
		}
		
		public List<AndroidVirtualDevice> VirtualDevices {
			get { return poller.VirtualDevices; }
		}
		
		class DevicePoller : AsyncPoller
		{
			AndroidToolbox.GetDevicesOperation devicesOp;
			AndroidToolbox.GetVirtualDevicesOperation virtualDevicesOp;
			
			public List<AndroidDevice> Devices { get; private set; }
			public List<AndroidVirtualDevice> VirtualDevices { get; private set; }
						
			protected override void BeginRefresh ()
			{
				var op = new AggregatedAsyncOperation ();
				devicesOp = MonoDroidFramework.Toolbox.GetDevices (Console.Out);
				virtualDevicesOp = MonoDroidFramework.Toolbox.GetAllVirtualDevices (Console.Out);
				op.Add (devicesOp);
				op.Add (virtualDevicesOp);
				op.StartMonitoring ();
				op.Completed += PollCompleted;
			}

			void PollCompleted (IAsyncOperation op)
			{
				List<AndroidDevice> devices = null;
				List<AndroidVirtualDevice> virtualDevices = null;
				try {
					devices = devicesOp.Result;
					virtualDevices = virtualDevicesOp.Result;
					devicesOp.Dispose ();
					virtualDevicesOp.Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading device data", ex);
				}
				
				Gtk.Application.Invoke (delegate {
					bool changed = false;
					if (!ListEqual (this.Devices, devices)) {
						this.Devices = devices;
						changed = true;
					}
					
					if (!ListEqual (this.VirtualDevices, virtualDevices)) {
						this.VirtualDevices = virtualDevices;
						changed = true;
					}
					OnRefreshed (changed);
				});
			}
			
			static bool ListEqual<T> (List<T> a, List<T> b)
			{
				if (a == null && b == null)
					return true;
				if (a == null || b == null)
					return false;
				if (a.Count != b.Count)
					return false;
				var hashSet = new HashSet<T> (a);
				foreach (var item in b)
					if (!hashSet.Contains (item))
						return false;
				return true;
			}
		}
	}
	
	// MUST BE USED FROM GUI THREAD ONLY
	// polls in the GUI thread. each poll is async, and when it returns, another is queued up
	class AsyncPoller
	{
		bool active;
		uint source;
		
		public int Timeout { get; set; }
		
		public bool Active {
			get { return active; }
		}
		
		public void Start ()
		{
#if DEBUG
			MonoDevelop.Ide.DispatchService.AssertGuiThread ();
#endif
			if (!active) {
				active = true;
				Refresh ();
			}
		}
		
		public void Stop ()
		{
#if DEBUG
			MonoDevelop.Ide.DispatchService.AssertGuiThread ();
#endif
			if (active) {
				active = false;
				if (source > 0)
					GLib.Source.Remove (source);
			}
		}
		
		public AsyncPoller ()
		{
			Timeout = 2000; //ms
		}
		
		public void Refresh ()
		{
#if DEBUG
			MonoDevelop.Ide.DispatchService.AssertGuiThread ();
#endif
			Stop ();
			BeginRefresh ();
		}
		
		//must call OnRefreshed when done, whether succeeds or fails
		protected virtual void BeginRefresh ()
		{
		}
		
		protected void OnRefreshed (bool changed)
		{
#if DEBUG
			MonoDevelop.Ide.DispatchService.AssertGuiThread ();
#endif
			source = GLib.Timeout.Add ((uint)Timeout, delegate {
				BeginRefresh ();
				return false;
			});
			if (changed && Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public event EventHandler Changed;
	}
}

