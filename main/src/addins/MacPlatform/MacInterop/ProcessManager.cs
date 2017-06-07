// 
// ProcessManager.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Runtime.InteropServices;

using AppKit;

namespace MonoDevelop.MacInterop
{
	public static class ProcessManager
	{
		const string CARBON = "/System/Library/Frameworks/Carbon.framework/Versions/A/Carbon";
		
		[DllImport (CARBON)]
		static extern OSStatus GetProcessPID (ref ProcessSerialNumber psn, out int pid);
		
		[DllImport (CARBON)]
		static extern OSStatus KillProcess (ref ProcessSerialNumber process);
		
		public static int GetProcessPid (ProcessSerialNumber psn)
		{
			int pid;
			if (GetProcessPID (ref psn, out pid) == OSStatus.Ok)
				return pid;
			return -1;
		}

		[Obsolete ("Use KillProcess (int pid) instead")]
		public static bool KillProcess (ProcessSerialNumber psn)
		{
			return KillProcess (ref psn) == OSStatus.Ok;
		}

		public static bool KillProcess (int pid)
		{
			NSRunningApplication runningApp = NSRunningApplication.GetRunningApplication (pid);
			if (runningApp == null)
				return false;

			return runningApp.ForceTerminate ();
		}

		public static bool IsRunning (int pid)
		{
			return NSRunningApplication.GetRunningApplication (pid) != null;
		}

		enum OSStatus
		{
			Ok = 0
		}
	}
	
	public struct ProcessSerialNumber
	{
		uint high;
		uint low;
		
		public ProcessSerialNumber (uint high, uint low)
		{
			this.high = high;
			this.low = low;
		}
		
		public uint High { get { return high; } }
		public uint Low { get { return low; } }
	}
}

