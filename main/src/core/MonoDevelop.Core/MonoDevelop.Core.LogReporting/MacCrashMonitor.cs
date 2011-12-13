// 
// MacCrashMonitor.cs
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

namespace MonoDevelop.Core.LogReporting
{
	class MacCrashMonitor : CrashMonitor
	{
		static readonly string CrashLogDirectory = 
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Library/Logs/CrashReporter");
		
		public MacCrashMonitor (int pid)
			: base (pid, CrashLogDirectory, "mono*")
		{
			
		}
		
		protected override void OnCrashDetected (CrashEventArgs e)
		{
			if (IsFromMonitoredPid (e.CrashLogPath))
				base.OnCrashDetected (e);
		}
		
		bool IsFromMonitoredPid (string logPath)
		{
			int parsedPid;
			
			using (var reader = new StreamReader (File.OpenRead (logPath))) {
				// First line of a macos crash dump should look like:
				// Process:         mono [1132]
				var line = reader.ReadLine ();
				if (string.IsNullOrEmpty (line) || !line.StartsWith ("Process"))
					return false;
				
				var startIndex = line.LastIndexOf ('[');
				var endIndex = line.LastIndexOf  (']');
				if (startIndex < 0 || endIndex < 0)
					return false;

				startIndex ++; // We don't want to include the '['
				var number = line.Substring (startIndex, endIndex  - startIndex);
				if (!int.TryParse (number, out parsedPid))
					return false;

				Console.WriteLine ("Parsed Pid was: {0}", parsedPid);
				return parsedPid ==  Pid;
			}
		}
	}
}
