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
using System.ComponentModel;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.StringParsing;
using System.Collections.Generic;
using MonoDevelop.Core.ProgressMonitoring;
using System.Threading.Tasks;
using System.Threading;

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
		
		[ItemProperty("EnvironmentVariables", SkipEmpty = true)]
		[ItemProperty("Variable", Scope = "item")]
		[ItemProperty("name", Scope = "key")]
		[ItemProperty("value", Scope = "value")]
		Dictionary<string, string> environmentVariables = new Dictionary<string, string> ();
		
		public Dictionary<string, string> EnvironmentVariables {
			get { return environmentVariables; }
		}
		
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
		
		public string GetCommandFile (WorkspaceObject entry, ConfigurationSelector configuration)
		{
			string exe, args;
			StringTagModel tagSource = GetTagModel (entry, configuration);
			ParseCommand (tagSource, out exe, out args);
			return exe;
		}
		
		public string GetCommandArgs (WorkspaceObject entry, ConfigurationSelector configuration)
		{
			string exe, args;
			StringTagModel tagSource = GetTagModel (entry, configuration);
			ParseCommand (tagSource, out exe, out args);
			return args;
		}
		
		public FilePath GetCommandWorkingDir (WorkspaceObject entry, ConfigurationSelector configuration)
		{
			StringTagModel tagSource = GetTagModel (entry, configuration);
			if (string.IsNullOrEmpty (workingdir))
				return entry.BaseDirectory;
			FilePath dir = StringParserService.Parse (workingdir, tagSource);
			return dir.ToAbsolute (entry.BaseDirectory);
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
		
		StringTagModel GetTagModel (WorkspaceObject entry, ConfigurationSelector configuration)
		{
			if (entry is SolutionFolderItem)
				return ((SolutionFolderItem)entry).GetStringTagModel (configuration);
			else if (entry is WorkspaceItem)
				return ((WorkspaceItem)entry).GetStringTagModel ();
			else
				return new StringTagModel ();
		}
		
		void ParseCommand (StringTagModel tagSource, out string cmd, out string args)
		{
			if (command.Length > 0 && command [0] == '"') {
				int n = command.IndexOf ('"', 1);
				if (n != -1) {
					cmd = command.Substring (1, n - 1);
					args = command.Substring (n + 1).Trim ();
				}
				else {
					cmd = command;
					args = string.Empty;
				}
			}
			else {
				int i = command.IndexOf (' ');
				if (i != -1) {
					cmd = command.Substring (0, i);
					args = command.Substring (i + 1).Trim ();
				} else {
					cmd = command;
					args = string.Empty;
				}
			}
			cmd = StringParserService.Parse (cmd, tagSource);
			args = StringParserService.Parse (args, tagSource);
		}
		
		public ProcessExecutionCommand CreateExecutionCommand (WorkspaceObject entry, ConfigurationSelector configuration)
		{
			string exe, args;
			StringTagModel tagSource = GetTagModel (entry, configuration);
			ParseCommand (tagSource, out exe, out args);
			
			//if the executable name matches an executable in the project directory, use that, for back-compat
			//else fall back and let the execution handler handle it via PATH, working directory, etc.
			if (!Path.IsPathRooted (exe)) {
				string localPath = ((FilePath) exe).ToAbsolute (entry.BaseDirectory).FullPath;
				if (File.Exists (localPath))
					exe = localPath;
			}
			
			ProcessExecutionCommand cmd = Runtime.ProcessService.CreateCommand (exe);
			
			cmd.Arguments = args;
			
			FilePath workingDir = this.workingdir;
			if (!workingDir.IsNullOrEmpty)
				workingDir = StringParserService.Parse (workingDir, tagSource);
			cmd.WorkingDirectory = workingDir.IsNullOrEmpty
				? entry.BaseDirectory
				: workingDir.ToAbsolute (entry.BaseDirectory);
			
			if (environmentVariables != null) {
				var vars = new Dictionary<string, string> ();
				foreach (var v in environmentVariables)
					vars [v.Key] = StringParserService.Parse (v.Value, tagSource);
				cmd.EnvironmentVariables = vars;
			}
			
			return cmd;
		}
		
		public Task<bool> Execute (ProgressMonitor monitor, WorkspaceObject entry, ConfigurationSelector configuration)
		{
			return Execute (monitor, entry, null, configuration);
		}
		
		public bool CanExecute (WorkspaceObject entry, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (string.IsNullOrEmpty (command))
				return false;
			
			if (context == null)
				return true;
			
			var cmd = CreateExecutionCommand (entry, configuration);
			return context.ExecutionHandler.CanExecute (cmd);
		}
		
		public async Task<bool> Execute (ProgressMonitor monitor, WorkspaceObject entry, ExecutionContext context,
			ConfigurationSelector configuration)
		{
			ProcessExecutionCommand cmd = CreateExecutionCommand (entry, configuration);
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Executing: {0} {1}", cmd.Command, cmd.Arguments));
			
			if (!Directory.Exists (cmd.WorkingDirectory)) {
				monitor.ReportError (GettextCatalog.GetString ("Custom command working directory does not exist"), null);
				return false;
			}
			
			ProcessAsyncOperation oper = null;
			IConsole console = null;
			var result = true;
			
			try {
				if (context != null) {
					if (externalConsole)
						console = context.ExternalConsoleFactory.CreateConsole (!pauseExternalConsole);
					else
						console = context.ConsoleFactory.CreateConsole (!pauseExternalConsole);
					oper = context.ExecutionHandler.Execute (cmd, console);
				} else {
					if (externalConsole) {
						console = ExternalConsoleFactory.Instance.CreateConsole (!pauseExternalConsole);
						oper = Runtime.ProcessService.StartConsoleProcess (cmd.Command, cmd.Arguments,
							cmd.WorkingDirectory, console, null);
					} else {
						oper = Runtime.ProcessService.StartProcess (cmd.Command, cmd.Arguments,
							cmd.WorkingDirectory, monitor.Log, monitor.Log, null, false).ProcessAsyncOperation;
					}
				}

				var stopper = monitor.CancellationToken.Register (oper.Cancel);

				await oper.Task;

				stopper.Dispose ();

				if (oper.ExitCode != 0) {
					monitor.ReportError ("Custom command failed (exit code: " + oper.ExitCode + ")", null);
				}
			} catch (Win32Exception w32ex) {
				monitor.ReportError (GettextCatalog.GetString ("Failed to execute custom command '{0}': {1}",
					cmd.Command, w32ex.Message), null);
				return false;
			} catch (Exception ex) {
				LoggingService.LogError ("Command execution failed", ex);
				throw new UserException (GettextCatalog.GetString ("Command execution failed: {0}", ex.Message));
			} finally {
				result = oper != null && oper.ExitCode == 0;
				if (console != null) {
					console.Dispose ();
				}
			}
			return result;
		}
	}
}
