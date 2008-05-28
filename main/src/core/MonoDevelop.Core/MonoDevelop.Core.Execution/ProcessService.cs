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
using System.Diagnostics;
using Mono.Remoting.Channels.Unix;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Core.Execution
{
	public class ProcessService : AbstractService
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
		
		public override void InitializeService ()
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
				
			p.StartInfo = startInfo;
			ProcessEnvironmentVariableOverrides (p.StartInfo);
			p.EnableRaisingEvents = true;
			
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
				string additionalCommands = "";
				if (!console.CloseOnDispose)
					additionalCommands = @"echo; read -p 'Press any key to continue...' -n1;";
				string xtermCommand = String.Format (
					@" -title ""{4}"" -e ""cd {3} ; '{0}' {1} ; {2}""",
					command,
					arguments.Replace ("\\", "\\\\").Replace ("\"", "\\\""),
					additionalCommands,
					workingDirectory.Replace (" ", "\\ "),
				    GettextCatalog.GetString ("MonoDevelop External Console")
				);
				ProcessStartInfo psi = new ProcessStartInfo("xterm",xtermCommand);
				psi.UseShellExecute = false;
				
				if (workingDirectory != null)
					psi.WorkingDirectory = workingDirectory;

				psi.UseShellExecute  =  false;
				
				if (environmentVariables != null)
					foreach (KeyValuePair<string, string> kvp in environmentVariables)
						psi.EnvironmentVariables [kvp.Key] = kvp.Value;
				
				ProcessWrapper p = new ProcessWrapper();
				
				if (exited != null)
					p.Exited += exited;
				
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
		
		public IExecutionHandler GetDefaultExecutionHandler (string platformId)
		{
			if (executionHandlers == null) {
				executionHandlers = new List<ExtensionNode> ();
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/ExecutionHandlers", OnExtensionChange);
			}
			
			foreach (ExecutionHandlerCodon codon in executionHandlers)
				if (codon.Platform == platformId) return codon.ExecutionHandler;
			return null;
		}
		
		void OnExtensionChange (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				executionHandlers.Add (args.ExtensionNode);
			else
				executionHandlers.Remove (args.ExtensionNode);
		}
		
		ProcessHostController GetHost (string id, bool shared)
		{
			if (!shared)
				return new ProcessHostController (id, 0);
			
			lock (this) {
				if (externalProcess == null)
					externalProcess = new ProcessHostController ("SharedHostProcess", 10000);
	
				return externalProcess;
			}
		}
		
		public RemoteProcessObject CreateExternalProcessObject (Type type)
		{
			return CreateExternalProcessObject (type, true);
		}
		
		public RemoteProcessObject CreateExternalProcessObject (Type type, bool shared)
		{
			return GetHost (type.ToString(), shared).CreateInstance (type.Assembly.Location, type.FullName, GetRequiredAddins (type));
		}
		
		public RemoteProcessObject CreateExternalProcessObject (string assemblyPath, string typeName, bool shared, params string[] requiredAddins)
		{
			return GetHost (typeName, shared).CreateInstance (assemblyPath, typeName, requiredAddins);
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
				if (ch == null)
					ChannelServices.RegisterChannel (new TcpChannel (0), false);
			} else {
				IChannel ch = ChannelServices.GetChannel ("unix");
				if (ch == null) {
					unixRemotingFile = Path.GetTempFileName ();
					ChannelServices.RegisterChannel (new UnixChannel (unixRemotingFile), false);
				}
			}
			return remotingChannel;
		}
		
		public override void UnloadService ()
		{
			if (unixRemotingFile != null)
				File.Delete (unixRemotingFile);
		}
	}
	
	class ProcessMonitor
	{
		public IConsole console;
		EventHandler exited;
		IAsyncOperation operation;

		public ProcessMonitor (IConsole console, IAsyncOperation operation, EventHandler exited)
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
