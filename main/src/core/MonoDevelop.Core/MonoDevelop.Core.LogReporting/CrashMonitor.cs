// 
// CrashMonitor.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright 2011, Xamarin Inc.
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
using System.IO;
using System.Threading;

namespace MonoDevelop.Core.LogReporting
{
	public abstract class CrashMonitor : ICrashMonitor
	{
		public static ICrashMonitor Create (int pid)
		{
			// FIXME: Add in proper detection for mac/windows/linux as appropriate
			return new MacCrashMonitor (pid);
		}
		
		public event EventHandler ApplicationExited;
		public event EventHandler<CrashEventArgs> CrashDetected;
		
		public int Pid {
			get; private set;
		}
		
		FileSystemWatcher Watcher {
			get; set;
		}
		
		protected CrashMonitor (int pid, string path)
			: this (pid, path, "")
		{
			
		}
		
		protected CrashMonitor (int pid, string path, string filter)
		{
			Pid = pid;
			Watcher = new FileSystemWatcher (path, filter);
			Watcher.Created += (o, e) => {
				OnCrashDetected (new CrashEventArgs (e.FullPath));
			};
			
			// Wait for the parent MD process to exit. This could be a crash
			// or a graceful exit.
			ThreadPool.QueueUserWorkItem (o => {
				// Do a loop rather than calling WaitForExit or hooking into
				// the Exited event as those do not work on MacOS on mono 2.10.3
				var info = System.Diagnostics.Process.GetProcessById (Pid);
				while (!info.HasExited) {
					Thread.Sleep (1000);
					info.Refresh ();
				}
				
				// If the application has crashed we want to wait a few seconds before
				// raising this event so we allow time for the native crash reporter to
				// write its log files.
				Thread.Sleep (5000);
				OnApplicationExited ();
			});
		}
		
		protected virtual void OnApplicationExited ()
		{
			if (ApplicationExited != null)
				ApplicationExited (this, EventArgs.Empty);
		}
		
		protected virtual void OnCrashDetected (CrashEventArgs e)
		{
			if (CrashDetected != null)
				CrashDetected (this, e);
		}

		public void Start () {
			Watcher.EnableRaisingEvents = true;
		}

		public void Stop () {
			Watcher.EnableRaisingEvents = false;
		}
	}
}