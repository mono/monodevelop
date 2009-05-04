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
using Mono.Remoting.Channels.Unix;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using Mono.Addins;

namespace MonoDevelop.Core.Execution
{
	public class ProcessService : IDisposable
	{
		ProcessHostController externalProcess;
		List<ExtensionNode> executionHandlers;
		string remotingChannel = "unix";
		string unixRemotingFile;
		
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
			if (PlatformID.Unix != Environment.OSVersion.Platform) {
				remotingChannel = "tcp";
			}
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
				p.OutputStreamChanged += outputStreamChanged;
			}
				
			if (errorStreamChanged != null)
				p.ErrorStreamChanged += errorStreamChanged;
			
			if (exited != null)
				p.Exited += exited;

			startInfo.CreateNoWindow = true;
			p.StartInfo = startInfo;
			ProcessEnvironmentVariableOverrides (p.StartInfo);
			
			// WORKAROUND for "Bug 410743 - wapi leak in System.Diagnostic.Process"
			// Process leaks when an exit event is registered
			// instead we use another thread to monitor I/O and wait for exit
			// p.EnableRaisingEvents = true;
			
			p.Start ();
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
		
		public ProcessWrapper StartConsoleProcess (string command, string arguments, string workingDirectory, IConsole console, EventHandler exited)
		{
			return StartConsoleProcess (command, arguments, workingDirectory, null, console, exited);
		}
		
