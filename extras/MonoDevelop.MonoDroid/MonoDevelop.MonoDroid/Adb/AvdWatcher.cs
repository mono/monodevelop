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
	class AvdWatcher : IDisposable
	{
		object lockObj = new object ();
		FileSystemWatcher fsw;
		
		uint timeoutId;
		FilePath avdDir;
		Dictionary<string,DateTime> modTimes;
		
		//TODO: handle errors
		public AvdWatcher ()
		{
			VirtualDevices = new AndroidVirtualDevice[0];
			
			FilePath home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			avdDir = home.Combine (".android", "avd");
			if (!Directory.Exists (avdDir))
				Directory.CreateDirectory (avdDir);
			
			var avds = Directory.GetFiles (avdDir, "*.ini");
			UpdateAvds (avds, null);
			
			//FSW on mac is unreliable
			if (PropertyService.IsMac) {
				modTimes = new Dictionary<string, DateTime> ();
				foreach (var f in avds)
					modTimes[f] = File.GetLastWriteTimeUtc (f);
				timeoutId = GLib.Timeout.Add (750, HandleTimeout);
			} else {
				CreateFsw ();
			}
		}
		
		public IList<AndroidVirtualDevice> VirtualDevices { get; private set; }
		
		public event Action<IList<AndroidVirtualDevice>> Changed;
		
		void OnChanged ()
		{
			try {
				var c = Changed;
				if (c != null)
					c (VirtualDevices);
			} catch (Exception ex) {
				LoggingService.LogError ("Error in AvdWatcher change event handler", ex);
			}
		}
		
		void UpdateAvds (IEnumerable<string> addedOrChangedInis, IEnumerable<string> removedInis)
		{
			List<AndroidVirtualDevice> toAdd = null;
			
			if (addedOrChangedInis != null) {
				toAdd = new List<AndroidVirtualDevice> ();
				foreach (var ini in addedOrChangedInis) {
					try {
						var avd = AndroidVirtualDevice.Load (ini);
						toAdd.Add (avd);
					} catch (Exception ex) {
						LoggingService.LogError ("Error loading avd " + ini, ex);
					}
				}
			}
			
			if ((toAdd != null && toAdd.Count > 0) || (removedInis != null))
				UpdateList (toAdd, removedInis);
		}
		
		void UpdateList (IEnumerable<AndroidVirtualDevice> toAdd, IEnumerable<string> toRemoveIniFiles)
		{
			lock (lockObj) {
				var dict = new Dictionary<string,AndroidVirtualDevice> ();
				foreach (var avd in VirtualDevices)
					dict[avd.Name] = avd;
				
				if (toRemoveIniFiles != null)
					foreach (var r in toRemoveIniFiles)
						dict.Remove (Path.GetFileNameWithoutExtension (r));
				
				if (toAdd != null)
					foreach (var a in toAdd)
						dict[a.Name] = a;
				
				var l = new AndroidVirtualDevice[dict.Count];
				int i = 0;
				foreach (var kvp in dict)
					l[i++] = kvp.Value;
				VirtualDevices = l;
			}
			OnChanged ();
		}
		
		public void Dispose ()
		{
			if (fsw == null && timeoutId == 0)
				return;
			lock (lockObj) {
				if (fsw != null) {
					fsw.Dispose ();
					fsw = null;
				}
				if (timeoutId > 0) {
					GLib.Source.Remove (timeoutId);
					timeoutId = 0;
				}
			}
		}
		
		void CreateFsw ()
		{
			fsw = new System.IO.FileSystemWatcher (avdDir, "*.ini");
			fsw.Changed += delegate (object sender, FileSystemEventArgs e) {
				UpdateAvds (new string[] { e.FullPath }, null);
			};
			fsw.Renamed += delegate (object sender, RenamedEventArgs e) {
				UpdateAvds (new string[] { e.FullPath }, new string[] { e.OldFullPath });
			};
			fsw.Created += delegate (object sender, FileSystemEventArgs e) {
				UpdateAvds (new string[] { e.FullPath }, null);
			};
			//FIXME: this seems a bit flaky, for some files we don't get the delete event
			fsw.Deleted += delegate (object sender, FileSystemEventArgs e) {
				UpdateAvds (null, new string[] { e.FullPath });
			};
			fsw.EnableRaisingEvents = true;
		}

		bool HandleTimeout ()
		{
			try {
				if (!Directory.Exists (avdDir)) {
					if (VirtualDevices.Count > 0)
						VirtualDevices = new AndroidVirtualDevice[0];
					return true;
				}
				
				string [] files = Directory.GetFiles (avdDir, "*.ini");
				if (files.Length == 0 && modTimes.Count == 0)
					return true;
				
				var addedOrChanged = new HashSet<string> (files);
				var removed = new HashSet<string> (modTimes.Keys);
				foreach (string f in files) {
					removed.Remove (f);
					var modified = File.GetLastWriteTimeUtc (f);
					if (modTimes.ContainsKey (f) && modTimes[f] == modified)
						addedOrChanged.Remove (f);
					else
						modTimes[f] = modified;
				}
				foreach (var f in removed)
					modTimes.Remove (f);
				
				if (addedOrChanged.Count == 0)
					addedOrChanged = null;
				if (removed.Count == 0)
					removed = null;
				if (addedOrChanged != null || removed != null)
					UpdateAvds (addedOrChanged, removed);
			} catch (Exception ex) {
				LoggingService.LogError ("Error in AvdWatcher timeout", ex);
				timeoutId = 0;
				return false;
			}
			return true;
		}
	}
}
