// 
// MonoMacDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Generic;
using Mono.Debugger.Soft;
using Mono.Debugging;
using Mono.Debugging.Soft;
using Mono.Debugging.Client;
using MonoDevelop.MonoMac;
using MonoDevelop.Core;
using MonoDevelop.MacInterop;

namespace MonoDevelop.Debugger.Soft.MonoMac
{

	public class MonoMacDebuggerSession : SoftDebuggerSession
	{
		MonoMacProcess process;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (MonoMacDebuggerStartInfo) startInfo;
			var startArgs = (SoftDebuggerRemoteArgs) dsi.StartArgs;
			var cmd = dsi.ExecutionCommand;		
			
			int assignedPort;
			StartListening (dsi, out assignedPort);
			
			Action<string> stdout = s => OnTargetOutput (false, s);
			Action<string> stderr = s => OnTargetOutput (true, s);
			
			var asi = new ApplicationStartInfo (cmd.AppPath);
			asi.Environment ["MONOMAC_DEBUGLAUNCHER_OPTIONS"]
				= string.Format ("--debug --debugger-agent=transport=dt_socket,address={0}:{1}", startArgs.Address, assignedPort);
			
			process = MonoMacExecutionHandler.OpenApplication (cmd, asi, stdout, stderr);
			
			process.Completed += delegate {
				EndSession ();
				process = null;
			};
		}
		
		protected override void EnsureExited ()
		{
			try {
				if (process != null && !process.IsCompleted)
					process.Cancel ();
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Error force-terminating soft debugger process", ex);
			}
		}
	}
	
	class MonoMacDebuggerStartInfo : SoftDebuggerStartInfo
	{
		public MonoMacExecutionCommand ExecutionCommand { get; private set; }
		
		public MonoMacDebuggerStartInfo (MonoMacExecutionCommand cmd)
			: base (new SoftDebuggerListenArgs (cmd.AppPath.FileNameWithoutExtension, IPAddress.Loopback, 0))
		{
			ExecutionCommand = cmd;
		}
	}
}
