// MonoDebuggerFactory.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using MonoDevelop.Debugger;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.AspNet;
using Mono.Debugging.Backend.Mdb;

namespace MonoDevelop.Debugger.Mdb
{
	public class MonoXspDebuggerSessionFactory: IDebuggerEngine
	{
		public string Name {
			get { return "Mono Debugger"; }
		}
		
		public bool CanDebugCommand (ExecutionCommand command)
		{
			AspNetExecutionCommand cmd = command as AspNetExecutionCommand;
			return cmd != null && MonoDebuggerSessionFactory.DebuggingSupported (cmd.TargetRuntime);
		}
		
		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			AspNetExecutionCommand cmd = (AspNetExecutionCommand) command;
			MonoDebuggerStartInfo startInfo = MonoDebuggerSessionFactory.CreateDebuggerStartInfo (cmd.TargetRuntime);
			
			string xspPath = Path.Combine (startInfo.MonoPrefix, "lib" + Path.DirectorySeparatorChar + "mono" + Path.DirectorySeparatorChar);
			
			if (cmd.ClrVersion == ClrVersion.Net_1_1)
				xspPath += Path.Combine ("1.0","xsp.exe");
			else
				xspPath += Path.Combine ("2.0","xsp2.exe");
			
			startInfo.IsXsp = true;
			startInfo.UserCodeOnly = true;
			startInfo.Command = xspPath;
			startInfo.WorkingDirectory = cmd.BaseDirectory;
			startInfo.Arguments = cmd.XspParameters.GetXspParameters ().Trim ();
			
			string binDir = Path.Combine (cmd.BaseDirectory, "bin");
			startInfo.UserModules = new List<string> ();
			foreach (string file in Directory.GetFiles (binDir)) {
				if (file.EndsWith (".dll") || file.EndsWith (".exe"))
					startInfo.UserModules.Add (file);
			}
			
			return startInfo;
		}

		public DebuggerFeatures SupportedFeatures {
			get {
				return DebuggerFeatures.All & ~(
				     DebuggerFeatures.ConditionalBreakpoints |
				     DebuggerFeatures.Tracepoints |
				     DebuggerFeatures.Attaching);
			}
		}
		
		public DebuggerSession CreateSession ()
		{
			MonoDebuggerSession ds = new MonoDebuggerSession ();
			ds.StartDebugger ();
			return ds;
		}
		
		public ProcessInfo[] GetAttachablePocesses ()
		{
			return new ProcessInfo[0];
		}
	}
}
