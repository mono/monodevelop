// CustomCommand.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.StringParsing;

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
		
		[ItemProperty ()]
		string workingdir;
		
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
		
		public string WorkingDir {
			get { return workingdir; }
			set { workingdir = value; }
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
			cmd.workingdir = workingdir;
			cmd.name = name;
			cmd.externalConsole = externalConsole;
			cmd.pauseExternalConsole = pauseExternalConsole;
			cmd.type = type;
			return cmd;
		}
		
		public void Execute (IProgressMonitor monitor, IWorkspaceObject entry, ConfigurationSelector configuration)
		{
			Execute (monitor, entry, null, configuration);
		}
		
		public bool CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			string exe = command;
			int i = exe.IndexOf (' ');
			if (i != -1)
				exe = command.Substring (0, i);
			
			ExecutionCommand cmd = Runtime.ProcessService.CreateCommand (exe);
			return context == null || context.ExecutionHandler.CanExecute (cmd);
		}
		
		public void Execute (IProgressMonitor monitor, IWorkspaceObject entry, ExecutionContext context, ConfigurationSelector configuration)
		{
			StringTagModel tagSource;
			
			if (entry is SolutionItem)
				tagSource = ((SolutionItem)entry).GetStringTagModel (configuration);
			else if (entry is WorkspaceItem)
				tagSource = ((WorkspaceItem)entry).GetStringTagModel ();
			else
				tagSource = new StringTagModel ();
			
			if (string.IsNullOrEmpty (command))
				return;
			
			int i = command.IndexOf (' ');
			string exe;
			string args = string.Empty;
			if (i == -1) {
				exe = command;
				args = string.Empty;
			} else {
				exe = command.Substring (0, i);
				args = StringParserService.Parse (command.Substring (i + 1), tagSource);
			}
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Executing: {0} {1}", exe, args));

			FilePath dir = (string.IsNullOrEmpty (workingdir) ? entry.BaseDirectory : (FilePath) StringParserService.Parse (workingdir, tagSource));

			FilePath localPath = entry.BaseDirectory.Combine (exe);
			if (File.Exists (localPath))
				exe = localPath;
			
			IProcessAsyncOperation oper;
			
			if (context != null) {
				IConsole console;
				if (externalConsole)
					console = context.ExternalConsoleFactory.CreateConsole (!pauseExternalConsole);
				else
					console = context.ConsoleFactory.CreateConsole (!pauseExternalConsole);

				ExecutionCommand cmd = Runtime.ProcessService.CreateCommand (exe);
				ProcessExecutionCommand pcmd = cmd as ProcessExecutionCommand;
				if (pcmd != null) {
					pcmd.Arguments = args;
					pcmd.WorkingDirectory = dir;
				}
				oper = context.ExecutionHandler.Execute (cmd, console);
			}
			else {
				if (externalConsole) {
					IConsole console = ExternalConsoleFactory.Instance.CreateConsole (!pauseExternalConsole);
					oper = Runtime.ProcessService.StartConsoleProcess (exe, args, dir, console, null);
				} else {
					oper = Runtime.ProcessService.StartProcess (exe, args, dir, monitor.Log, monitor.Log, null, false);
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
