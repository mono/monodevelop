// 
// MacExternalConsoleProcess.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.MacInterop;
using System.Threading.Tasks;
using System.IO;

namespace MonoDevelop.MacIntegration
{
	internal class MacExternalConsoleProcess : ProcessAsyncOperation
	{
/*
NOTES ON CONTROLLING A TERMINAL WITH APPLESCRIPT	 

//start a command in a new tab of a new window
tell app "Terminal" to do script "grep -r foo /Developer"
	tab 1 of window id 35938

//get whether the process is running, bash doesn't count for this, also doesn't seem reliable when waiting for input
tell app "Terminal" to get busy of tab 1 of window id 35938
	true
	
//get processes running in tab, not very useful
tell app "Terminal" to get processes of tab 1 of window id 35938
	login, bash, grep

//try to close the tab, fails
tell app "Terminal" to close tab 1 of window id 35938
	error: tab 1 of window id 35938 doesn’t understand the close message.

//try to close the window, can't stop it from prompting the user. maybe that's useful behaviour, like "press any key"?
tell app "Terminal" to close window id 35938
tell app "Terminal" to close window id 35938 saving no

// name the tab
tell app "Terminal" to set custom title of tab 1 of window id 35938 to "MonoDevelop Console"

//make sure tab closes as soon as the process ends by queueing an exit command
tell app "Terminal" to do script "exit" in tab 1 of window id 35938'

//start named process
tell app "Terminal" to do script "exec -a md_ps_333 grep -r foo /Developer"
	tab 1 of window id 36206

//kill the process we named
killall md_ps_333

using exec -a to name the process has problems because the terminal can get into a state
where running an explicit exec causes it to quit after the exec runs, so we can't use the 
bash pause on exit trick
*/ 

		//use full path to fix command failure when users have another app called "Terminal"
		//this happens with Parallels exporting a stub for the Unbuntu "Terminal"
		const string TERMINAL_APP = "/Applications/Utilities/Terminal.app";

		string windowId;

		public MacExternalConsoleProcess (string command, string arguments, string workingDirectory,
			IDictionary<string, string> environmentVariables,
			string title, bool pauseWhenFinished)
		{
			CancellationTokenSource = new CancellationTokenSource ();
			CancellationTokenSource.Token.Register (CloseTerminal);

			// FIXME set value of ProcessId, if possible
			string tabId;
			var intTask = RunTerminal (
				command, arguments, workingDirectory, environmentVariables, title, pauseWhenFinished,
				out tabId, out windowId, CancellationTokenSource.Token
			);
			intTask.ContinueWith (delegate {
				ExitCode = intTask.Result;
			});
			Task = intTask;
		}

		#region AppleScript terminal manipulation

		internal static Task<int> RunTerminal (
			string command, string arguments, string workingDirectory,
			IDictionary<string, string> environmentVariables,
			string title, bool pauseWhenFinished, out string tabId, out string windowId, CancellationToken cancelToken = default(CancellationToken))
		{
			TaskCompletionSource<int> taskSource = new TaskCompletionSource<int> ();
			cancelToken.Register (delegate {
				taskSource.SetCanceled ();
			});
			//build the sh command
			var sb = new StringBuilder ("clear; ");
			if (!string.IsNullOrEmpty (workingDirectory))
				sb.AppendFormat ("cd \"{0}\"; ", Escape (workingDirectory));
			if (environmentVariables != null) {
				foreach (string env in environmentVariables.Keys) {
					string val = environmentVariables [env];
					if (!string.IsNullOrEmpty (val))
						val = Escape (val);
					sb.AppendFormat ("export {0}=\"{1}\"; ", env, val);
				}
			}

			if (command != null) {
				sb.AppendFormat ("\"{0}\" {1}", Escape (command), arguments);
				var tempFileName = Path.GetTempFileName ();
				sb.Append ($"; echo $? > {tempFileName}");
				var fileWatcher = new FileSystemWatcher (Path.GetDirectoryName (tempFileName), Path.GetFileName (tempFileName));
				fileWatcher.EnableRaisingEvents = true;
				fileWatcher.Changed += delegate {
					lock(taskSource) {
						if (taskSource.Task.IsCompleted)
							return;
						taskSource.SetResult (int.Parse (File.ReadAllText (tempFileName)));
						File.Delete (tempFileName);
					}
				};
				
				if (pauseWhenFinished)
					sb.Append ("; echo; read -p \"Press any key to continue...\" -n1");
				sb.Append ("; exit");
			}

			//run the command in Terminal.app and extract tab and window IDs
			string appleScript;
			if (string.IsNullOrEmpty (command)) {
				appleScript = string.Format ("tell app \"{0}\" to do script \"{1}\"", TERMINAL_APP, Escape (sb.ToString ()));
			} else {
				// run the command inside Bash because we do echo $? and that is a bash extension and breaks when people
				// use other shells such as zsh or fish. https://bugzilla.xamarin.com/show_bug.cgi?id=56053
				appleScript = string.Format ("tell app \"{0}\" to do script \"bash -c '{1}'; exit\"", TERMINAL_APP, Escape (sb.ToString ()));
			}
			var ret = AppleScript.Run (appleScript);
			int i = ret.IndexOf ("of", StringComparison.Ordinal);
			tabId = ret.Substring (0, i -1);
			windowId = ret.Substring (i + 3);

			//rename tab and give it focus
			sb.Clear ();
			sb.AppendFormat ("tell app \"{0}\"\n", TERMINAL_APP);
			if (!string.IsNullOrEmpty (title))
				sb.AppendFormat ("\tset custom title of {0} of {1} to \"{2}\"\n", tabId, windowId, Escape (title));
			sb.AppendFormat ("\tset frontmost of {0} to true\n", windowId);
			sb.AppendFormat ("\tset selected of {0} of {1} to true\n", tabId, windowId);
			sb.Append ("\tactivate\n");
			sb.Append ("end tell");

			try {
				AppleScript.Run (sb.ToString ());
			} catch (Exception ex) {
				taskSource.SetException (ex);
			}
			return taskSource.Task;
		}
		
		//FIXME: make escaping work properly
		static string Escape (string str)
		{
			return str.Replace ("\\","\\\\").Replace ("\"", "\\\"");
		}

		static void CloseTerminalWindow (string windowId)
		{
			try {
				AppleScript.Run (
@"tell application ""{0}""
	activate
	set frontmost of {1} to true
	close {1}
	tell application ""System Events"" to tell process ""{0}"" to keystroke return
end tell", TERMINAL_APP, windowId);
			} catch (AppleScriptException) {
				//it may already have closed
			}
		}

		static bool TabExists (string tabId, string windowId)
		{
			return AppleScript.Run ("tell app \"{0}\" to get exists of {1} of {2}", TERMINAL_APP, tabId, windowId) == "true";
		}

		#endregion

		#region IAsyncOperation implementation
		
		void CloseTerminal ()
		{
			//FIXME: try to kill the process without closing the window, if pauseWhenFinished is true
			CloseTerminalWindow (windowId);
		}

		#endregion
	}
}
