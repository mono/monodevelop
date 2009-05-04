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
		bool started;
		static bool detectMdbVersion = true;
		
		public void StartDebugger ()
		{
			controller = new DebuggerController (this, Frontend);
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			if (started)
				controller.StopDebugger ();
		}
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			started = true;
			MonoDebuggerStartInfo info = (MonoDebuggerStartInfo) startInfo;
			controller.StartDebugger (info);
			controller.DebuggerServer.Run (info, detectMdbVersion);
			
			// Try to detect mdb version only once per session
			detectMdbVersion = false;
		}

		protected override void OnAttachToProcess (int processId)
		{
			started = true;
			controller.StartDebugger (null);
			controller.DebuggerServer.AttachToProcess (processId);
		}
		
		protected override void OnDetach ()
		{
			controller.DebuggerServer.Detach ();
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
		protected override object OnInsertBreakEvent (BreakEvent be, bool activate)
		{
			return controller.DebuggerServer.InsertBreakEvent (be, activate);
		}

		protected override void OnRemoveBreakEvent (object handle)
		{
			if (controller.DebuggerServer != null)
				controller.DebuggerServer.RemoveBreakEvent ((int)handle);
		}
		
		protected override void OnEnableBreakEvent (object handle, bool enable)
		{
			controller.DebuggerServer.EnableBreakEvent ((int)handle, enable);
		}

		protected override object OnUpdateBreakEvent (object handle, BreakEvent be)
		{
			return controller.DebuggerServer.UpdateBreakEvent (handle, be);
		}
		
		protected override void OnContinue ()
		{
			controller.DebuggerServer.Continue ();
		}
		
		protected override ThreadInfo[] OnGetThreads (int processId)
		{
			return controller.DebuggerServer.GetThreads (processId);
		}
		
		protected override ProcessInfo[] OnGetPocesses ()
		{
			return controller.DebuggerServer.GetPocesses ();
		}
		
		protected override Backtrace OnGetThreadBacktrace (int processId, int threadId)
		{
			return controller.DebuggerServer.GetThreadBacktrace (processId, threadId);
		}
		
		protected override void OnSetActiveThread (int processId, int threadId)
		{
			controller.DebuggerServer.SetActiveThread (processId, threadId);
		}
		
		protected override AssemblyLine[] OnDisassembleFile (string file)
		{
			return controller.DebuggerServer.DisassembleFile (file);
		}
		
		internal void UpdateBreakEvent (object handle, int count, string lastTrace)
		{
			if (count != -1)
				UpdateHitCount (handle, count);
			if (lastTrace != null)
				UpdateLastTraceValue (handle, lastTrace);
		}
	}
}
