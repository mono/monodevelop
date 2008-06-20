// MonoDebuggerSession.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend.Mdb
{
	public class MonoDebuggerSession: DebuggerSession
	{
		DebuggerController controller;
		
		public void StartDebugger ()
		{
			controller = new DebuggerController (Frontend);
			controller.StartDebugger ();
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			controller.StopDebugger ();
		}
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			controller.DebuggerServer.Run (startInfo);
		}

		protected override void OnAttachToProcess (int processId)
		{
			controller.DebuggerServer.AttachToProcess (processId);
		}
		
		protected override void OnStop ()
		{
			controller.DebuggerServer.Stop ();
		}
		
		protected override void OnExit ()
		{
			controller.Exit ();
		}

		protected override void OnStepLine ()
		{
			controller.DebuggerServer.StepLine ();
		}

		protected override void OnNextLine ()
		{
			controller.DebuggerServer.NextLine ();
		}

		protected override void OnStepInstruction ()
		{
			controller.DebuggerServer.StepInstruction ();
		}

		protected override void OnNextInstruction ()
		{
			controller.DebuggerServer.NextInstruction ();
		}

		protected override void OnFinish ()
		{
			controller.DebuggerServer.Finish ();
		}

		//breakpoints etc

		// returns a handle
		protected override int OnInsertBreakpoint (string filename, int line, bool activate)
		{
			return controller.DebuggerServer.InsertBreakpoint (filename, line, activate);
		}

		protected override void OnRemoveBreakpoint (int handle)
		{
			controller.DebuggerServer.RemoveBreakpoint (handle);
		}
		
		protected override void OnEnableBreakpoint (int handle, bool enable)
		{
			controller.DebuggerServer.EnableBreakpoint (handle, enable);
		}

		protected override void OnContinue ()
		{
			controller.DebuggerServer.Continue ();
		}
		
		protected override ThreadInfo[] OnGetThreads (int processId)
		{
			return new ThreadInfo [0];
		}
		
		protected override ProcessInfo[] OnGetPocesses ()
		{
			return new ProcessInfo [0];
		}
		
		protected override Backtrace OnGetThreadBacktrace (int threadId)
		{
			return null;
		}
	}
}
