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
using System.Collections.Generic;
using Mono.Debugging.Client;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;
using System.IO;
using System.Reflection;
using Mono.Debugging.Soft;
using MDLS = MonoDevelop.Core.LoggingService;

namespace MonoDevelop.Debugger.Soft
{
	public class SoftDebuggerEngine: IDebuggerEngine
	{
		public SoftDebuggerEngine ()
		{
			EnsureSdbLoggingService ();
		}
		
		public string Id {
			get {
				return "Mono.Debugger.Soft";
			}
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
			
			string error;
			dsi.UserAssemblyNames = GetAssemblyNames (cmd.UserAssemblyPaths, out error);
			dsi.LogMessage = error;
			
			foreach (KeyValuePair<string,string> var in cmd.EnvironmentVariables)
				dsi.EnvironmentVariables [var.Key] = var.Value;
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
		
		public string Name {
			get {
				return "Mono Soft Debugger";
			}
		}
		
		public DebuggerFeatures SupportedFeatures {
			get {
				return DebuggerFeatures.Breakpoints | 
					   DebuggerFeatures.Pause | 
					   DebuggerFeatures.Stepping | 
					   DebuggerFeatures.DebugFile |
					   DebuggerFeatures.ConditionalBreakpoints |
					   DebuggerFeatures.Tracepoints |
					   DebuggerFeatures.Catchpoints;
			}
		}
		
		public static List<AssemblyName> GetAssemblyNames (IList<string> files, out string error)
		{
			error = null;
			if (files == null || files.Count == 0)
				return null;
			
			var names = new List<AssemblyName> ();
			foreach (var file in files) {
				if (!File.Exists (file)) {
					error = GettextCatalog.GetString ("User assembly '{0}' is missing. " +
						"Debugger will now debug all code, not just user code.", file);
					return null;
				}
				try {
					var asm = Mono.Cecil.AssemblyFactory.GetAssemblyManifest (file);
					if (string.IsNullOrEmpty (asm.Name.Name))
						throw new InvalidOperationException ("Assembly has no assembly name");
					names.Add (new AssemblyName (asm.Name.FullName));
				} catch (Exception ex) {
					error = GettextCatalog.GetString ("Could not get assembly name for user assembly '{0}'. " +
						"Debugger will now debug all code, not just user code.", file);
					MDLS.LogError ("Error getting assembly name for user assembly '" + file + "'", ex);
					return null;
				}
			}
			return names;
		}
		
		static ICustomLogger logger;
		internal static void EnsureSdbLoggingService ()
		{
			if (logger == null)
				Mono.Debugging.Soft.LoggingService.CustomLogger = logger = new MDLogger ();
		}
		
		class MDLogger : ICustomLogger
		{
			public void LogError (string message, Exception ex)
			{
				MonoDevelop.Core.LoggingService.LogError (message, ex);
			}
			
			public void LogAndShowException (string message, Exception ex)
			{
				LogError (message, ex);
				MonoDevelop.Core.Gui.MessageService.ShowException (ex, message);
			}
		}
	}
}
