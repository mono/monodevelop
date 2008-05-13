//
// DebugExecutionHandlerFactory.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Debugging.Client;
using MonoDevelop.Ide.Gui;
using Mono.Debugging;

namespace MonoDevelop.Ide.Debugging
{
	internal class DebugExecutionHandlerFactory: IExecutionHandlerFactory
	{
		public IExecutionHandler CreateExecutionHandler (string platformId)
		{
			if (DebuggerEngine.CanDebugPlatform (platformId))
				return new DebugExecutionHandler (platformId);
			else
				return null;
		}
	}
	
	class DebugExecutionHandler: IExecutionHandler, IProcessAsyncOperation
	{
		bool done;
		ManualResetEvent stopEvent;
		string platformId;
		
		public DebugExecutionHandler (string platformId)
		{
			this.platformId = platformId;
			IdeApp.Services.DebuggingService.StoppedEvent += new EventHandler (OnStopDebug);
		}
		
		public IProcessAsyncOperation Execute (string command, string arguments, string workingDirectory, IDictionary<string, string> environmentVariables, IConsole console)
		{
			DebuggerStartInfo startInfo = new DebuggerStartInfo ();
			startInfo.Command = command;
			startInfo.Arguments = arguments;
			startInfo.WorkingDirectory = workingDirectory;
			if (environmentVariables != null) {
				foreach (KeyValuePair<string,string> val in environmentVariables)
					startInfo.EnvironmentVariables [val.Key] = val.Value;
			}

			IdeApp.Services.DebuggingService.InternalRun (platformId, startInfo, console);
			return this;
		}
		
		public void Cancel ()
		{
			IdeApp.Services.DebuggingService.Stop ();
		}
		
		public void WaitForCompleted ()
		{
			lock (this) {
				if (done) return;
				if (stopEvent == null)
					stopEvent = new ManualResetEvent (false);
			}
			stopEvent.WaitOne ();
		}
		
		public int ExitCode {
			get { return 0; }
		}
		
		public bool IsCompleted {
			get { return done; }
		}
		
		public bool Success {
			get { return true; }
		}

		void OnStopDebug (object sender, EventArgs args)
		{
			lock (this) {
				done = true;
				if (stopEvent != null)
					stopEvent.Set ();
				if (completedEvent != null)
					completedEvent (this);
			}

			IdeApp.Services.DebuggingService.StoppedEvent -= new EventHandler (OnStopDebug);
		}
		
		event OperationHandler IAsyncOperation.Completed {
			add {
				bool raiseNow = false;
				lock (this) {
					if (done)
						raiseNow = true;
					else
						completedEvent += value;
				}
				if (raiseNow)
					value (this);
			}
			remove {
				lock (this) {
					completedEvent -= value;
				}
			}
		}
		
		//FIXME:
		public int ProcessId {
			get { return -1; }
		}
		
		event OperationHandler completedEvent;
	}
}
