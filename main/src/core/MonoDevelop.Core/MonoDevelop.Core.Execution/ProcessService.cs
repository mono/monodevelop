// ProcessService.cs
//
// Author:
//   Sander Rijken <sr+ximianbugs@d-90.nl>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2004 Sander Rijken
// Copyright (c) 2004 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using System.Diagnostics;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Instrumentation;
using Mono.Addins;
using System.Threading.Tasks;

namespace MonoDevelop.Core.Execution
{
	public class ProcessService
	{
		ProcessHostController externalProcess;
		List<ExtensionNode> executionHandlers;
		DefaultExecutionModeSet defaultExecutionModeSet = new DefaultExecutionModeSet ();
		IExecutionHandler defaultExecutionHandler = new DefaultExecutionHandler ();
		IExecutionMode defaultExecutionMode = new DefaultExecutionMode ();
		ExternalConsoleHandler externalConsoleHandler;
		
		const string ExecutionModesExtensionPath = "/MonoDevelop/Core/ExecutionModes";

		Dictionary<string, string> environmentVariableOverrides = null;
		
		public IDictionary<string, string> EnvironmentVariableOverrides {
			get {
				if (environmentVariableOverrides == null)
					environmentVariableOverrides = new Dictionary<string,string> ();
				return environmentVariableOverrides;
			}
		}
		
		void ProcessEnvironmentVariableOverrides (ProcessStartInfo info)
		{
			if (environmentVariableOverrides == null)
				return;
			foreach (KeyValuePair<string, string> kvp in environmentVariableOverrides) {
				if (kvp.Value == null && info.EnvironmentVariables.ContainsKey (kvp.Key))
					info.EnvironmentVariables.Remove (kvp.Key);
				else
					info.EnvironmentVariables[kvp.Key] = kvp.Value;
			}
		}
		
		internal ProcessService ()
		{
		}
		
		public void SetExternalConsoleHandler (ExternalConsoleHandler handler)
		{
			if (externalConsoleHandler != null)
				throw new InvalidOperationException ("External console handler already set");
			externalConsoleHandler = handler;
		}
		
		public ProcessWrapper StartProcess (string command, string arguments, string workingDirectory, EventHandler exited) 
		{
			return StartProcess (command, arguments, workingDirectory, (ProcessEventHandler)null, (ProcessEventHandler)null, exited);	
		}
		
		public ProcessWrapper StartProcess (string command, string arguments, string workingDirectory, ProcessEventHandler outputStreamChanged, ProcessEventHandler errorStreamChanged)
		{	
			return StartProcess (command, arguments, workingDirectory, outputStreamChanged, errorStreamChanged, null);
		}
		
		public ProcessWrapper StartProcess (string command, string arguments, string workingDirectory, TextWriter outWriter, TextWriter errorWriter, EventHandler exited) 
		{
			return StartProcess (command, arguments, workingDirectory, outWriter, errorWriter, exited, false);
		}

		public ProcessWrapper StartProcess (string command, string arguments, string workingDirectory, TextWriter outWriter, TextWriter errorWriter, EventHandler exited, bool redirectStandardInput) 
		{
			ProcessEventHandler wout = OutWriter.GetWriteHandler (outWriter);
			ProcessEventHandler werr = OutWriter.GetWriteHandler (errorWriter);
			return StartProcess (command, arguments, workingDirectory, wout, werr, exited, redirectStandardInput);	
		}
		
		public ProcessWrapper StartProcess (string command, string arguments, string workingDirectory, ProcessEventHandler outputStreamChanged, ProcessEventHandler errorStreamChanged, EventHandler exited)
		{
			return StartProcess (command, arguments, workingDirectory, outputStreamChanged, errorStreamChanged, exited, false);
		}

		public ProcessWrapper StartProcess (string command, string arguments, string workingDirectory, ProcessEventHandler outputStreamChanged, ProcessEventHandler errorStreamChanged, EventHandler exited, bool redirectStandardInput)
		{
			return StartProcess (CreateProcessStartInfo (command, arguments, workingDirectory, redirectStandardInput), 
				outputStreamChanged, errorStreamChanged, exited);
		}

