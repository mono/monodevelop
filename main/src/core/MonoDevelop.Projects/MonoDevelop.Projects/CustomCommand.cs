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
		
		public void Execute (IProgressMonitor monitor, IWorkspaceObject entry, string configuration)
		{
			Execute (monitor, entry, null, configuration);
		}
		
		public bool CanExecute (ExecutionContext context, string configuration)
		{
			return context == null || context.ExecutionHandlerFactory.SupportsPlatform ("Native");
		}
		
		public void Execute (IProgressMonitor monitor, IWorkspaceObject entry, ExecutionContext context, string configuration)
		{
			string [,] customtags = null;
			
			Project project = entry as Project;
			if (project != null) {
				string outputname = project.GetOutputFileName(configuration);
				customtags = new string [,] {
					// Keep in sync with CustomCommandWidget.cs
					{"ItemName", entry.Name},
					{"ItemDir", entry.BaseDirectory},
					{"ItemFile", project.FileName},
					{"ProjectName", entry.Name},
					{"ProjectDir", entry.BaseDirectory},
					{"ProjectFile", project.FileName},
					{"TargetName", Path.GetFileName (outputname)},
					{"TargetDir", Path.GetDirectoryName (outputname)},
					{"SolutionName", project.ParentSolution.Name},
					{"SolutionDir", project.ParentSolution.BaseDirectory},
					{"SolutionFile", project.ParentSolution.FileName}
				};
			} else if (entry is SolutionEntityItem) {
				SolutionEntityItem it = entry as SolutionEntityItem;
				customtags = new string [,] {
					// Keep in sync with CustomCommandWidget.cs
					{"ItemName", it.Name},
					{"ItemDir", it.BaseDirectory},
					{"ItemFile", it.FileName},
					{"ProjectName", it.Name},
					{"ProjectDir", it.BaseDirectory},
					{"ProjectFile", it.FileName},
					{"SolutionName", it.ParentSolution.Name},
					{"SolutionDir", it.ParentSolution.BaseDirectory},
					{"SolutionFile", it.ParentSolution.FileName}
				};
			} else {
				customtags = new string [,] {
					// Keep in sync with CustomCommandWidget.cs
					{"SolutionName", entry.Name},
					{"SolutionDir", entry.BaseDirectory}
				};
			}
			
			int i = command.IndexOf (' ');
			string exe;
			string args = string.Empty;
			if (i == -1) {
				exe = command;
				args = string.Empty;
			} else {
				exe = command.Substring (0, i);
				args = StringParserService.Parse (command.Substring (i + 1), customtags);
			}
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Executing: {0} {1}", exe, args));

			string dir = (string.IsNullOrEmpty (workingdir) ? entry.BaseDirectory : StringParserService.Parse (workingdir, customtags));
			
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
				oper = handler.Execute (exe, args, dir, null, console);
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
