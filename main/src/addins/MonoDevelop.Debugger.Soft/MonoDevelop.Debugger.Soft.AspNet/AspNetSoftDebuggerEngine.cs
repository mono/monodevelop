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
			AspNetExecutionCommand cmd = (AspNetExecutionCommand) command;
			
			var startInfo = new SoftDebuggerStartInfo ((MonoTargetRuntime)cmd.TargetRuntime) {
				WorkingDirectory = cmd.BaseDirectory,
				Arguments = cmd.XspParameters.GetXspParameters ().Trim (),
			};
			
			string xspPath = Path.Combine (startInfo.MonoPrefix, "lib" + Path.DirectorySeparatorChar + "mono" + Path.DirectorySeparatorChar);
			if (cmd.ClrVersion == ClrVersion.Net_1_1)
				xspPath += Path.Combine ("1.0","xsp.exe");
			else
				xspPath += Path.Combine ("2.0","xsp2.exe");
			startInfo.Command = xspPath;
			
			return startInfo;
		}

		public DebuggerFeatures SupportedFeatures {
			get {
				return DebuggerFeatures.Breakpoints | 
					   DebuggerFeatures.Pause | 
					   DebuggerFeatures.Stepping | 
					   DebuggerFeatures.DebugFile |
					   DebuggerFeatures.Catchpoints;
			}
		}
		
		public DebuggerSession CreateSession ()
		{
			return new SoftDebuggerSession ();
		}
		
		public ProcessInfo[] GetAttachableProcesses ()
		{
			return new ProcessInfo[0];
		}
		
		public string Name {
			get {
				return "Mono Soft Debugger";
			}
		}
	}
}
