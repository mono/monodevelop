// 
// OptionsParser.cs
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

namespace MonoDevelop.CrashLog
{
	public static class CrashLogOptions
	{
		public static string LogPath {
			get; private set;
		}

		public static int Pid {
			get; private set;
		}
		
		public static string SessionUuid {
			get; private set;
		}
		
		public static bool TryParse (string[] args, out string error)
		{
			error = null;
			Pid = -1;
			
			for (int i = 0; i < args.Length; i ++) {
				if (args [i] == "-pid") {
					Pid = int.Parse (args [++ i]);
				}
				if (args [i] == "-log") {
					LogPath = args [++ i];
				}
				if (args[i] == "-session") {
					SessionUuid = args [++ i];
				}
			}
			
			if (Pid == -1) {
				error = "The pid of the MonoDevelop process being monitored must be supplied";
			} else if (string.IsNullOrEmpty (LogPath)) {
				error = "The path to write log files to must be supplied";
			} else if (string.IsNullOrEmpty (SessionUuid)) {
				error = "The session uuid must be supplied";
			}
			
			
			return error == null;
		}
	}
}

