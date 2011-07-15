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

namespace MonoDevelop.MacIntegration
{
	internal class MacExternalConsoleProcess : IProcessAsyncOperation
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
		string tabId, windowId;
		bool cancelled;
		
		public MacExternalConsoleProcess (string command, string arguments, string workingDirectory,
			IDictionary<string, string> environmentVariables,
			string title, bool pauseWhenFinished)
		{
			//build the sh command
			var sb = new StringBuilder ("clear; ");
			if (!string.IsNullOrEmpty (workingDirectory))
				sb.AppendFormat ("cd \"{0}\"; ", Escape (workingDirectory));
			foreach (string env in environmentVariables.Keys)
				sb.AppendFormat ("{0}=\"{1}\" ", env, Escape (environmentVariables[env]));
			sb.AppendFormat ("{0} {1}", Escape (command), arguments);
			if (pauseWhenFinished)
				sb.Append ("; echo; read -p 'Press any key to continue...' -n1");
			sb.Append ("; exit");
			var cmd = Escape (sb.ToString ());
			
			//run the command in Terminal.app and extrac tab and window handles
			var ret = AppleScript.Run ("tell app \"Terminal\" to do script \"{0}\"", cmd);
			int i = ret.IndexOf ("of");
			tabId = ret.Substring (0, i -1);
			windowId = ret.Substring (i + 3);
			
			//rename tab and give it focus
			AppleScript.Run (
@"tell app ""Terminal""
	set custom title of {0} of {1} to ""{2}""
	set frontmost of {1} to true
	set selected of {0} of {1} to true
	activate
end tell", tabId, windowId, Escape (title));
		}
		
		//FIXME: make escaping work properly
		string Escape (string str)
		{
			return str.Replace ("\\","\\\\").Replace ("\"", "\\\"");
		}
			
		#region IProcessAsyncOperation implementation

		public void Dispose ()
		{
		}
		
		public int ExitCode {
			get {
				//FIXME: implement. is it possible?
				return 0;
			}
		}
		
		public int ProcessId {
			get {
				//FIXME: implement. is it possible?
				return 0;
			}
		}
		
		#endregion
		
		#region IAsyncOperation implementation
		
		public event MonoDevelop.Core.OperationHandler Completed;
		
		void CloseTerminalWindow ()
		{
			try {
				AppleScript.Run (
@"tell application ""Terminal""
	activate
	set frontmost of {1} to true
	close {1}
	tell application ""System Events"" to tell process ""Terminal"" to keystroke return
end tell", tabId, windowId);
			} catch {}
		}
		
		public void Cancel ()
		{
			cancelled = true;
			//FIXME: try to kill the process without closing the window, if pauseWhenFinished is true
			CloseTerminalWindow ();
		}
		
		public void WaitForCompleted ()
		{
			while (!IsCompleted) {
				Thread.Sleep (1000);
			}
		}
		
		public bool IsCompleted {
			get {
				//FIXME: get the status of the process, not the whole script
				var ret = AppleScript.Run ("tell app \"Terminal\" to get exists of {0} of {1}", tabId, windowId);
				return ret != "true";
			}
		}
		
		public bool Success {
			get {
				//FIXME: any way to get the real result?
				return !cancelled;
			}
		}
		
		
		public bool SuccessWithWarnings {
			get {
				return Success;
			}
		}
		
		#endregion
	}
}