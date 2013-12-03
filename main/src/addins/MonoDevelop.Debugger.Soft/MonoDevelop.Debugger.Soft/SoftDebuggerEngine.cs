// 
// SoftDebuggerEngine.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Mono.Debugging.Soft;
using Mono.Debugging.Client;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Debugger.Soft
{
	public class SoftDebuggerEngine: IDebuggerEngine
	{
		static SoftDebuggerEngine ()
		{
			DebuggerLoggingService.CustomLogger = new MDLogger ();
		}
		
		public bool CanDebugCommand (ExecutionCommand cmd)
		{
			var netCmd = cmd as DotNetExecutionCommand;
			if (netCmd == null)
				return false;

			return CanDebugRuntime (netCmd.TargetRuntime);
		}

		public static bool CanDebugRuntime (TargetRuntime runtime)
		{
			var mrun = runtime as MonoTargetRuntime;
			if (mrun == null)
				return false;
			
			return mrun.AssemblyContext.GetAssemblyLocation ("Mono.Debugger.Soft", null) != null;
		}
		
		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand c)
		{
			var cmd = (DotNetExecutionCommand) c;
			var runtime = (MonoTargetRuntime)cmd.TargetRuntime;
			var dsi = new SoftDebuggerStartInfo (runtime.Prefix, runtime.EnvironmentVariables) {
				Command = cmd.Command,
				Arguments = cmd.Arguments,
				WorkingDirectory = cmd.WorkingDirectory,
			};
			
			SetUserAssemblyNames (dsi, cmd.UserAssemblyPaths);
			
			foreach (KeyValuePair<string,string> var in cmd.EnvironmentVariables)
				dsi.EnvironmentVariables [var.Key] = var.Value;
			
			var varsCopy = new Dictionary<string, string> (cmd.EnvironmentVariables);
			var startArgs = (SoftDebuggerLaunchArgs) dsi.StartArgs;
			startArgs.ExternalConsoleLauncher = delegate (System.Diagnostics.ProcessStartInfo info) {
				IProcessAsyncOperation oper;
				oper = Runtime.ProcessService.StartConsoleProcess (info.FileName, info.Arguments, info.WorkingDirectory,
					varsCopy, ExternalConsoleFactory.Instance.CreateConsole (dsi.CloseExternalConsoleOnExit), null);
				return new ProcessAdapter (oper, Path.GetFileName (info.FileName));
			};

			return dsi;
		}
		
		public ProcessInfo[] GetAttachableProcesses ()
		{
			return new ProcessInfo [0];
		}
		
		public DebuggerSession CreateSession ()
		{
			return new SoftDebuggerSession ();
		}
		
		public static void SetUserAssemblyNames (SoftDebuggerStartInfo dsi, IList<string> files)
		{
			if (files == null || files.Count == 0)
				return;
			
			var pathMap = new Dictionary<string, string> ();
			var names = new List<AssemblyName> ();
			
			foreach (var file in files) {
				if (!File.Exists (file)) {
					dsi.LogMessage = GettextCatalog.GetString ("User assembly '{0}' is missing. " +
						"Debugger will now debug all code, not just user code.", file);
					return;
				}
				
				try {
					var asm = Mono.Cecil.AssemblyDefinition.ReadAssembly (file);
					if (string.IsNullOrEmpty (asm.Name.Name))
						throw new InvalidOperationException ("Assembly has no assembly name");
					
					AssemblyName name = new AssemblyName (asm.Name.FullName);
					if (!pathMap.ContainsKey (asm.Name.FullName))
						pathMap.Add (asm.Name.FullName, file);
					names.Add (name);
				} catch (Exception ex) {
					dsi.LogMessage = GettextCatalog.GetString ("Could not get assembly name for user assembly '{0}'. " +
						"Debugger will now debug all code, not just user code.", file);
					LoggingService.LogError ("Error getting assembly name for user assembly '" + file + "'", ex);
					return;
				}
			}
			
			dsi.UserAssemblyNames = names;
			dsi.AssemblyPathMap = pathMap;
		}
		
		class MDLogger : ICustomLogger
		{
			public void LogError (string message, Exception ex)
			{
				LoggingService.LogError (message, ex);
			}
			
			public void LogAndShowException (string message, Exception ex)
			{
				LogError (message, ex);
				MonoDevelop.Ide.MessageService.ShowException (ex, message);
			}

			public void LogMessage (string messageFormat, params object[] args)
			{
				LoggingService.LogInfo (messageFormat, args);
			}
		}
	}
	
	class ProcessAdapter: Mono.Debugger.Soft.ITargetProcess
	{
		IProcessAsyncOperation oper;
		string name;
		
		public ProcessAdapter (IProcessAsyncOperation oper, string name)
		{
			this.oper = oper;
			this.name = name;
			oper.Completed += delegate {
				if (Exited != null)
					Exited (this, EventArgs.Empty);
			};
		}
		
		#region IProcess implementation
		public event EventHandler Exited;
		
		
		public void Kill ()
		{
			oper.Cancel ();
		}
		
		
		public StreamReader StandardOutput {
			get {
				// Not supported in external console
				throw new System.NotSupportedException ();
			}
		}
		
		
		public StreamReader StandardError {
			get {
				// Not supported in external console
				throw new System.NotSupportedException ();
			}
		}
		
		
		public bool HasExited {
			get {
				return oper.IsCompleted;
			}
		}
		
		
		public int Id {
			get {
				return oper.ProcessId;
			}
		}
		
		
		public string ProcessName {
			get {
				return name;
			}
		}
		
		#endregion
	}
}