		public ProcessWrapper StartConsoleProcess (string command, string arguments, string workingDirectory, IDictionary<string, string> environmentVariables, IConsole console, EventHandler exited)
		{
			if (console == null || (console is ExternalConsole)) {
				ProcessStartInfo psi = ExternalConsoleLocator.GetConsoleProcess (command, arguments, workingDirectory, 
				    GettextCatalog.GetString ("MonoDevelop External Console"), !console.CloseOnDispose);

				if (environmentVariables != null)
					foreach (KeyValuePair<string, string> kvp in environmentVariables)
						psi.EnvironmentVariables [kvp.Key] = kvp.Value;
				
				ProcessWrapper p = new ProcessWrapper();
				
				if (exited != null)
					p.Exited += exited;

				psi.CreateNoWindow = true;
				p.StartInfo = psi;
				ProcessEnvironmentVariableOverrides (p.StartInfo);
				p.Start();
				return p;
			} else {
				ProcessStartInfo psi = CreateProcessStartInfo (command, arguments, workingDirectory, false);
				if (environmentVariables != null)
					foreach (KeyValuePair<string, string> kvp in environmentVariables)
						psi.EnvironmentVariables [kvp.Key] = kvp.Value;
				ProcessWrapper pw = StartProcess (psi, console.Out, console.Error, null);
				new ProcessMonitor (console, pw, exited);
				return pw;
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
		
		public ExecutionCommand CreateCommand (string file)
		{
			string f = file.ToLower ();
			if (f.EndsWith (".exe") || f.EndsWith (".dll"))
				return new DotNetExecutionCommand (file);
			else
				return new NativeExecutionCommand (file);
		}
		
		public IEnumerable<IExecutionModeSet> GetExecutionModes ()
		{
			yield return new DefaultExecutionModeSet ();
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/Core/ExecutionModes")) {
				if (!(node is ExecutionModeNode))
					yield return (IExecutionModeSet) node.CreateInstance (typeof (IExecutionModeSet));
			}
		}
		
		public IExecutionMode DefaultExecutionMode {
			get {
				return AddinManager.GetExtensionNode ("/MonoDevelop/Core/ExecutionModes/Default") as IExecutionMode;
			}
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
		
		public RemoteProcessObject CreateExternalProcessObject (Type type)
		{
			return CreateExternalProcessObject (type, true);
		}
		
		public RemoteProcessObject CreateExternalProcessObject (Type type, bool shared)
		{
			ProcessHostController hc = GetHost (type.ToString(), shared, null);
			RemoteProcessObject ob = hc.CreateInstance (type.Assembly.Location, type.FullName, GetRequiredAddins (type));
			return ob;
		}
		
		public RemoteProcessObject CreateExternalProcessObject (Type type, TargetRuntime runtime)
		{
			return CreateExternalProcessObject (type, runtime.GetExecutionHandler ());
		}
		
		public RemoteProcessObject CreateExternalProcessObject (Type type, IExecutionHandler executionHandler)
		{
			return GetHost (type.ToString(), false, executionHandler).CreateInstance (type.Assembly.Location, type.FullName, GetRequiredAddins (type));
		}
		
		public RemoteProcessObject CreateExternalProcessObject (string assemblyPath, string typeName, bool shared, params string[] requiredAddins)
		{
			return GetHost (typeName, shared, null).CreateInstance (assemblyPath, typeName, requiredAddins);
		}
		
		public RemoteProcessObject CreateExternalProcessObject (string assemblyPath, string typeName, IExecutionHandler executionHandler, params string[] requiredAddins)
		{
			return GetHost (typeName, false, executionHandler).CreateInstance (assemblyPath, typeName, requiredAddins);
		}
		
		public void DisposeExternalProcessObject (RemoteProcessObject obj, int timeout)
		{
			foreach (KeyValuePair<RemoteProcessObject,ProcessHostController> ob in runningObjects) {
				if (ob.Key == obj) {
					ob.Value.ReleaseInstance (obj, timeout);
					return;
				}
			}
		}

		// WARNING: don't use a hastable here since remote objects won't work as keys
		List<KeyValuePair<RemoteProcessObject,ProcessHostController>> runningObjects = new List<KeyValuePair<RemoteProcessObject, ProcessHostController>> ();
		
		internal void RegisterHostInstance (ProcessHostController hc, RemoteProcessObject obj)
		{
			runningObjects.Add (new KeyValuePair<RemoteProcessObject,ProcessHostController> (obj, hc));
		}
		
		internal void UnregisterHostInstance (ProcessHostController hc, RemoteProcessObject obj)
		{
			for (int n=0; n<runningObjects.Count; n++) {
				if ((obj != null && runningObjects[n].Key == obj) || (obj == null && runningObjects[n].Value == hc)) {
					runningObjects.RemoveAt (n--);
					return;
				}
			}
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
		
		public string ExternalProcessRemotingChannel {
			get { return remotingChannel; }
			set { 
				if (value != "tcp" && value != "unix")
					throw new InvalidOperationException ("Channel not supported: " + value);
				remotingChannel = value; 
			}
		}
		
		internal string RegisterRemotingChannel ()
		{
			if (remotingChannel == "tcp") {
				IChannel ch = ChannelServices.GetChannel ("tcp");
				if (ch == null) {
					IDictionary dict = new Hashtable ();
					BinaryClientFormatterSinkProvider clientProvider = new BinaryClientFormatterSinkProvider();
					BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();

					dict ["port"] = 0;
					serverProvider.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

					ChannelServices.RegisterChannel (new TcpChannel (dict, clientProvider, serverProvider), false);
				}
			} else {
				IChannel ch = ChannelServices.GetChannel ("unix");
				if (ch == null) {
					unixRemotingFile = Path.GetTempFileName ();
					ChannelServices.RegisterChannel (new UnixChannel (unixRemotingFile), false);
				}
			}
			return remotingChannel;
		}
		
		public virtual void Dispose ()
		{
			if (unixRemotingFile != null)
				File.Delete (unixRemotingFile);
		}
	}
	
	class ProcessMonitor
	{
		public IConsole console;
		EventHandler exited;
		IProcessAsyncOperation operation;

		public ProcessMonitor (IConsole console, IProcessAsyncOperation operation, EventHandler exited)
		{
			this.exited = exited;
			this.operation = operation;
			this.console = console;
			operation.Completed += new OperationHandler (OnOperationCompleted);
			console.CancelRequested += new EventHandler (OnCancelRequest);
		}
		
		public void OnOperationCompleted (IAsyncOperation op)
		{
			try {
				if (exited != null)
					exited (op, null);
				
				if (Mono.Unix.Native.Syscall.WIFSIGNALED (operation.ExitCode))
					console.Log.WriteLine (GettextCatalog.GetString ("The application was terminated by a signal: {0}"), Mono.Unix.Native.Syscall.WTERMSIG (operation.ExitCode));
				else if (operation.ExitCode != 0)
					console.Log.WriteLine (GettextCatalog.GetString ("The application exited with code: {0}"), operation.ExitCode);
			} finally {
				console.Dispose ();
			}
		}

		void OnCancelRequest (object sender, EventArgs args)
		{
			operation.Cancel ();

			//remove the cancel handler, it will be attached again when StartConsoleProcess is called
			console.CancelRequested -= new EventHandler (OnCancelRequest);
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
}
