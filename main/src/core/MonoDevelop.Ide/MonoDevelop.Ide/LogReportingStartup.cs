// 
// LogReportingStartup.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011, Xamarin Inc.
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
using System.Diagnostics;
using System.IO;
using System.Linq;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.LogReporting;


namespace MonoDevelop.Ide
{
	public class LogReportingStartup : CommandHandler
	{
		ICrashMonitor Monitor = new CrashMonitor ();
		protected override void Run ()
		{
			if (Platform.IsMac) {
				var crashmonitor = Path.Combine (PropertyService.EntryAssemblyPath, "MonoDevelopLogAgent.app");
				var pid = Process.GetCurrentProcess ().Id;
				var logPath = LogReportingService.CrashLogDirectory;
				
				var info = new DirectoryInfo (logPath);
				if (!LogReportingService.ReportCrashes.HasValue && info.Exists && info.EnumerateFiles ().Any ()) {
					var result = MessageService.AskQuestion ("A crash has been detected",
						"MonoDevelop has crashed recently. Details of this crash along with anonymous installation " +
						"information can be uploaded to Xamarin to help diagnose the issue. This information " +
						"will be used to help diagnose the crash and notify you of potential workarounds " +
						"or fixes. Do you wish to upload this information?",
						AlertButton.Yes, AlertButton.No);
					LogReportingService.ReportCrashes = result == AlertButton.Yes;
				}

				var psi = new ProcessStartInfo ("open", string.Format ("-a {0} -n --args -pid {1} -log {2} -session {3}", crashmonitor, pid, logPath, SystemInformation.SessionUuid)) {
					UseShellExecute = false,
				};
				Process.Start (psi);
			}
		}
	}
}

