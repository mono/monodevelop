
using System;
using System.IO;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects
{
	public class CustomCommand
	{
		[ItemProperty ()]
		CustomCommandType type;
		
		[ItemProperty (DefaultValue = "")]
		string name = "";
		
		[ItemProperty ()]
		string command;
		
		[ItemProperty (DefaultValue = false)]
		bool externalConsole;
		
		[ItemProperty (DefaultValue = false)]
		bool pauseExternalConsole;
		
		public CustomCommandType Type {
			get { return type; }
			set { type = value; }
		}
		
		public string Command {
			get { return command; }
			set { command = value; }
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public bool ExternalConsole {
			get { return externalConsole; }
			set { externalConsole = value; }
		}
		
		public bool PauseExternalConsole {
			get { return pauseExternalConsole; }
			set { pauseExternalConsole = value; }
		}
		
		public CustomCommand Clone ()
		{
			CustomCommand cmd = new CustomCommand ();
			cmd.command = command;
			cmd.name = name;
			cmd.externalConsole = externalConsole;
			cmd.pauseExternalConsole = pauseExternalConsole;
			cmd.type = type;
			return cmd;
		}
		
		public void Execute (IProgressMonitor monitor, CombineEntry entry)
		{
			Execute (monitor, entry, null);
		}
		
		public void Execute (IProgressMonitor monitor, CombineEntry entry, ExecutionContext context)
		{
			monitor.Log.WriteLine (GettextCatalog.GetString ("Executing: {0}", command));
			
			int i = command.IndexOf (' ');
			string exe;
			string args = "";
			if (i == -1) {
				exe = command;
				args = string.Empty;
			} else {
				exe = command.Substring (0, i);
				args = command.Substring (i + 1);
			}
			string localPath = Path.Combine (entry.BaseDirectory, exe);
			if (File.Exists (localPath))
				exe = localPath;
			
			IProcessAsyncOperation oper;
			
			if (context != null) {
				IConsole console;
				if (externalConsole)
					console = context.ExternalConsoleFactory.CreateConsole (!pauseExternalConsole);
				else
					console = context.ConsoleFactory.CreateConsole (!pauseExternalConsole);
				IExecutionHandler handler = context.ExecutionHandlerFactory.CreateExecutionHandler ("Native");
				oper = handler.Execute (exe, args, entry.BaseDirectory, console);
			}
			else {
				if (externalConsole) {
					IConsole console = ExternalConsoleFactory.Instance.CreateConsole (!pauseExternalConsole);
					oper = Runtime.ProcessService.StartConsoleProcess (exe, args, entry.BaseDirectory, console, null);
				} else {
					oper = Runtime.ProcessService.StartProcess (exe, args, entry.BaseDirectory, monitor.Log, monitor.Log, null, false);
				}
			}
			
			monitor.CancelRequested += delegate {
				if (!oper.IsCompleted)
					oper.Cancel ();
			};
			
			oper.WaitForCompleted ();
			if (!oper.Success) {
				monitor.ReportError ("Custom command failed (exit code: " + oper.ExitCode + ")", null);
				monitor.AsyncOperation.Cancel ();
			}
		}
	}
}
