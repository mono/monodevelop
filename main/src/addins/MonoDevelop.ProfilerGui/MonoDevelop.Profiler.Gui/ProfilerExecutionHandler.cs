// 
// ProfilerExecutionHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Execution;
using System.Diagnostics;
using System.IO;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.Profiler
{
	public class ProfilerStartInfo
	{
		public string Command {
			get;
			set;
		}

		public string Arguments {
			get;
			set;
		}

		public string WorkingDirectory {
			get;
			set;
		}

		Dictionary<string, string> environmentVariables;
		public Dictionary<string, string> EnvironmentVariables {
			get {
				if (environmentVariables == null)
					environmentVariables = new Dictionary<string,string> ();
				return environmentVariables;
			}
		}

		public bool UseExternalConsole { get; set; }

		public bool CloseExternalConsoleOnExit { get; set; }
		
		public ProfilerStartInfo (string monoRuntimePrefix, Dictionary<string,string> monoRuntimeEnvironmentVariables)
		{
			this.MonoRuntimePrefix = monoRuntimePrefix;
			this.MonoRuntimeEnvironmentVariables = monoRuntimeEnvironmentVariables;
		}

		public string MonoRuntimePrefix { get; private set; }

		public Dictionary<string,string> MonoRuntimeEnvironmentVariables { get; private set; }

		/// <summary>
		/// The session will output this to the debug log as soon as it starts. It can be used to log warnings from
		/// creating the ProfilerStartInfo
		/// </summary>
		public string LogMessage { get; set; }
	}
	
	public class ProfilerExecutionHandler : IExecutionHandler
	{
		#region IExecutionHandler implementation
		public bool CanExecute (ExecutionCommand command)
		{
			return command is DotNetExecutionCommand;
		}

		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			var cmd = (DotNetExecutionCommand)command;
			string tmpfile = System.IO.Path.GetTempFileName ();
			string args = "--profile=log:output=" + tmpfile;
			cmd.RuntimeArguments += args;
			if (File.Exists (tmpfile))
				File.Delete (tmpfile);
			var result = cmd.TargetRuntime.GetExecutionHandler ().Execute (cmd, console);
			result.Completed += delegate {
				if (File.Exists (tmpfile)) {
					Application.Invoke (delegate {
						new ProfileDialog (tmpfile);
						File.Delete (tmpfile);
					});
				}
			};
			return result;
		}
		#endregion		
	}
}