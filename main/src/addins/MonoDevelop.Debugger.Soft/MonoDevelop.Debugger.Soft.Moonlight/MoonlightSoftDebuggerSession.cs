// 
// IPhoneDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Mono.Debugger;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.Moonlight;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;

namespace MonoDevelop.Debugger.Soft.Moonlight
{


	public class MoonlightSoftDebuggerSession : RemoteSoftDebuggerSession
	{
		string appName;
		Process browser;
		
		protected override string AppName {
			get { return appName; }
		}
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (MoonlightDebuggerStartInfo) startInfo;
			
			appName = GetNameFromXapUrl (dsi.Url);
			
			StartBrowserProcess (dsi);
			StartListening (dsi);
		}

		void StartBrowserProcess (MoonlightDebuggerStartInfo dsi)
		{
			if (browser != null)
				throw new InvalidOperationException ("Browser already started");
			
			var psi = new ProcessStartInfo ("firefox", string.Format (" -no-remote \"{0}\"", dsi.Url)) {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			psi.EnvironmentVariables.Add ("MOON_SOFT_DEBUG",
				string.Format ("transport=dt_socket,address={0}:{1}", dsi.Address, dsi.DebugPort));
			
			browser = Process.Start (psi);
			ConnectOutput (browser.StandardOutput, false);
			ConnectOutput (browser.StandardError, true);
			
			browser.Exited += delegate {
				EndSession ();
			};
		}

		static string GetNameFromXapUrl (string url)
		{
			int start = url.LastIndexOf ('/');
			int end = url.LastIndexOf ('.');
			if (end > start)
				return url.Substring (start, end);
			return "";
		}
		
		protected override void EndSession ()
		{
			EndBrowserProcess ();
			base.EndSession ();
		}
		
		protected override void OnExit ()
		{
			EndBrowserProcess ();
			base.OnExit ();
		}
		
		void EndBrowserProcess ()
		{
			if (browser == null)
				return;
			
			browser.Kill ();
			browser = null;
		}
	}
}
