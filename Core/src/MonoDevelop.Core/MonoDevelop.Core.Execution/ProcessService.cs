// created on 12/17/2004 at 22:07
using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using Mono.Remoting.Channels.Unix;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Core.Execution
{
	public class ProcessService : AbstractService
	{
		ProcessHostController externalProcess;
		ArrayList executionHandlers;
		string remotingChannel = "unix";
		string unixRemotingFile;
		
		public override void InitializeService ()
		{
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
			ProcessEventHandler wout = OutWriter.GetWriteHandler (outWriter);
			ProcessEventHandler werr = OutWriter.GetWriteHandler (errorWriter);
			return StartProcess (command, arguments, workingDirectory, wout, werr, exited);	
		}
		
		public ProcessWrapper StartProcess (string command, string arguments, string workingDirectory, ProcessEventHandler outputStreamChanged, ProcessEventHandler errorStreamChanged, EventHandler exited)
		{
			if (command == null)
				throw new ArgumentNullException("command");
			
			if (command.Length == 0)
				throw new ArgumentException("command");
		
			ProcessWrapper p = new ProcessWrapper();

			if (outputStreamChanged != null) {
				p.OutputStreamChanged += outputStreamChanged;
			}
				
			if (errorStreamChanged != null)
				p.ErrorStreamChanged += errorStreamChanged;
			
			if (exited != null)
				p.Exited += exited;
				
			if(String.IsNullOrEmpty (arguments))
				p.StartInfo = new ProcessStartInfo (command);
			else
				p.StartInfo = new ProcessStartInfo (command, arguments);
			
			if(workingDirectory != null && workingDirectory.Length > 0)
				p.StartInfo.WorkingDirectory = workingDirectory;


			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.UseShellExecute = false;
			p.EnableRaisingEvents = true;
			
			p.Start ();
			return p;
		}
		
		public ProcessWrapper StartConsoleProcess (string command, string arguments, string workingDirectory, IConsole console, EventHandler exited)
		{
			if (console == null || (console is ExternalConsole)) {
				string additionalCommands = "";
				if (!console.CloseOnDispose)
					additionalCommands = @"echo; read -p 'Press any key to continue...' -n1;";
				ProcessStartInfo psi = new ProcessStartInfo("xterm",
					String.Format (@"-e ""cd {3} ; '{0}' {1} ; {2}""", command, arguments, additionalCommands, workingDirectory.Replace (" ", "\\ ")));
				psi.UseShellExecute = false;
				
				if (workingDirectory != null)
					psi.WorkingDirectory = workingDirectory;

				psi.UseShellExecute  =  false;
				
				ProcessWrapper p = new ProcessWrapper();
				
				if (exited != null)
					p.Exited += exited;
				
				p.StartInfo = psi;
				p.Start();
				return p;
			} else {
				ProcessWrapper pw = StartProcess (command, arguments, workingDirectory, console.Out, console.Error, null);
				new ProcessMonitor (console, pw, exited);
				return pw;
			}
		}
		
		public IExecutionHandler GetDefaultExecutionHandler (string platformId)
		{
			if (executionHandlers == null) {
				executionHandlers = new ArrayList ();
				Runtime.AddInService.RegisterExtensionItemListener ("/SharpDevelop/Workbench/ExecutionHandlers", OnExtensionChange);
			}
			
			foreach (ExecutionHandlerCodon codon in executionHandlers)
				if (codon.Platform == platformId) return codon.ExecutionHandler;
			return null;
		}
		
		void OnExtensionChange (ExtensionAction action, object item)
		{
			if (action == ExtensionAction.Add)
				executionHandlers.Add (item);
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
					ChannelServices.RegisterChannel (new TcpChannel (0));
			} else {
				IChannel ch = ChannelServices.GetChannel ("unix");
				if (ch == null) {
					unixRemotingFile = Path.GetTempFileName ();
					ChannelServices.RegisterChannel (new UnixChannel (unixRemotingFile));
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
			writer.WriteLine (s);
		}
		
		public static ProcessEventHandler GetWriteHandler (TextWriter tw)
		{
			return tw != null ? new ProcessEventHandler(new OutWriter (tw).WriteOut) : null;
		}
	}
}