		public ProcessWrapper StartProcess (ProcessStartInfo startInfo, TextWriter outWriter, TextWriter errorWriter, EventHandler exited)
		{
			ProcessEventHandler wout = OutWriter.GetWriteHandler (outWriter);
			ProcessEventHandler werr = OutWriter.GetWriteHandler (errorWriter);
			return StartProcess (startInfo, wout, werr, exited);	
		}
		
		public ProcessWrapper StartProcess (ProcessStartInfo startInfo, ProcessEventHandler outputStreamChanged, ProcessEventHandler errorStreamChanged, EventHandler exited)
		{
			if (startInfo == null)
				throw new ArgumentException ("startInfo");
		
			ProcessWrapper p = new ProcessWrapper();

			if (outputStreamChanged != null) {
				startInfo.RedirectStandardOutput = true;
				p.OutputStreamChanged += outputStreamChanged;
			}
				
			if (errorStreamChanged != null) {
				startInfo.RedirectStandardError = true;
				p.ErrorStreamChanged += errorStreamChanged;
			}

			startInfo.CreateNoWindow = true;
			p.StartInfo = startInfo;
			ProcessEnvironmentVariableOverrides (p.StartInfo);
			
			// FIXME: the bug is long gone, but removing the hacks in ProcessWrapper w/o bugs will be tricky
			// WORKAROUND for "Bug 410743 - wapi leak in System.Diagnostic.Process"
			// Process leaks when an exit event is registered
			// instead we use another thread to monitor I/O and wait for exit
			// if (exited != null)
			// 	p.Exited += exited;
			// p.EnableRaisingEvents = true;
			
			Counters.ProcessesStarted++;
			p.Start ();

			if (exited != null)
				p.Task.ContinueWith (t => exited (p, EventArgs.Empty), Runtime.MainTaskScheduler);

			return p;
		}

		public ProcessStartInfo CreateProcessStartInfo (string command, string arguments, string workingDirectory, bool redirectStandardInput)
		{
			if (command == null)
				throw new ArgumentNullException("command");
			
			if (command.Length == 0)
				throw new ArgumentException("command");
		
			ProcessStartInfo startInfo = null;
			if(String.IsNullOrEmpty (arguments))
				startInfo = new ProcessStartInfo (command);
			else
				startInfo = new ProcessStartInfo (command, arguments);
			
			if(workingDirectory != null && workingDirectory.Length > 0)
				startInfo.WorkingDirectory = workingDirectory;

			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardInput = redirectStandardInput;
			startInfo.UseShellExecute = false;

			return startInfo;
		}
		
		public ProcessAsyncOperation StartConsoleProcess (string command, string arguments, string workingDirectory, OperationConsole console,
			IDictionary<string, string> environmentVariables = null, EventHandler exited = null)
		{
			var externalConsole = console as ExternalConsole;

			if ((console == null || externalConsole != null) && externalConsoleHandler != null) {

				var dict = new Dictionary<string,string> ();
				if (environmentVariables != null)
					foreach (var kvp in environmentVariables)
						dict[kvp.Key] = kvp.Value;
				if (environmentVariableOverrides != null)
					foreach (var kvp in environmentVariableOverrides)
						dict[kvp.Key] = kvp.Value;
				
				var p = externalConsoleHandler (command, arguments, workingDirectory, dict,
					externalConsole?.Title ?? GettextCatalog.GetString ("{0} External Console", BrandingService.ApplicationName),
					externalConsole != null ? !externalConsole.CloseOnDispose : false);

				if (p != null) {
					if (exited != null)
						p.Task.ContinueWith (t => exited (p, EventArgs.Empty), Runtime.MainTaskScheduler);
					Counters.ProcessesStarted++;
					return p;
				} else {
					LoggingService.LogError ("Could not create external console for command: " + command + " " + arguments);
				}
			}
			ProcessStartInfo psi = CreateProcessStartInfo (command, arguments, workingDirectory, false);
			if (environmentVariables != null)
				foreach (KeyValuePair<string, string> kvp in environmentVariables)
					psi.EnvironmentVariables [kvp.Key] = kvp.Value;
			try {
				ProcessWrapper pw = StartProcess (psi, console.Out, console.Error, null);
				new ProcessMonitor (console, pw.ProcessAsyncOperation, exited);
				return pw.ProcessAsyncOperation;
			} catch (Exception ex) {
				// If the process can't be started, dispose the console now since ProcessMonitor won't do it
				console.Error.WriteLine (GettextCatalog.GetString ("The application could not be started"));
				LoggingService.LogError ("Could not start process for command: " + psi.FileName + " " + psi.Arguments, ex);
				console.Dispose ();
				return NullProcessAsyncOperation.Failure;
			}
		}
		
