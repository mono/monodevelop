// 
// MonoDroidDebuggerSession.cs
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

using System;
using Mono.Debugger.Soft;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.MonoDroid;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace MonoDevelop.Debugger.Soft.MonoDroid
{


	public class MonoDroidDebuggerSession : RemoteSoftDebuggerSession
	{
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (MonoDroidDebuggerStartInfo) startInfo;
			var cmd = dsi.ExecutionCommand;
			
			//start app
			throw new NotImplementedException ();
			
			StartListening (dsi);
		}
		
		protected override string GetListenMessage (RemoteDebuggerStartInfo dsi)
		{
			//var cmd = ((MonoDroidDebuggerStartInfo)dsi).ExecutionCommand;
			string message = GettextCatalog.GetString ("Waiting for debugger to connect on {0}:{1}...", dsi.Address, dsi.DebugPort);
			return message;
		}
	}
	
	class MonoDroidDebuggerStartInfo : RemoteDebuggerStartInfo
	{
		public MonoDroidExecutionCommand ExecutionCommand { get; private set; }
		
		public MonoDroidDebuggerStartInfo (IPAddress address, int debugPort, int outputPort, MonoDroidExecutionCommand cmd)
			: base (cmd.PackageName, address, debugPort, outputPort)
		{
			ExecutionCommand = cmd;
		}
	}
}
