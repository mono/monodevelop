// 
// AppDelegate.cs
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
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

using MonoDevelop;
using MonoDevelop.Core.LogReporting;

namespace MacCrashLogger
{
	public partial class AppDelegate : NSApplicationDelegate
	{
//		ICrashMonitor Monitor {
//			get; set;
//		}
//		
//		bool ProcessingCrashLog {
//			get; set;
//		}
//		
//		CrashReporter Reporter {
//			get; set;
//		}
//		
//		bool ShouldExit {
//			get; set;
//		}
//		
//		public AppDelegate ()
//		{
//			Reporter = new CrashReporter (CrashLogOptions.LogPath, CrashLogOptions.Email);
//			
//			Monitor = CrashMonitor.Create (CrashLogOptions.Pid);
//			Monitor.ApplicationExited += HandleMonitorApplicationExited;
//			Monitor.CrashDetected += HandleMonitorCrashDetected;
//		}
//		
//		public override void FinishedLaunching (NSObject notification)
//		{
//			Monitor.Start ();
//			Reporter.ProcessCache ();
//		}
//		
//		void HandleMonitorCrashDetected (object sender, CrashEventArgs e)
//		{
//			InvokeOnMainThread (() => {
//				try {
//					ProcessingCrashLog = true;
//					Reporter.UploadOrCache (e.CrashLogPath);
//					
//					if (ShouldExit)
//						NSApplication.SharedApplication.Terminate (this);
//				} finally {
//					ProcessingCrashLog = false;
//				}
//			});
//		}
//
//		void HandleMonitorApplicationExited (object sender, EventArgs e)
//		{
//			InvokeOnMainThread (() => {
//				ShouldExit = true;
//				if (!ProcessingCrashLog) {
//					NSApplication.SharedApplication.Terminate (null);
//				}
//			});
//		}
	}
}