		public IExecutionHandler GetDefaultExecutionHandler (ExecutionCommand command)
		{
			if (executionHandlers == null) {
				executionHandlers = new List<ExtensionNode> ();
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/ExecutionHandlers", OnExtensionChange);
			}
			
			foreach (TypeExtensionNode codon in executionHandlers) {
				IExecutionHandler handler = (IExecutionHandler) codon.GetInstance (typeof(IExecutionHandler));
				if (handler.CanExecute (command)) return handler;
			}
			return null;
		}
		
		public ProcessExecutionCommand CreateCommand (string file)
		{
			foreach (ICommandFactory f in AddinManager.GetExtensionObjects<ICommandFactory> ("/MonoDevelop/Core/CommandFactories")) {
				var cmd = f.CreateCommand (file);
				if (cmd != null)
					return cmd;
			}
			return new NativeExecutionCommand (file);
		}

		public IEnumerable<IExecutionModeSet> GetExecutionModes ()
		{
			yield return defaultExecutionModeSet;
			foreach (ExtensionNode node in AddinManager.GetExtensionNodes (ExecutionModesExtensionPath)) {
				if (node is ExecutionModeSetNode)
					yield return (ExecutionModeSetNode) node;
				else if (!(node is ExecutionModeNode))
					yield return (IExecutionModeSet) ((TypeExtensionNode)node).GetInstance (typeof (IExecutionModeSet));
			}
		}

		/// <summary>
		/// Returns the debug execution mode set
		/// </summary>
		/// <remarks>The returned mode set can be used to run applications in debug mode</remarks>
		public IExecutionModeSet GetDebugExecutionMode ()
		{
			foreach (ExtensionNode node in AddinManager.GetExtensionNodes (ExecutionModesExtensionPath)) {
				if (node.Id == "Debug") {
					foreach (ExtensionNode childNode in node.ChildNodes) {
						if (childNode.Id == "MonoDevelop.Debugger")
							return (IExecutionModeSet) ((TypeExtensionNode)childNode).GetInstance (typeof (IExecutionModeSet));
					}
				}
			}
			return null;
		}

		public IExecutionHandler DefaultExecutionHandler {
			get {
				return defaultExecutionHandler;
			}
		}
		
		public IExecutionMode DefaultExecutionMode {
			get { return defaultExecutionMode; }
		}
		
		void OnExtensionChange (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				executionHandlers.Add (args.ExtensionNode);
			else
				executionHandlers.Remove (args.ExtensionNode);
		}
		
		ProcessHostController GetHost (string id, bool shared, IExecutionHandler executionHandler)
		{
			if (!shared)
				return new ProcessHostController (id, 0, executionHandler);
			
			lock (this) {
				if (externalProcess == null)
					externalProcess = new ProcessHostController ("SharedHostProcess", 10000, null);
	
				return externalProcess;
			}
		}

		void CheckRemoteType (Type type)
		{
			if (!typeof(IDisposable).IsAssignableFrom (type))
				throw new ArgumentException ("The remote object type must implement IDisposable", "type");
		}
		
		public IDisposable CreateExternalProcessObject (Type type, bool shared = true, IList<string> userAssemblyPaths = null, OperationConsole console = null)
		{
			CheckRemoteType (type);
			var hc = GetHost (type.ToString(), shared, null);
			return (IDisposable) hc.CreateInstance (type.Assembly.Location, type.FullName, GetRequiredAddins (type), userAssemblyPaths, console);
		}

