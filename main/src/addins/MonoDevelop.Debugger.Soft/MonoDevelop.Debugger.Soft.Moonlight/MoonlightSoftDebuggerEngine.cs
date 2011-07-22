// 
// MoonlightSoftDebuggerEngine.cs
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
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Debugger;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Moonlight;
using Mono.Debugging.Client;
using MonoDevelop.Debugger.Soft;
using System.Net;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Soft.Moonlight
{
	public class MoonlightSoftDebuggerEngine: IDebuggerEngine
	{
		public bool CanDebugCommand (ExecutionCommand command)
		{
			if (Platform.IsMac || Platform.IsWindows)
				return false;
			
			var cmd = command as MoonlightExecutionCommand;
			return cmd != null && cmd.Url.StartsWith ("file://");
		}
		
		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			var cmd = (MoonlightExecutionCommand) command;
			var msi = new MoonlightDebuggerStartInfo (cmd.AppName, cmd.Url);
			SoftDebuggerEngine.SetUserAssemblyNames (msi, cmd.UserAssemblyPaths);
			return msi;
		}

		public DebuggerSession CreateSession ()
		{
			return new MoonlightSoftDebuggerSession ();
		}
		
		public ProcessInfo[] GetAttachableProcesses ()
		{
			return new ProcessInfo[0];
		}
	}
	
	class MoonlightDebuggerStartInfo : SoftDebuggerStartInfo
	{
		public string Url { get; private set; }
		
		public MoonlightDebuggerStartInfo (string appName, string url)
			: base (new SoftDebuggerListenArgs (appName, IPAddress.Loopback, 0))
		{
			this.Url = url;
		}
	}
}
