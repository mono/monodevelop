// 
// AspNetSoftDebuggerEngine.cs
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
using MonoDevelop.AspNet;
using Mono.Debugging.Client;
using MonoDevelop.Debugger.Soft;
using System.Net;
using MonoDevelop.Core.Assemblies;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Soft.AspNet
{
	public class AspNetSoftDebuggerEngine: IDebuggerEngine
	{
		public bool CanDebugCommand (ExecutionCommand command)
		{
			var cmd = command as AspNetExecutionCommand;
			return cmd != null && SoftDebuggerEngine.CanDebugRuntime (cmd.TargetRuntime);
		}
		
		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			var cmd = (AspNetExecutionCommand) command;
			
			var runtime = (MonoTargetRuntime)cmd.TargetRuntime;
			var startInfo = new SoftDebuggerStartInfo (runtime.Prefix, runtime.EnvironmentVariables) {
				WorkingDirectory = cmd.BaseDirectory,
				Arguments = cmd.XspParameters.GetXspParameters ().Trim (),
			};
			
			FilePath prefix = runtime.Prefix;
			startInfo.Command = (cmd.ClrVersion == ClrVersion.Net_1_1)
				? prefix.Combine ("lib", "mono", "1.0", "xsp.exe")
				: prefix.Combine ("lib", "mono", "2.0", "xsp2.exe");
			
			string error;
			startInfo.UserAssemblyNames = SoftDebuggerEngine.GetAssemblyNames (cmd.UserAssemblyPaths, out error);
			startInfo.LogMessage = error;
			
			return startInfo;
		}

		public DebuggerSession CreateSession ()
		{
			return new SoftDebuggerSession ();
		}
		
		public ProcessInfo[] GetAttachableProcesses ()
		{
			return new ProcessInfo[0];
		}
	}
}
