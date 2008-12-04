//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Profiling
{
	public class DummyProcessAsyncOperation : IProcessAsyncOperation
	{
		public event OperationHandler Completed;
		
		private Process process;
		private bool done;
		
		public DummyProcessAsyncOperation (Process process)
		{
			this.process = process;
			
			process.Exited += new EventHandler (OnExited);
		}
		
		public int ExitCode {
			get { return process.ExitCode; }
		}
		
		public int ProcessId {
			get { return process.Id; }
		}
		
		public bool Success {
			get { return done ? ExitCode == 0 : false; }
		}
		
		public bool SuccessWithWarnings {
			get { return false; }
		}
		
		public bool IsCompleted {
			get { return done; }
		}
		
		public void Cancel ()
		{
			//do nothing, we don't actually want to kill the running process
			OnExited (null, EventArgs.Empty);
		}
		
		public void WaitForCompleted ()
		{
			//do nothing
		}
		
		void OnExited (object sender, EventArgs args)
		{
			done = true;
			if (Completed != null)
				Completed (this);
		}
	}
}