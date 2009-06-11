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
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;
using Mono.Debugging.Backend.Mdb;

namespace MonoDevelop.Debugger.Mdb
{
	public class MonoDebuggerSessionFactory: IDebuggerEngine
	{
		public string Name {
			get { return "Mono Debugger"; }
		}
		
		public bool CanDebugCommand (ExecutionCommand command)
		{
			DotNetExecutionCommand cmd = command as DotNetExecutionCommand;
			return cmd != null && DebuggingSupported (cmd.TargetRuntime);
		}
		
		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand command)
		{
			DotNetExecutionCommand cmd = (DotNetExecutionCommand) command;
			MonoDebuggerStartInfo startInfo = CreateDebuggerStartInfo (cmd.TargetRuntime);
			
			startInfo.Command = cmd.Command;
			startInfo.Arguments = cmd.Arguments;
			startInfo.WorkingDirectory = cmd.WorkingDirectory;
			if (cmd.EnvironmentVariables.Count > 0) {
				foreach (KeyValuePair<string,string> val in cmd.EnvironmentVariables)
					startInfo.EnvironmentVariables [val.Key] = val.Value;
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
			List<ProcessInfo> procs = new List<ProcessInfo> ();
			foreach (string dir in Directory.GetDirectories ("/proc")) {
				int id;
				if (!int.TryParse (Path.GetFileName (dir), out id))
					continue;
				try {
					File.ReadAllText (Path.Combine (dir, "sessionid"));
				} catch {
					continue;
				}
				string cmdline = File.ReadAllText (Path.Combine (dir, "cmdline"));
				cmdline = cmdline.Replace ('\0',' ');
				ProcessInfo pi = new ProcessInfo (id, cmdline);
				procs.Add (pi);
			}
			return procs.ToArray ();
		}
		
		public static bool DebuggingSupported (TargetRuntime tr)
		{
			if (tr == null) tr = MonoDevelop.Core.Runtime.SystemAssemblyService.DefaultRuntime;
			return (tr is MonoTargetRuntime) && tr.GetPackage ("mono-debugger") != null;
		}
		
		public static MonoDebuggerStartInfo CreateDebuggerStartInfo (TargetRuntime tr)
		{
			if (tr == null) tr = MonoDevelop.Core.Runtime.SystemAssemblyService.DefaultRuntime;
			MonoDebuggerStartInfo startInfo = new MonoDebuggerStartInfo ();
			MonoTargetRuntime mtr = (MonoTargetRuntime) tr;
			startInfo.ServerEnvironment = mtr.EnvironmentVariables;
			startInfo.MonoPrefix = mtr.Prefix;
			return startInfo;
		}
	}
}