		public IDisposable CreateExternalProcessObject (Type type, IExecutionHandler executionHandler, IList<string> userAssemblyPaths = null, OperationConsole console = null)
		{
			CheckRemoteType (type);
			var hc = GetHost (type.ToString (), false, executionHandler);
			return (IDisposable)hc.CreateInstance (type.Assembly.Location, type.FullName, GetRequiredAddins (type), userAssemblyPaths, console);
		}
		
		public bool IsValidForRemoteHosting (IExecutionHandler handler)
		{
			string location = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
			location = Path.Combine (location, "mdhost.exe");
			return handler.CanExecute (new DotNetExecutionCommand (location));
		}
		
		string[] GetRequiredAddins (Type type)
		{
			if (type.IsDefined (typeof(AddinDependencyAttribute), true)) {
				object[] ats = type.GetCustomAttributes (typeof(AddinDependencyAttribute), true);
				string[] addins = new string [ats.Length];
				for (int n=0; n<ats.Length; n++)
					addins [n] = ((AddinDependencyAttribute)ats [n]).Addin;
				return addins;
			} else
				return null;
		}
		
		internal void Dispose ()
		{
			RemotingService.Dispose ();
		}
		
		public class ExecutionModeReference
		{
			// This class can be used to hold a reference to the execution mode of an
			// execution set, and be able to compare it with other references.
			// It's useful for comparing references to IExecutionMode objects
			// obtained from different GetExecutionModes calls (which may return
			// new instances of IExecutionMode).
			
			IExecutionModeSet mset;
			IExecutionMode mode;
			
			public ExecutionModeReference (IExecutionModeSet mset, IExecutionMode mode)
			{
				this.mset = mset;
				this.mode = mode;
			}
			
			public override bool Equals (object obj)
			{
				ExecutionModeReference mref = obj as ExecutionModeReference;
				if (mref == null)
					return false;
				return mref.mset == mset && mref.mode.Name == mode.Name;
			}
			
			public override int GetHashCode ()
			{
				return mset.GetHashCode () + mode.Name.GetHashCode ();
			}
			
			public IExecutionMode ExecutionMode {
				get { return mode; }
			}
		}
	}
	
	class ProcessMonitor
	{
		public OperationConsole console;
		EventHandler exited;
		ProcessAsyncOperation operation;
		IDisposable cancelRegistration;

		public ProcessMonitor (OperationConsole console, ProcessAsyncOperation operation, EventHandler exited)
		{
			this.exited = exited;
			this.operation = operation;
			this.console = console;
			operation.Task.ContinueWith (t => OnOperationCompleted ());
			cancelRegistration = console.CancellationToken.Register (operation.Cancel);
		}
		
		public void OnOperationCompleted ()
		{
			cancelRegistration.Dispose ();
			try {
				if (exited != null)
					Runtime.RunInMainThread (() => {
						exited (operation, EventArgs.Empty);
					});

				if (!Platform.IsWindows && Mono.Unix.Native.Syscall.WIFSIGNALED (operation.ExitCode))
					console.Log.WriteLine (GettextCatalog.GetString ("The application was terminated by a signal: {0}"), Mono.Unix.Native.Syscall.WTERMSIG (operation.ExitCode));
				else if (operation.ExitCode != 0)
					console.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}"), operation.ExitCode);
			} catch (ArgumentException ex) {
				// ArgumentException comes from Syscall.WTERMSIG when an unknown signal is encountered
				console.Error.WriteLine (GettextCatalog.GetString ("The application was terminated by an unknown signal: {0}"), ex.Message);
			} finally {
				console.Dispose ();
			}
		}
	}
	
	class OutWriter
	{
		TextWriter writer;
		
		public OutWriter (TextWriter writer)
		{
			this.writer = writer;
		}
		
		public void WriteOut (object sender, string s)
		{
			writer.Write (s);
		}
		
		public static ProcessEventHandler GetWriteHandler (TextWriter tw)
		{
			return tw != null ? new ProcessEventHandler(new OutWriter (tw).WriteOut) : null;
		}
	}
	
	public delegate ProcessAsyncOperation ExternalConsoleHandler (string command, string arguments, string workingDirectory, IDictionary<string, string> environmentVariables, string title, bool pauseWhenFinished);
}
