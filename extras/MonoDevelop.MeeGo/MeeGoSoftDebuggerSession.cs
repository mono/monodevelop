// 
// MeeGoDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

using System.Net;
using MonoDevelop.Debugger.Soft;
using Mono.Debugging.Soft;
using Mono.Debugging.Client;
using MonoDevelop.Core.Execution;
using System.IO;

namespace MonoDevelop.MeeGo
{
	public class MeeGoSoftDebuggerSession : Mono.Debugging.Soft.SoftDebuggerSession
	{
		SshRemoteProcess process;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (MeeGoSoftDebuggerStartInfo) startInfo;
			StartProcess (dsi);
			StartListening (dsi);
		}
		
		protected override string GetConnectingMessage (DebuggerStartInfo startInfo)
		{
			var dsi = (MeeGoSoftDebuggerStartInfo) startInfo;
			var dra = (SoftDebuggerRemoteArgs) dsi.StartArgs;

			return string.Format ("Waiting for debugger to connect on {0}:{1}...", dra.Address, dra.DebugPort);
		}
		
		protected override void EndSession ()
		{
			base.EndSession ();
			EndProcess ();
		}
		
		void StartProcess (MeeGoSoftDebuggerStartInfo dsi)
		{
			MeeGoUtility.Upload (dsi.Device, dsi.ExecutionCommand.Config, null, null).WaitForCompleted ();
			var auth = MeeGoExecutionHandler.GetGdmXAuth (dsi.Device);
			var dra = (SoftDebuggerRemoteArgs) dsi.StartArgs;
			string debugOptions = string.Format ("transport=dt_socket,address={0}:{1}", dra.Address, dra.DebugPort);
			
			process = MeeGoExecutionHandler.CreateProcess (dsi.ExecutionCommand, debugOptions, dsi.Device, auth,
			                                               x => OnTargetOutput (false, x),
			                                               x => OnTargetOutput (true, x));
			
			process.Completed += delegate {
				process = null;
			};
			
			TargetExited += delegate {
				EndProcess ();
			};
			
			process.Run ();
		}
		
		void EndProcess ()
		{
			if (process == null)
				return;
			if (!process.IsCompleted) {
				try {
					process.Cancel ();
				} catch {}
			}
		}
		
		protected override void OnExit ()
		{
			base.OnExit ();
			EndProcess ();
		}
	}
	
	class MeeGoSoftDebuggerStartInfo : SoftDebuggerStartInfo
	{
		public MeeGoExecutionCommand ExecutionCommand { get; private set; }
		public MeeGoDevice Device { get; private set; }
		
		public MeeGoSoftDebuggerStartInfo (IPAddress address, int debugPort, MeeGoExecutionCommand cmd, MeeGoDevice device)
			: base (new SoftDebuggerListenArgs (cmd.Name, address, debugPort))
		{
			ExecutionCommand = cmd;
			Device = device;
		}
	}
}
